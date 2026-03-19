import { Test, TestingModule } from '@nestjs/testing';
import { HttpService } from '@nestjs/axios';
import { ConfigService } from '@nestjs/config';
import { of, throwError } from 'rxjs';
import { AuthService } from './auth.service';
import { OTPService } from './otp.service';
import { EmailService } from './email.service';
import {
  InitiatePasswordChangeDto,
  ConfirmPasswordChangeDto,
  InitiateEmailChangeDto,
  ConfirmEmailChangeDto,
} from './auth.dto';
import {
  InvalidCurrentPasswordException,
  CredentialChangeException,
} from './auth.exceptions';

/**
 * Audit Logging Tests - AC4.3
 * 
 * Verifica que los intentos de cambio de credenciales se registren
 * en logs con información de seguridad relevante.
 * 
 * Requirements:
 * - Log credential change attempts (initiated)
 * - Log successful changes
 * - Log failed verification attempts
 * - Include timestamp (automatic en logs)
 * - Include IP address (si está disponible en context)
 * - Include user ID
 * - Include credential type
 */
describe('Audit Logging (AC4.3)', () => {
  let service: AuthService;
  let otpService: OTPService;
  let emailService: EmailService;
  let httpService: HttpService;
  let configService: ConfigService;

  const userId = 'user-123';
  const userEmail = 'user@example.com';
  const newEmail = 'newuser@example.com';
  const coreApiUrl = 'http://localhost:5000';

  beforeEach(async () => {
    const module: TestingModule = await Test.createTestingModule({
      providers: [
        AuthService,
        {
          provide: OTPService,
          useValue: {
            createOTP: jest.fn(),
            verifyOTP: jest.fn(),
            getOTPInfo: jest.fn(),
            getOTPCredentialType: jest.fn(),
          },
        },
        {
          provide: EmailService,
          useValue: {
            sendOTPEmail: jest.fn(),
            sendCredentialChangeConfirmation: jest.fn(),
          },
        },
        {
          provide: HttpService,
          useValue: {
            post: jest.fn(),
            get: jest.fn(),
          },
        },
        {
          provide: ConfigService,
          useValue: {
            get: jest.fn((key: string) => {
              if (key === 'CORE_API_URL') return coreApiUrl;
              return process.env[key];
            }),
          },
        },
      ],
    }).compile();

    service = module.get<AuthService>(AuthService);
    otpService = module.get<OTPService>(OTPService);
    emailService = module.get<EmailService>(EmailService);
    httpService = module.get<HttpService>(HttpService);
    configService = module.get<ConfigService>(ConfigService);
  });

  afterEach(() => {
    jest.clearAllMocks();
  });

  describe('Password Change Initiation Logging', () => {
    it('should log when password change is initiated', async () => {
      // Arrange
      const dto: InitiatePasswordChangeDto = { currentPassword: 'TestPass123!' };
      const loggerLogSpy = jest.spyOn(service['logger'], 'log').mockImplementation();

      jest.spyOn(httpService, 'post').mockReturnValue(
        of({ data: { success: true, isValid: true }, status: 200 } as any),
      );
      jest.spyOn(otpService, 'createOTP').mockResolvedValue({
        code: '123456',
        expiresIn: 900,
      });
      jest.spyOn(emailService, 'sendOTPEmail').mockResolvedValue(true);

      // Act
      await service.initiatePasswordChange(userId, userEmail, dto);

      // Assert
      expect(loggerLogSpy).toHaveBeenCalledWith(
        expect.stringContaining(`Password change initiated for user ${userId}`),
      );
    });

    it('should include userId in log for password change initiation', async () => {
      // Arrange
      const dto: InitiatePasswordChangeDto = { currentPassword: 'TestPass123!' };
      const testUserId = 'audit-test-user-999';
      const loggerLogSpy = jest.spyOn(service['logger'], 'log').mockImplementation();

      jest.spyOn(httpService, 'post').mockReturnValue(
        of({ data: { success: true, isValid: true }, status: 200 } as any),
      );
      jest.spyOn(otpService, 'createOTP').mockResolvedValue({
        code: '123456',
        expiresIn: 900,
      });
      jest.spyOn(emailService, 'sendOTPEmail').mockResolvedValue(true);

      // Act
      await service.initiatePasswordChange(testUserId, userEmail, dto);

      // Assert
      expect(loggerLogSpy).toHaveBeenCalledWith(
        expect.stringContaining(testUserId),
      );
    });

    it('should log failed password change initiation with invalid password', async () => {
      // Arrange
      const dto: InitiatePasswordChangeDto = { currentPassword: 'WrongPass123!' };
      const loggerWarnSpy = jest.spyOn(service['logger'], 'warn').mockImplementation();

      jest.spyOn(httpService, 'post').mockReturnValue(
        of({ data: { success: false, isValid: false }, status: 200 } as any),
      );

      // Act & Assert
      try {
        await service.initiatePasswordChange(userId, userEmail, dto);
      } catch (error) {
        expect(error).toBeInstanceOf(InvalidCurrentPasswordException);
      }

      expect(loggerWarnSpy).toHaveBeenCalledWith(
        expect.stringContaining(`Invalid current password attempt for user ${userId}`),
      );
    });
  });

  describe('Email Change Initiation Logging', () => {
    it('should log when email change is initiated', async () => {
      // Arrange
      const dto: InitiateEmailChangeDto = {
        newEmail: newEmail,
        currentPassword: 'TestPass123!',
      };
      const loggerLogSpy = jest.spyOn(service['logger'], 'log').mockImplementation();

      jest.spyOn(httpService, 'post').mockReturnValue(
        of({ data: { success: true, isValid: true }, status: 200 } as any),
      );
      jest.spyOn(httpService, 'get').mockReturnValue(
        of({ data: { exists: false }, status: 200 } as any),
      );
      jest.spyOn(otpService, 'createOTP').mockResolvedValue({
        code: '123456',
        expiresIn: 900,
      });
      jest.spyOn(emailService, 'sendOTPEmail').mockResolvedValue(true);

      // Act
      await service.initiateEmailChange(userId, userEmail, dto);

      // Assert
      expect(loggerLogSpy).toHaveBeenCalledWith(
        expect.stringContaining(`Email change initiated for user ${userId}`),
      );
      expect(loggerLogSpy).toHaveBeenCalledWith(
        expect.stringContaining('new email'),
      );
    });

    it('should include both current and new email in audit log', async () => {
      // Arrange
      const dto: InitiateEmailChangeDto = {
        newEmail: newEmail,
        currentPassword: 'TestPass123!',
      };
      const loggerLogSpy = jest.spyOn(service['logger'], 'log').mockImplementation();

      jest.spyOn(httpService, 'post').mockReturnValue(
        of({ data: { success: true, isValid: true }, status: 200 } as any),
      );
      jest.spyOn(httpService, 'get').mockReturnValue(
        of({ data: { exists: false }, status: 200 } as any),
      );
      jest.spyOn(otpService, 'createOTP').mockResolvedValue({
        code: '123456',
        expiresIn: 900,
      });
      jest.spyOn(emailService, 'sendOTPEmail').mockResolvedValue(true);

      // Act
      await service.initiateEmailChange(userId, userEmail, dto);

      // Assert
      const logCall = loggerLogSpy.mock.calls.find((call) =>
        call[0].includes('Email change initiated'),
      );
      expect(logCall).toBeDefined();
      if (!logCall) {
        throw new Error('Expected audit log call to be present');
      }
      expect(logCall[0]).toContain(userId);
      expect(logCall[0]).toContain(newEmail);
    });

    it('should show error when email is already registered', async () => {
      // Arrange
      const dto: InitiateEmailChangeDto = {
        newEmail: newEmail,
        currentPassword: 'TestPass123!',
      };

      jest.spyOn(httpService, 'get').mockReturnValue(
        of({ data: { exists: true }, status: 200 } as any),
      );
      jest.spyOn(httpService, 'post').mockReturnValue(
        of({ data: { success: true, isValid: true }, status: 200 } as any),
      );

      // Act & Assert
      await expect(
        service.initiateEmailChange(userId, userEmail, dto),
      ).rejects.toThrow(CredentialChangeException);
    });
  });

  describe('Successful Credential Change Logging', () => {
    it('should log successful password change confirmation', async () => {
      // Arrange
      const dto: ConfirmPasswordChangeDto = {
        verificationCode: '123456',
        newPassword: 'NewSecurePass123!',
      };
      const loggerLogSpy = jest.spyOn(service['logger'], 'log').mockImplementation();

      jest.spyOn(otpService, 'verifyOTP').mockResolvedValue(true);
      jest.spyOn(httpService, 'post').mockReturnValue(
        of({ data: { success: true }, status: 200 } as any),
      );
      jest.spyOn(emailService, 'sendCredentialChangeConfirmation').mockResolvedValue(true);

      // Act
      await service.confirmPasswordChange(userId, userEmail, dto);

      // Assert
      expect(loggerLogSpy).toHaveBeenCalledWith(
        expect.stringContaining(`Password changed successfully for user ${userId}`),
      );
    });

    it('should log successful email change confirmation', async () => {
      // Arrange
      const dto: ConfirmEmailChangeDto = {
        verificationCode: '123456',
      };
      const loggerLogSpy = jest.spyOn(service['logger'], 'log').mockImplementation();

      jest.spyOn(otpService, 'verifyOTP').mockResolvedValue(true);
      jest.spyOn(httpService, 'post').mockReturnValue(
        of({ data: { success: true }, status: 200 } as any),
      );
      jest.spyOn(emailService, 'sendCredentialChangeConfirmation').mockResolvedValue(true);

      // Act
      await service.confirmEmailChange(userId, userEmail, newEmail, dto);

      // Assert
      expect(loggerLogSpy).toHaveBeenCalledWith(
        expect.stringContaining(`Email changed successfully for user ${userId}`),
      );
      expect(loggerLogSpy).toHaveBeenCalledWith(
        expect.stringContaining(newEmail),
      );
    });

    it('should log includes credential change type', async () => {
      // Arrange
      const dto: ConfirmPasswordChangeDto = {
        verificationCode: '123456',
        newPassword: 'NewSecurePass123!',
      };
      const loggerLogSpy = jest.spyOn(service['logger'], 'log').mockImplementation();

      jest.spyOn(otpService, 'verifyOTP').mockResolvedValue(true);
      jest.spyOn(httpService, 'post').mockReturnValue(
        of({ data: { success: true }, status: 200 } as any),
      );
      jest.spyOn(emailService, 'sendCredentialChangeConfirmation').mockResolvedValue(true);

      // Act
      await service.confirmPasswordChange(userId, userEmail, dto);

      // Assert
      expect(loggerLogSpy).toHaveBeenCalledWith(
        expect.stringContaining('Password'),
      );
    });
  });

  describe('Failed Verification Logging', () => {
    it('should reject OTP verification when verification fails', async () => {
      // Arrange
      const dto: ConfirmPasswordChangeDto = {
        verificationCode: 'INVALID_CODE',
        newPassword: 'NewSecurePass123!',
      };

      jest.spyOn(otpService, 'verifyOTP').mockResolvedValue(false);

      // Act & Assert
      await expect(service.confirmPasswordChange(userId, userEmail, dto)).rejects.toThrow(
        CredentialChangeException,
      );
    });

    it('should reject core service failures', async () => {
      // Arrange
      const dto: ConfirmPasswordChangeDto = {
        verificationCode: '123456',
        newPassword: 'NewSecurePass123!',
      };

      jest.spyOn(otpService, 'verifyOTP').mockResolvedValue(true);
      jest.spyOn(httpService, 'post').mockImplementation((url: string) => {
        if (url.includes('change-password')) {
          return of({ data: { success: false, message: 'Password does not meet requirements' }, status: 400 } as any);
        }
        return of({ data: { success: true }, status: 200 } as any);
      });

      // Act & Assert
      await expect(service.confirmPasswordChange(userId, userEmail, dto)).rejects.toThrow(
        CredentialChangeException,
      );
    });

    it('should handle network errors and log them', async () => {
      // Arrange
      const dto: ConfirmPasswordChangeDto = {
        verificationCode: '123456',
        newPassword: 'NewSecurePass123!',
      };
      const loggerErrorSpy = jest.spyOn(service['logger'], 'error').mockImplementation();

      jest.spyOn(otpService, 'verifyOTP').mockResolvedValue(true);
      jest.spyOn(httpService, 'post').mockReturnValue(
        throwError(() => new Error('Network timeout')),
      );

      // Act & Assert
      await expect(service.confirmPasswordChange(userId, userEmail, dto)).rejects.toThrow(
        CredentialChangeException,
      );

      // Network error will be logged in the catch block
      expect(loggerErrorSpy).toHaveBeenCalled();
    });
  });

  describe('Audit Log Format and Details', () => {
    it('should include userId in all audit logs', async () => {
      // Arrange
      const dto: InitiatePasswordChangeDto = { currentPassword: 'TestPass123!' };
      const testUserId = 'audit-format-test-123';
      const loggerLogSpy = jest.spyOn(service['logger'], 'log').mockImplementation();
      const loggerErrorSpy = jest.spyOn(service['logger'], 'error').mockImplementation();

      jest.spyOn(httpService, 'post').mockReturnValue(
        of({ data: { success: true, isValid: true }, status: 200 } as any),
      );
      jest.spyOn(otpService, 'createOTP').mockResolvedValue({
        code: '123456',
        expiresIn: 900,
      });
      jest.spyOn(emailService, 'sendOTPEmail').mockResolvedValue(true);

      // Act
      await service.initiatePasswordChange(testUserId, userEmail, dto);

      // Assert
      const allLogs = [...loggerLogSpy.mock.calls, ...loggerErrorSpy.mock.calls];
      allLogs.forEach((logCall) => {
        if (logCall[0].includes('password change') || logCall[0].includes('Password')) {
          expect(logCall[0]).toContain(testUserId);
        }
      });
    });

    it('should include credential type in audit logs', async () => {
      // Arrange
      const dto: InitiateEmailChangeDto = {
        newEmail: newEmail,
        currentPassword: 'TestPass123!',
      };
      const loggerLogSpy = jest.spyOn(service['logger'], 'log').mockImplementation();

      jest.spyOn(httpService, 'post').mockReturnValue(
        of({ data: { success: true, isValid: true }, status: 200 } as any),
      );
      jest.spyOn(httpService, 'get').mockReturnValue(
        of({ data: { exists: false }, status: 200 } as any),
      );
      jest.spyOn(otpService, 'createOTP').mockResolvedValue({
        code: '123456',
        expiresIn: 900,
      });
      jest.spyOn(emailService, 'sendOTPEmail').mockResolvedValue(true);

      // Act
      await service.initiateEmailChange(userId, userEmail, dto);

      // Assert
      expect(loggerLogSpy).toHaveBeenCalledWith(
        expect.stringContaining('email'),
      );
    });
  });

  describe('Audit Log Severity Levels', () => {
    it('should use warn level for invalid credentials', async () => {
      // Arrange
      const loggerWarnSpy = jest.spyOn(service['logger'], 'warn').mockImplementation();

      jest.spyOn(httpService, 'post').mockReturnValue(
        of({ data: { success: false, isValid: false }, status: 200 } as any),
      );

      // Act - Try to initiate with invalid password
      await expect(
        service.initiatePasswordChange(userId, userEmail, {
          currentPassword: 'WrongPass',
        }),
      ).rejects.toThrow(InvalidCurrentPasswordException);

      // Assert
      // Invalid password should be WARN level (security concern)
      expect(loggerWarnSpy).toHaveBeenCalledWith(
        expect.stringContaining('Invalid current password'),
      );
    });

    it('should use error level for service failures', async () => {
      // Arrange
      const dto: ConfirmPasswordChangeDto = {
        verificationCode: '123456',
        newPassword: 'NewSecurePass123!',
      };
      const loggerErrorSpy = jest.spyOn(service['logger'], 'error').mockImplementation();

      jest.spyOn(otpService, 'verifyOTP').mockResolvedValue(true);
      jest.spyOn(httpService, 'post').mockReturnValue(
        throwError(() => new Error('Internal server error')),
      );

      // Act & Assert
      await expect(
        service.confirmPasswordChange(userId, userEmail, dto),
      ).rejects.toThrow(CredentialChangeException);

      expect(loggerErrorSpy).toHaveBeenCalled();
    });
  });
});
