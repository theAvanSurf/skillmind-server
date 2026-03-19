import { Injectable, Inject, Logger } from '@nestjs/common';
import { HttpService } from '@nestjs/axios';
import { ConfigService } from '@nestjs/config';
import { firstValueFrom } from 'rxjs';
import { OTPService } from './otp.service';
import { EmailService } from './email.service';
import {
  InvalidCurrentPasswordException,
  CredentialChangeException,
  RateLimitExceededException,
  VerificationCodeRequiredException,
} from './auth.exceptions';
import {
  InitiatePasswordChangeDto,
  InitiateEmailChangeDto,
  VerifyOTPDto,
  ConfirmPasswordChangeDto,
  ConfirmEmailChangeDto,
  OTPSentResponseDto,
  ChangeCredentialResponseDto,
  VerificationStatusDto,
} from './auth.dto';

@Injectable()
export class AuthService {
  private readonly logger = new Logger(AuthService.name);
  private readonly coreApiUrl: string;
  private readonly coreApiTimeout: number = 10000;

  constructor(
    private readonly otpService: OTPService,
    private readonly emailService: EmailService,
    private readonly httpService: HttpService,
    private readonly configService: ConfigService,
  ) {
    this.coreApiUrl = this.configService.get<string>('CORE_API_URL') || 'http://localhost:5000';
  }

  /**
   * Inicia cambio de contraseña
   */
  async initiatePasswordChange(userId: string, userEmail: string, dto: InitiatePasswordChangeDto): Promise<OTPSentResponseDto> {
    try {
      // Validar contraseña actual contra .NET
      const isValidPassword = await this.validateCurrentPassword(userId, dto.currentPassword);

      if (!isValidPassword) {
        this.logger.warn(`Invalid current password attempt for user ${userId}`);
        throw new InvalidCurrentPasswordException();
      }

      // Generar y almacenar OTP
      const { code, expiresIn } = await this.otpService.createOTP(userId, 'password');

      // Enviar OTP por email
      await this.emailService.sendOTPEmail(userEmail, code, 'password', Math.ceil(expiresIn / 60));

      this.logger.log(`Password change initiated for user ${userId}`);

      return {
        success: true,
        message: 'Verification code sent to your email',
        expiresIn,
        destination: this.maskEmail(userEmail),
      };
    } catch (error) {
      if (error instanceof InvalidCurrentPasswordException) {
        throw error;
      }
      const errorMessage = error instanceof Error ? error.message : String(error);
      if (errorMessage.includes('Rate limit')) {
        throw new RateLimitExceededException();
      }
      this.logger.error(`Failed to initiate password change: ${errorMessage}`);
      throw new CredentialChangeException('Failed to initiate password change');
    }
  }

  /**
   * Inicia cambio de email
   */
  async initiateEmailChange(userId: string, currentEmail: string, dto: InitiateEmailChangeDto): Promise<OTPSentResponseDto> {
    try {
      // Validar que el email nuevo no esté registrado
      const isEmailTaken = await this.checkEmailExists(dto.newEmail);

      if (isEmailTaken) {
        throw new CredentialChangeException('Email is already registered');
      }

      // Validar contraseña actual
      const isValidPassword = await this.validateCurrentPassword(userId, dto.currentPassword);

      if (!isValidPassword) {
        throw new InvalidCurrentPasswordException();
      }

      // Generar y almacenar OTP
      const { code, expiresIn } = await this.otpService.createOTP(userId, 'email');

      // Enviar OTP al email actual para confirmar identidad
      await this.emailService.sendOTPEmail(currentEmail, code, 'email', Math.ceil(expiresIn / 60));

      this.logger.log(`Email change initiated for user ${userId}, new email: ${dto.newEmail}`);

      return {
        success: true,
        message: 'Verification code sent to your current email',
        expiresIn,
        destination: this.maskEmail(currentEmail),
      };
    } catch (error) {
      if (
        error instanceof InvalidCurrentPasswordException ||
        error instanceof CredentialChangeException ||
        error instanceof RateLimitExceededException
      ) {
        throw error;
      }
      const errorMessage = error instanceof Error ? error.message : String(error);
      if (errorMessage.includes('Rate limit')) {
        throw new RateLimitExceededException();
      }
      this.logger.error(`Failed to initiate email change: ${errorMessage}`);
      throw new CredentialChangeException('Failed to initiate email change');
    }
  }

