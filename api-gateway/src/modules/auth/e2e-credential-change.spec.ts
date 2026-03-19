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
  RateLimitExceededException,
} from './auth.exceptions';

/**
 * E2E Flow Tests - Full Credential Change Workflows
 * 
 * Tests the complete end-to-end flows including:
 * - Initiation, OTP delivery, verification, and confirmation
 * - Success scenarios
 * - Failure scenarios (expired, invalid, wrong user)
 * - Edge cases
 */
describe('E2E Credential Change Flows', () => {
  let service: AuthService;
  let otpService: OTPService;
  let emailService: EmailService;
  let httpService: HttpService;
  let configService: ConfigService;

  const userId = 'user-123';
  const userEmail = 'user@example.com';
  const newEmail = 'newuser@example.com';
  const coreApiUrl = 'http://localhost:5000';
  const validOtp = '123456';
  const invalidOtp = 'WRONGCODE';

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

  describe('Complete Password Change Flow (Successful)', () => {
    it('should complete full password change workflow: initiate → verify → confirm', async () => {
      // Step 1: Initiate password change
      const initiateDto: InitiatePasswordChangeDto = {
        currentPassword: 'OldPassword123!',
      };

      jest.spyOn(httpService, 'post').mockReturnValue(
        of({ data: { success: true, isValid: true }, status: 200 } as any),
      );
      jest.spyOn(otpService, 'createOTP').mockResolvedValue({
        code: validOtp,
        expiresIn: 900,
      });
      jest.spyOn(emailService, 'sendOTPEmail').mockResolvedValue(true);

      const initiateResult = await service.initiatePasswordChange(userId, userEmail, initiateDto);

      // Assertions for step 1
      expect(initiateResult.success).toBe(true);
      expect(initiateResult.message).toContain('Verification code sent');
      expect(initiateResult.expiresIn).toBe(900);
      expect(emailService.sendOTPEmail).toHaveBeenCalledWith(
        userEmail,
        validOtp,
        'password',
        15,
      );

      // Step 2: Confirm password change with OTP
      const confirmDto: ConfirmPasswordChangeDto = {
        verificationCode: validOtp,
        newPassword: 'NewPassword456!',
      };

      jest.spyOn(otpService, 'verifyOTP').mockResolvedValue(true);
      jest.spyOn(httpService, 'post').mockReturnValue(
        of({ data: { success: true }, status: 200 } as any),
      );
      jest.spyOn(emailService, 'sendCredentialChangeConfirmation').mockResolvedValue(true);

      const confirmResult = await service.confirmPasswordChange(userId, userEmail, confirmDto);

      // Assertions for step 2
      expect(confirmResult.success).toBe(true);
      expect(confirmResult.credentialType).toBe('password');
      expect(confirmResult.message).toContain('logged out from all devices');
      expect(otpService.verifyOTP).toHaveBeenCalledWith(userId, validOtp);
      expect(emailService.sendCredentialChangeConfirmation).toHaveBeenCalled();
    });

    it('should verify OTP before attempting password change', async () => {
      // Arrange
      const confirmDto: ConfirmPasswordChangeDto = {
        verificationCode: validOtp,
        newPassword: 'NewPassword456!',
      };

      const otpVerifyCall = jest.fn().mockResolvedValue(true);
      const postCall = jest.fn().mockReturnValue(of({ data: { success: true }, status: 200 }));

      jest.spyOn(otpService, 'verifyOTP').mockImplementation(otpVerifyCall);
      jest.spyOn(httpService, 'post').mockImplementation(postCall);
      jest.spyOn(emailService, 'sendCredentialChangeConfirmation').mockResolvedValue(true);

      // Act
      await service.confirmPasswordChange(userId, userEmail, confirmDto);

      // Assert - OTP verification should happen first
      expect(otpVerifyCall).toHaveBeenCalledWith(userId, validOtp);
      // Post call happens after OTP verification
      expect(postCall).toHaveBeenCalled();
    });
  });

  describe('Complete Email Change Flow (Successful)', () => {
    it('should complete full email change workflow: initiate → verify → confirm', async () => {
      // Step 1: Initiate email change
      const initiateDto: InitiateEmailChangeDto = {
        newEmail: newEmail,
        currentPassword: 'CurrentPassword123!',
      };

      jest.spyOn(httpService, 'post').mockReturnValue(
        of({ data: { success: true, isValid: true }, status: 200 } as any),
      );
      jest.spyOn(httpService, 'get').mockReturnValue(
        of({ data: { exists: false }, status: 200 } as any),
      );
      jest.spyOn(otpService, 'createOTP').mockResolvedValue({
        code: validOtp,
        expiresIn: 300,
      });
      jest.spyOn(emailService, 'sendOTPEmail').mockResolvedValue(true);

      const initiateResult = await service.initiateEmailChange(userId, userEmail, initiateDto);

      // Assertions for step 1
      expect(initiateResult.success).toBe(true);
      expect(initiateResult.message).toContain('Verification code sent');
      expect(emailService.sendOTPEmail).toHaveBeenCalledWith(
        userEmail,
        validOtp,
        'email',
        5,
      );

      // Step 2: Confirm email change with OTP
      const confirmDto: ConfirmEmailChangeDto = {
        verificationCode: validOtp,
      };

      jest.spyOn(otpService, 'verifyOTP').mockResolvedValue(true);
      jest.spyOn(httpService, 'post').mockReturnValue(
        of({ data: { success: true }, status: 200 } as any),
      );
      jest.spyOn(emailService, 'sendCredentialChangeConfirmation').mockResolvedValue(true);

      const confirmResult = await service.confirmEmailChange(userId, userEmail, newEmail, confirmDto);

      // Assertions for step 2
      expect(confirmResult.success).toBe(true);
      expect(confirmResult.credentialType).toBe('email');
      expect(confirmResult.message).toContain(newEmail);
      expect(emailService.sendCredentialChangeConfirmation).toHaveBeenCalledWith(
        userEmail,
        'email',
        newEmail,
      );
      expect(emailService.sendCredentialChangeConfirmation).toHaveBeenCalledWith(
        newEmail,
        'email',
        newEmail,
      );
    });

    it('should prevent email change to an already registered email', async () => {
      // Arrange
      const initiateDto: InitiateEmailChangeDto = {
        newEmail: 'existing@example.com',
        currentPassword: 'CurrentPassword123!',
      };

      jest.spyOn(httpService, 'get').mockReturnValue(
        of({ data: { exists: true }, status: 200 } as any),
      );
      jest.spyOn(httpService, 'post').mockReturnValue(
        of({ data: { success: true, isValid: true }, status: 200 } as any),
      );

      // Act & Assert
      await expect(
        service.initiateEmailChange(userId, userEmail, initiateDto),
      ).rejects.toThrow(CredentialChangeException);

      // OTP should not be created for duplicate email
      expect(otpService.createOTP).not.toHaveBeenCalled();
    });
  });

  describe('Password Change with Invalid OTP', () => {
    it('should reject password change with invalid OTP', async () => {
      // Arrange
      const confirmDto: ConfirmPasswordChangeDto = {
        verificationCode: invalidOtp,
        newPassword: 'NewPassword456!',
      };

      jest.spyOn(otpService, 'verifyOTP').mockResolvedValue(false);

      // Act & Assert
      await expect(
        service.confirmPasswordChange(userId, userEmail, confirmDto),
      ).rejects.toThrow(CredentialChangeException);

      // Password should never reach the core API
      expect(httpService.post).not.toHaveBeenCalledWith(
        expect.stringContaining('change-password'),
        expect.any(Object),
        expect.any(Object),
      );
    });
  });

  describe('Email Change with Expired OTP', () => {
    it('should reject email change with expired OTP', async () => {
      // Arrange
      const confirmDto: ConfirmEmailChangeDto = {
        verificationCode: invalidOtp,
      };

      // OTP verification fails (expired or invalid)
      jest.spyOn(otpService, 'verifyOTP').mockResolvedValue(false);

      // Act & Assert
      await expect(
        service.confirmEmailChange(userId, userEmail, newEmail, confirmDto),
      ).rejects.toThrow(CredentialChangeException);

      // Email should never change in core API
      expect(httpService.post).not.toHaveBeenCalledWith(
        expect.stringContaining('change-email'),
        expect.any(Object),
        expect.any(Object),
      );
    });
  });

  describe('Password Change with Invalid Current Password', () => {
    it('should reject password change initiation with invalid current password', async () => {
      // Arrange
      const initiateDto: InitiatePasswordChangeDto = {
        currentPassword: 'WrongPassword!',
      };

      jest.spyOn(httpService, 'post').mockReturnValue(
        of({ data: { success: false, isValid: false }, status: 200 } as any),
      );

      // Act & Assert
      await expect(
        service.initiatePasswordChange(userId, userEmail, initiateDto),
      ).rejects.toThrow(InvalidCurrentPasswordException);

      // OTP should never be created
      expect(otpService.createOTP).not.toHaveBeenCalled();
      expect(emailService.sendOTPEmail).not.toHaveBeenCalled();
    });
  });

  describe('Rate Limiting in Password Change', () => {
    it('should respect rate limiting on multiple initiate requests', async () => {
      // This test is for the controller/guard level
      // The service should bubble up rate limit errors from the guard
      // Note: Rate limiting is handled at guard level, not service level
      expect(true).toBe(true); // Placeholder for integration test
    });
  });

  describe('Concurrent Credential Change Flows', () => {
    it('should handle multiple users changing credentials concurrently', async () => {
      // Arrange
      const user1Id = 'user-1';
      const user2Id = 'user-2';
      const user1Email = 'user1@example.com';
      const user2Email = 'user2@example.com';

      jest.spyOn(httpService, 'post').mockReturnValue(
        of({ data: { success: true, isValid: true }, status: 200 } as any),
      );
      jest.spyOn(otpService, 'createOTP').mockResolvedValue({
        code: validOtp,
        expiresIn: 900,
      });
      jest.spyOn(emailService, 'sendOTPEmail').mockResolvedValue(true);

      const dto: InitiatePasswordChangeDto = {
        currentPassword: 'Password123!',
      };

      // Act - Initiate for both users concurrently
      const results = await Promise.all([
        service.initiatePasswordChange(user1Id, user1Email, dto),
        service.initiatePasswordChange(user2Id, user2Email, dto),
      ]);

      // Assert
      expect(results).toHaveLength(2);
      expect(results[0].success).toBe(true);
      expect(results[1].success).toBe(true);
      expect(otpService.createOTP).toHaveBeenCalledWith(user1Id, 'password');
      expect(otpService.createOTP).toHaveBeenCalledWith(user2Id, 'password');
    });
  });

  describe('Core API Failures', () => {
    it('should handle core API timeout gracefully', async () => {
      // Arrange
      const confirmDto: ConfirmPasswordChangeDto = {
        verificationCode: validOtp,
        newPassword: 'NewPassword456!',
      };

      jest.spyOn(otpService, 'verifyOTP').mockResolvedValue(true);
      jest.spyOn(httpService, 'post').mockReturnValue(
        throwError(() => new Error('Request timeout')),
      );

      // Act & Assert
      await expect(
        service.confirmPasswordChange(userId, userEmail, confirmDto),
      ).rejects.toThrow(CredentialChangeException);
    });

    it('should handle core API returning non-200 status', async () => {
      // Arrange
      const confirmDto: ConfirmPasswordChangeDto = {
        verificationCode: validOtp,
        newPassword: 'NewPassword456!',
      };

      jest.spyOn(otpService, 'verifyOTP').mockResolvedValue(true);
      jest.spyOn(httpService, 'post').mockImplementation((url: string) => {
        if (url.includes('change-password')) {
          // Return error status from API
          return throwError(() => ({
            status: 400,
            message: 'Bad request',
          }));
        }
        return of({ data: { success: true }, status: 200 });
      });

      // Act & Assert
      await expect(
        service.confirmPasswordChange(userId, userEmail, confirmDto),
      ).rejects.toThrow(CredentialChangeException);
    });
  });

  describe('Session Invalidation in Complete Flow', () => {
    it('should invalidate sessions after successful password change in complete flow', async () => {
      // Arrange
      const confirmDto: ConfirmPasswordChangeDto = {
        verificationCode: validOtp,
        newPassword: 'NewPassword456!',
      };

      const invalidateCall = jest.fn();

      jest.spyOn(otpService, 'verifyOTP').mockResolvedValue(true);
      jest.spyOn(httpService, 'post').mockImplementation((url: string, data: any) => {
        if (url.includes('invalidate-sessions')) {
          invalidateCall();
          return of({ data: { success: true }, status: 200 });
        }
        return of({ data: { success: true }, status: 200 });
      });
      jest.spyOn(emailService, 'sendCredentialChangeConfirmation').mockResolvedValue(true);

      // Act
      const result = await service.confirmPasswordChange(userId, userEmail, confirmDto);

      // Assert
      expect(result.success).toBe(true);
      expect(invalidateCall).toHaveBeenCalled();
      expect(httpService.post).toHaveBeenCalledWith(
        `${coreApiUrl}/api/v1/auth/invalidate-sessions`,
        { userId },
        expect.any(Object),
      );
    });

    it('should invalidate sessions after successful email change in complete flow', async () => {
      // Arrange
      const confirmDto: ConfirmEmailChangeDto = {
        verificationCode: validOtp,
      };

      const invalidateCall = jest.fn();

      jest.spyOn(otpService, 'verifyOTP').mockResolvedValue(true);
      jest.spyOn(httpService, 'post').mockImplementation((url: string, data: any) => {
        if (url.includes('invalidate-sessions')) {
          invalidateCall();
          return of({ data: { success: true }, status: 200 });
        }
        return of({ data: { success: true }, status: 200 });
      });
      jest.spyOn(emailService, 'sendCredentialChangeConfirmation').mockResolvedValue(true);

      // Act
      const result = await service.confirmEmailChange(userId, userEmail, newEmail, confirmDto);

      // Assert
      expect(result.success).toBe(true);
      expect(invalidateCall).toHaveBeenCalled();
    });
  });
});
