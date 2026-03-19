import { Test, TestingModule } from '@nestjs/testing';
import { HttpService } from '@nestjs/axios';
import { ConfigService } from '@nestjs/config';
import { of } from 'rxjs';
import { AuthService } from './auth.service';
import { OTPService } from './otp.service';
import { EmailService } from './email.service';
import {
  InitiatePasswordChangeDto,
  ConfirmPasswordChangeDto,
} from './auth.dto';
import {
  InvalidCurrentPasswordException,
  CredentialChangeException,
} from './auth.exceptions';

describe('AuthService', () => {
  let service: AuthService;
  let otpService: OTPService;
  let emailService: EmailService;
  let httpService: HttpService;
  let configService: ConfigService;

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
              if (key === 'CORE_API_URL') return 'http://localhost:5000';
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

  describe('initiatePasswordChange', () => {
    it('should initiate password change successfully', async () => {
      const userId = 'user-123';
      const userEmail = 'user@example.com';
      const dto: InitiatePasswordChangeDto = { currentPassword: 'TestPass123!' };

      jest.spyOn(httpService, 'post').mockReturnValue(
        of({ data: { success: true, isValid: true }, status: 200 } as any),
      );
      jest.spyOn(otpService, 'createOTP').mockResolvedValue({
        code: '123456',
        expiresIn: 900,
      });
      jest.spyOn(emailService, 'sendOTPEmail').mockResolvedValue(true);

      const result = await service.initiatePasswordChange(userId, userEmail, dto);

      expect(result).toHaveProperty('success', true);
      expect(result).toHaveProperty('message');
      expect(result).toHaveProperty('destination');
      expect(otpService.createOTP).toHaveBeenCalledWith(userId, 'password');
      expect(emailService.sendOTPEmail).toHaveBeenCalled();
    });

    it('should throw InvalidCurrentPasswordException when password is invalid', async () => {
      const userId = 'user-123';
      const userEmail = 'user@example.com';
      const dto: InitiatePasswordChangeDto = { currentPassword: 'WrongPassword' };

      jest.spyOn(httpService, 'post').mockReturnValue(
        of({ data: { success: false, isValid: false }, status: 200 } as any),
      );

      await expect(service.initiatePasswordChange(userId, userEmail, dto)).rejects.toThrow(
        InvalidCurrentPasswordException,
      );
    });
  });

  describe('confirmPasswordChange', () => {
    it('should confirm password change successfully', async () => {
      const userId = 'user-123';
      const userEmail = 'user@example.com';
      const dto: ConfirmPasswordChangeDto = {
        verificationCode: '123456',
        newPassword: 'NewPass123!',
      };

      jest.spyOn(otpService, 'verifyOTP').mockResolvedValue(true);
      jest.spyOn(httpService, 'post').mockReturnValue(
        of({ data: { success: true }, status: 200 } as any),
      );
      jest.spyOn(emailService, 'sendCredentialChangeConfirmation').mockResolvedValue(true);

      const result = await service.confirmPasswordChange(userId, userEmail, dto);

      expect(result).toHaveProperty('success', true);
      expect(result).toHaveProperty('message');
      expect(otpService.verifyOTP).toHaveBeenCalledWith(userId, dto.verificationCode);
    });

    it('should throw error when OTP verification fails', async () => {
      const userId = 'user-123';
      const userEmail = 'user@example.com';
      const dto: ConfirmPasswordChangeDto = {
        verificationCode: 'invalid',
        newPassword: 'NewPass123!',
      };

      jest
        .spyOn(otpService, 'verifyOTP')
        .mockRejectedValue(new Error('Invalid OTP'));

      await expect(service.confirmPasswordChange(userId, userEmail, dto)).rejects.toThrow(
        CredentialChangeException,
      );
    });
  });

  describe('getVerificationStatus', () => {
    it('should return verification status with pending OTP', async () => {
      const userId = 'user-123';

      jest.spyOn(otpService, 'getOTPInfo').mockResolvedValue({
        expiresIn: 600,
        attemptsRemaining: 2,
      });

      jest.spyOn(otpService, 'getOTPCredentialType').mockResolvedValue('password');

      const result = await service.getVerificationStatus(userId);

      expect(result).toHaveProperty('isVerificationPending', true);
      expect(result).toHaveProperty('attemptsRemaining', 2);
    });
  });
});