  /**
   * Confirma cambio de contraseña
   */
  async confirmPasswordChange(userId: string, userEmail: string, dto: ConfirmPasswordChangeDto): Promise<ChangeCredentialResponseDto> {
    try {
      // Verificar OTP
      const isOTPValid = await this.otpService.verifyOTP(userId, dto.verificationCode);

      if (!isOTPValid) {
        throw new CredentialChangeException('OTP verification failed');
      }

      // Cambiar contraseña en .NET
      const changeResult = await this.changePasswordInCore(userId, dto.newPassword);

      if (!changeResult.success) {
        throw new CredentialChangeException(changeResult.message || 'Failed to change password in core service');
      }

      // Invalidar todas las sesiones (excepto la actual)
      await this.invalidateAllSessions(userId);

      // Enviar confirmación por email
      await this.emailService.sendCredentialChangeConfirmation(userEmail, 'password');

      this.logger.log(`Password changed successfully for user ${userId}`);

      return {
        success: true,
        message: 'Password changed successfully. You have been logged out from all devices.',
        credentialType: 'password',
      };
    } catch (error) {
      if (error instanceof CredentialChangeException) {
        throw error;
      }
      const errorMessage = error instanceof Error ? error.message : String(error);
      this.logger.error(`Failed to confirm password change: ${errorMessage}`);
      throw new CredentialChangeException('Failed to confirm password change');
    }
  }

  /**
   * Confirma cambio de email
   */
  async confirmEmailChange(userId: string, currentEmail: string, newEmail: string, dto: ConfirmEmailChangeDto): Promise<ChangeCredentialResponseDto> {
    try {
      // Verificar OTP
      const isOTPValid = await this.otpService.verifyOTP(userId, dto.verificationCode);

      if (!isOTPValid) {
        throw new CredentialChangeException('OTP verification failed');
      }

      // Cambiar email en .NET
      const changeResult = await this.changeEmailInCore(userId, newEmail);

      if (!changeResult.success) {
        throw new CredentialChangeException(changeResult.message || 'Failed to change email in core service');
      }

      // Invalidar todas las sesiones después del cambio exitoso (AC4.4)
      await this.invalidateAllSessions(userId);

      // Enviar confirmación al email antiguo
      await this.emailService.sendCredentialChangeConfirmation(currentEmail, 'email', newEmail);

      // Enviar confirmación al nuevo email
      await this.emailService.sendCredentialChangeConfirmation(newEmail, 'email', newEmail);

      this.logger.log(`Email changed successfully for user ${userId}, from ${currentEmail} to ${newEmail}`);

      return {
        success: true,
        message: `Email changed successfully to ${newEmail}. You have been logged out from all devices for security.`,
        credentialType: 'email',
      };
    } catch (error) {
      if (error instanceof CredentialChangeException) {
        throw error;
      }
      const errorMessage = error instanceof Error ? error.message : String(error);
      this.logger.error(`Failed to confirm email change: ${errorMessage}`);
      throw new CredentialChangeException('Failed to confirm email change');
    }
  }

  /**
   * Obtiene estado actual de verificación
   */
  async getVerificationStatus(userId: string): Promise<VerificationStatusDto> {
    const otpInfo = await this.otpService.getOTPInfo(userId);
    const credentialType = await this.otpService.getOTPCredentialType(userId);

    return {
      isVerificationPending: otpInfo !== null,
      credentialType: credentialType || undefined,
      attemptsRemaining: otpInfo?.attemptsRemaining || 0,
      expiresIn: otpInfo?.expiresIn || undefined,
    };
  }

