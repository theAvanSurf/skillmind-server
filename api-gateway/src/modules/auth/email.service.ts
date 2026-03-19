import { Injectable, Logger } from '@nestjs/common';
import { MailerService } from '@nestjs-modules/mailer';

@Injectable()
export class EmailService {
  private readonly logger = new Logger(EmailService.name);

  constructor(private readonly mailerService: MailerService) {}

  /**
   * Normalizador de email para mostrar versión reducida
   */
  private maskEmail(email: string): string {
    const [localPart, domain] = email.split('@');
    const visibleChars = Math.max(1, Math.ceil(localPart.length / 3));
    const maskedLocal = localPart.substring(0, visibleChars) + '*'.repeat(localPart.length - visibleChars);
    return `${maskedLocal}@${domain}`;
  }

  /**
   * Envía código OTP por email
   */
  async sendOTPEmail(
    toEmail: string,
    code: string,
    credentialType: 'password' | 'email',
    expirationMinutes: number = 15,
  ): Promise<boolean> {
    try {
      const subject = credentialType === 'password' 
        ? 'Verify Your Password Change - SkillMind' 
        : 'Verify Your Email Change - SkillMind';

      const message = credentialType === 'password'
        ? 'Someone requested to change your password. If this was you, use the code below to confirm.'
        : 'Someone requested to change your email. If this was you, use the code below to confirm.';

      await this.mailerService.sendMail({
        to: toEmail,
        subject,
        template: 'otp-verification',
        context: {
          code,
          message,
          expirationMinutes,
          actionType: credentialType === 'password' ? 'Password Change' : 'Email Change',
          supportEmail: 'support@skillmind.com',
        },
      });

      this.logger.log(`OTP email sent successfully to ${this.maskEmail(toEmail)} for ${credentialType} change`);
      return true;
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : String(error);
      this.logger.error(`Failed to send OTP email to ${toEmail}: ${errorMessage}`);
      throw error;
    }
  }

  /**
   * Envía confirmación de cambio exitoso
   */
  async sendCredentialChangeConfirmation(
    toEmail: string,
    credentialType: 'password' | 'email',
    newValue?: string,
  ): Promise<boolean> {
    try {
      const subject = credentialType === 'password'
        ? 'Your Password Has Been Changed - SkillMind'
        : 'Your Email Has Been Changed - SkillMind';

      const message = credentialType === 'password'
        ? 'Your password has been successfully changed.'
        : `Your email has been successfully changed to ${newValue}.`;

      await this.mailerService.sendMail({
        to: toEmail,
        subject,
        template: 'credential-change-confirmation',
        context: {
          message,
          credentialType,
          newValue: credentialType === 'email' ? newValue : undefined,
          timestamp: new Date().toLocaleString(),
          supportEmail: 'support@skillmind.com',
        },
      });

      this.logger.log(`Confirmation email sent to ${this.maskEmail(toEmail)}`);
      return true;
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : String(error);
      this.logger.error(`Failed to send confirmation email to ${toEmail}: ${errorMessage}`);
      throw error;
    }
  }

  /**
   * Envía alerta de cambio de credencial fallido
   */
  async sendFailedAttemptAlert(
    toEmail: string,
    credentialType: 'password' | 'email',
    reason: string,
  ): Promise<boolean> {
    try {
      await this.mailerService.sendMail({
        to: toEmail,
        subject: 'Security Alert: Failed Credential Change Attempt - SkillMind',
        template: 'security-alert',
        context: {
          credentialType,
          reason,
          actionRequired: 'If this was not you, please secure your account immediately.',
          supportEmail: 'support@skillmind.com',
          timestamp: new Date().toLocaleString(),
        },
      });

      this.logger.log(`Security alert sent to ${this.maskEmail(toEmail)}`);
      return true;
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : String(error);
      this.logger.error(`Failed to send security alert to ${toEmail}: ${errorMessage}`);
      throw error;
    }
  }
}
