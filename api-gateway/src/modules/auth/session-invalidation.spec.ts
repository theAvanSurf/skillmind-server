import { Test, TestingModule } from '@nestjs/testing';
import { HttpService } from '@nestjs/axios';
import { ConfigService } from '@nestjs/config';
import { of, throwError } from 'rxjs';
import { AuthService } from './auth.service';
import { OTPService } from './otp.service';
import { EmailService } from './email.service';
import {
  ConfirmPasswordChangeDto,
  ConfirmEmailChangeDto,
} from './auth.dto';
import { CredentialChangeException } from './auth.exceptions';

/**
 * Session Invalidation Tests - AC4.4
 * 
 * Verifica que después de un cambio exitoso de credenciales,
 * todas las sesiones activas del usuario se invaliden.
 * 
 * Requirements:
 * - All active sessions must be invalidated after credential change
 * - User must re-authenticate on next request
 * - Session invalidation must happen automatically, not fail the main operation
 */
describe('Session Invalidation (AC4.4)', () => {
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

  describe('Session Invalidation after Password Change', () => {
    it('should invalidate all sessions after successful password change', async () => {
      // Arrange
      const dto: ConfirmPasswordChangeDto = {
        verificationCode: '123456',
        newPassword: 'NewSecurePass123!',
      };

      const invalidateSessionsCall = jest.fn();
      const invalidateSessionsResponse = { data: { success: true }, status: 200 };

      jest.spyOn(otpService, 'verifyOTP').mockResolvedValue(true);
      jest
        .spyOn(httpService, 'post')
        .mockImplementation((url: string, data: any, config: any) => {
          if (url.includes('change-password')) {
            return of({ data: { success: true }, status: 200 });
          }
          if (url.includes('invalidate-sessions')) {
            invalidateSessionsCall();
            return of(invalidateSessionsResponse);
          }
          return of({ data: {}, status: 200 });
        });
      jest.spyOn(emailService, 'sendCredentialChangeConfirmation').mockResolvedValue(true);

      // Act
      const result = await service.confirmPasswordChange(userId, userEmail, dto);

      // Assert
      expect(result.success).toBe(true);
      expect(invalidateSessionsCall).toHaveBeenCalled();
      expect(httpService.post).toHaveBeenCalledWith(
        `${coreApiUrl}/api/v1/auth/invalidate-sessions`,
        { userId },
        expect.any(Object),
      );
    });

    it('should call invalidate-sessions endpoint with correct userId', async () => {
      // Arrange
      const dto: ConfirmPasswordChangeDto = {
        verificationCode: '123456',
        newPassword: 'NewSecurePass123!',
      };
      const testUserId = 'special-user-999';

      jest.spyOn(otpService, 'verifyOTP').mockResolvedValue(true);
      jest.spyOn(httpService, 'post').mockReturnValue(of({ data: { success: true }, status: 200 }));
      jest.spyOn(emailService, 'sendCredentialChangeConfirmation').mockResolvedValue(true);

      // Act
      await service.confirmPasswordChange(testUserId, userEmail, dto);

      // Assert - Verify invalidate-sessions was called with correct userId
      const invalidateCall = (httpService.post as jest.Mock).mock.calls.find((call) =>
        call[0].includes('invalidate-sessions'),
      );
      expect(invalidateCall).toBeDefined();
      expect(invalidateCall[1]).toEqual({ userId: testUserId });
    });

    it('should not throw error even if session invalidation fails', async () => {
      // Arrange
      const dto: ConfirmPasswordChangeDto = {
        verificationCode: '123456',
        newPassword: 'NewSecurePass123!',
      };

      jest.spyOn(otpService, 'verifyOTP').mockResolvedValue(true);
      jest.spyOn(httpService, 'post').mockImplementation((url: string) => {
        if (url.includes('invalidate-sessions')) {
          return throwError(() => new Error('Session invalidation failed'));
        }
        return of({ data: { success: true }, status: 200 });
      });
      jest.spyOn(emailService, 'sendCredentialChangeConfirmation').mockResolvedValue(true);

      // Act & Assert
      expect(async () => {
        await service.confirmPasswordChange(userId, userEmail, dto);
      }).not.toThrow();

      const result = await service.confirmPasswordChange(userId, userEmail, dto);
      expect(result.success).toBe(true);
    });

    it('should log warning if session invalidation fails', async () => {
      // Arrange
      const dto: ConfirmPasswordChangeDto = {
        verificationCode: '123456',
        newPassword: 'NewSecurePass123!',
      };

      const loggerWarnSpy = jest.spyOn(service['logger'], 'warn').mockImplementation();

      jest.spyOn(otpService, 'verifyOTP').mockResolvedValue(true);
      jest.spyOn(httpService, 'post').mockImplementation((url: string) => {
        if (url.includes('invalidate-sessions')) {
          return throwError(() => new Error('Connection timeout'));
        }
        return of({ data: { success: true }, status: 200 });
      });
      jest.spyOn(emailService, 'sendCredentialChangeConfirmation').mockResolvedValue(true);

      // Act
      await service.confirmPasswordChange(userId, userEmail, dto);

      // Assert
      expect(loggerWarnSpy).toHaveBeenCalledWith(
        expect.stringContaining('Failed to invalidate sessions'),
      );
    });
  });

  describe('Session Invalidation after Email Change', () => {
    it('should invalidate all sessions after successful email change', async () => {
      // Arrange
      const dto: ConfirmEmailChangeDto = {
        verificationCode: '123456',
      };

      const invalidateSessionsCall = jest.fn();

      jest.spyOn(otpService, 'verifyOTP').mockResolvedValue(true);
      jest.spyOn(httpService, 'post').mockImplementation((url: string, data: any) => {
        if (url.includes('change-email')) {
          return of({ data: { success: true }, status: 200 });
        }
        if (url.includes('invalidate-sessions')) {
          invalidateSessionsCall();
          return of({ data: { success: true }, status: 200 });
        }
        return of({ data: {}, status: 200 });
      });
      jest.spyOn(emailService, 'sendCredentialChangeConfirmation').mockResolvedValue(true);

      // Act
      const result = await service.confirmEmailChange(userId, userEmail, newEmail, dto);

      // Assert
      expect(result.success).toBe(true);
      expect(invalidateSessionsCall).toHaveBeenCalled();
    });

    it('should send confirmation emails before and after session invalidation', async () => {
      // Arrange
      const dto: ConfirmEmailChangeDto = {
        verificationCode: '123456',
      };

      const emailSendCalls: any[] = [];

      jest.spyOn(otpService, 'verifyOTP').mockResolvedValue(true);
      jest.spyOn(httpService, 'post').mockReturnValue(of({ data: { success: true }, status: 200 }));
      jest.spyOn(emailService, 'sendCredentialChangeConfirmation').mockImplementation((email, type) => {
        emailSendCalls.push({ email, type });
        return Promise.resolve(true);
      });

      // Act
      await service.confirmEmailChange(userId, userEmail, newEmail, dto);

      // Assert - Should send confirmation to both old and new email
      expect(emailService.sendCredentialChangeConfirmation).toHaveBeenCalledTimes(2);
      expect(emailSendCalls).toEqual(
        expect.arrayContaining([
          { email: userEmail, type: 'email' },
          { email: newEmail, type: 'email' },
        ]),
      );
    });
  });

  describe('Session Invalidation Timeout Handling', () => {
    it('should handle timeout from invalidate-sessions endpoint', async () => {
      // Arrange
      const dto: ConfirmPasswordChangeDto = {
        verificationCode: '123456',
        newPassword: 'NewSecurePass123!',
      };

      jest.spyOn(otpService, 'verifyOTP').mockResolvedValue(true);
      jest.spyOn(httpService, 'post').mockImplementation((url: string) => {
        if (url.includes('invalidate-sessions')) {
          return throwError(() => new Error('Request timeout'));
        }
        return of({ data: { success: true }, status: 200 });
      });
      jest.spyOn(emailService, 'sendCredentialChangeConfirmation').mockResolvedValue(true);

      // Act & Assert
      const result = await service.confirmPasswordChange(userId, userEmail, dto);
      expect(result.success).toBe(true); // Should still succeed
      expect(result.message).toContain('logged out from all devices');
    });

    it('should handle network errors gracefully', async () => {
      // Arrange
      const dto: ConfirmPasswordChangeDto = {
        verificationCode: '123456',
        newPassword: 'NewSecurePass123!',
      };

      jest.spyOn(otpService, 'verifyOTP').mockResolvedValue(true);
      jest.spyOn(httpService, 'post').mockImplementation((url: string) => {
        if (url.includes('invalidate-sessions')) {
          return throwError(() => new Error('ERR_NETWORK: Network error'));
        }
        return of({ data: { success: true }, status: 200 });
      });
      jest.spyOn(emailService, 'sendCredentialChangeConfirmation').mockResolvedValue(true);

      // Act & Assert
      const result = await service.confirmPasswordChange(userId, userEmail, dto);
      expect(result.success).toBe(true);
    });
  });

  describe('Concurrent Session Invalidations', () => {
    it('should handle multiple concurrent session invalidations', async () => {
      // Arrange
      const dto: ConfirmPasswordChangeDto = {
        verificationCode: '123456',
        newPassword: 'NewSecurePass123!',
      };

      const invalidationCalls: string[] = [];

      jest.spyOn(otpService, 'verifyOTP').mockResolvedValue(true);
      jest.spyOn(httpService, 'post').mockImplementation((url: string, data: any) => {
        if (url.includes('invalidate-sessions')) {
          invalidationCalls.push(data.userId);
          return of({ data: { success: true }, status: 200 });
        }
        return of({ data: { success: true }, status: 200 });
      });
      jest.spyOn(emailService, 'sendCredentialChangeConfirmation').mockResolvedValue(true);

      // Act - Simulate multiple password changes
      const userIds = ['user-1', 'user-2', 'user-3'];
      await Promise.all(
        userIds.map((uid) =>
          service.confirmPasswordChange(uid, `${uid}@example.com`, dto),
        ),
      );

      // Assert
      expect(invalidationCalls).toEqual(expect.arrayContaining(userIds));
      expect(invalidationCalls.length).toBe(userIds.length);
    });
  });

  describe('Session Invalidation Response Format', () => {
    it('should return correctly formatted response indicating session logout', async () => {
      // Arrange
      const dto: ConfirmPasswordChangeDto = {
        verificationCode: '123456',
        newPassword: 'NewSecurePass123!',
      };

      jest.spyOn(otpService, 'verifyOTP').mockResolvedValue(true);
      jest.spyOn(httpService, 'post').mockReturnValue(of({ data: { success: true }, status: 200 }));
      jest.spyOn(emailService, 'sendCredentialChangeConfirmation').mockResolvedValue(true);

      // Act
      const result = await service.confirmPasswordChange(userId, userEmail, dto);

      // Assert
      expect(result).toHaveProperty('success', true);
      expect(result).toHaveProperty('message');
      expect(result.message).toContain('logged out from all devices');
      expect(result).toHaveProperty('credentialType', 'password');
    });

    it("should indicate in message that user's other sessions are invalidated", async () => {
      // Arrange
      const dto: ConfirmPasswordChangeDto = {
        verificationCode: '123456',
        newPassword: 'NewSecurePass123!',
      };

      jest.spyOn(otpService, 'verifyOTP').mockResolvedValue(true);
      jest.spyOn(httpService, 'post').mockReturnValue(of({ data: { success: true }, status: 200 }));
      jest.spyOn(emailService, 'sendCredentialChangeConfirmation').mockResolvedValue(true);

      // Act
      const result = await service.confirmPasswordChange(userId, userEmail, dto);

      // Assert
      expect(result.message).toMatch(/logged out|session|device/i);
    });
  });
});