  /**
   * Reenvía código OTP
   */
  async resendOTP(userId: string, userEmail: string, credentialType: 'password' | 'email'): Promise<OTPSentResponseDto> {
    try {
      // Si ya existe un OTP, esperar antes de reenviar
      const existingOTP = await this.otpService.getOTPInfo(userId);

      if (existingOTP) {
        throw new CredentialChangeException(
          `Code already sent. Please wait ${existingOTP.expiresIn} seconds before requesting a new code.`,
        );
      }

      // Generar nuevo OTP
      const { code, expiresIn } = await this.otpService.createOTP(userId, credentialType);

      // Enviar por email
      await this.emailService.sendOTPEmail(userEmail, code, credentialType, Math.ceil(expiresIn / 60));

      this.logger.log(`OTP resent for user ${userId}, type: ${credentialType}`);

      return {
        success: true,
        message: 'New verification code sent to your email',
        expiresIn,
        destination: this.maskEmail(userEmail),
      };
    } catch (error) {
      if (error instanceof CredentialChangeException) {
        throw error;
      }
      const errorMessage = error instanceof Error ? error.message : String(error);
      this.logger.error(`Failed to resend OTP: ${errorMessage}`);
      throw new CredentialChangeException('Failed to resend verification code');
    }
  }

  // ============ PRIVATE HELPER METHODS ============

  /**
   * Valida contraseña actual contra .NET
   */
  private async validateCurrentPassword(userId: string, password: string): Promise<boolean> {
    try {
      const response = await firstValueFrom(
        this.httpService.post(
          `${this.coreApiUrl}/api/v1/auth/validate-password`,
          {
            userId,
            password,
          },
          {
            timeout: this.coreApiTimeout,
            headers: {
              'Content-Type': 'application/json',
            },
          },
        ),
      );

      return response.data?.success || response.data?.isValid || false;
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : String(error);
      this.logger.error(`Error validating password against core API: ${errorMessage}`);
      return false;
    }
  }

  /**
   * Verifica si un email ya está registrado
   */
  private async checkEmailExists(email: string): Promise<boolean> {
    try {
      const response = await firstValueFrom(
        this.httpService.get(`${this.coreApiUrl}/api/v1/auth/email-exists`, {
          params: { email },
          timeout: this.coreApiTimeout,
        }),
      );

      return response.data?.exists || false;
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : String(error);
      this.logger.warn(`Error checking email existence: ${errorMessage}`);
      return false;
    }
  }

  /**
   * Cambia contraseña en .NET
   */
  private async changePasswordInCore(userId: string, newPassword: string): Promise<{ success: boolean; message?: string }> {
    try {
      const response = await firstValueFrom(
        this.httpService.post(
          `${this.coreApiUrl}/api/v1/auth/change-password`,
          {
            userId,
            newPassword,
          },
          {
            timeout: this.coreApiTimeout,
          },
        ),
      );

      return {
        success: response.data?.success || response.status === 200,
        message: response.data?.message,
      };
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : String(error);
      this.logger.error(`Error changing password in core: ${errorMessage}`);
      return {
        success: false,
        message: errorMessage,
      };
    }
  }

  /**
   * Cambia email en .NET
   */
  private async changeEmailInCore(userId: string, newEmail: string): Promise<{ success: boolean; message?: string }> {
    try {
      const response = await firstValueFrom(
        this.httpService.post(
          `${this.coreApiUrl}/api/v1/auth/change-email`,
          {
            userId,
            newEmail,
          },
          {
            timeout: this.coreApiTimeout,
          },
        ),
      );

      return {
        success: response.data?.success || response.status === 200,
        message: response.data?.message,
      };
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : String(error);
      this.logger.error(`Error changing email in core: ${errorMessage}`);
      return {
        success: false,
        message: errorMessage,
      };
    }
  }

  /**
   * Invalida todas las sesiones del usuario
   */
  private async invalidateAllSessions(userId: string): Promise<void> {
    try {
      await firstValueFrom(
        this.httpService.post(
          `${this.coreApiUrl}/api/v1/auth/invalidate-sessions`,
          { userId },
          { timeout: this.coreApiTimeout },
        ),
      );

      this.logger.log(`Sessions invalidated for user ${userId}`);
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : String(error);
      this.logger.warn(`Failed to invalidate sessions: ${errorMessage}`);
      // No lanzar error, no es crítico
    }
  }

  /**
   * Mascara email para logging
   */
  private maskEmail(email: string): string {
    const [localPart, domain] = email.split('@');
    const visibleChars = Math.max(1, Math.ceil(localPart.length / 3));
    const maskedLocal = localPart.substring(0, visibleChars) + '*'.repeat(localPart.length - visibleChars);
    return `${maskedLocal}@${domain}`;
  }
}
