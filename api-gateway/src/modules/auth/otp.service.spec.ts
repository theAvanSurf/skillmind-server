import { Test, TestingModule } from '@nestjs/testing';
import { CACHE_MANAGER } from '@nestjs/cache-manager';
import { OTPService } from './otp.service';
import { InvalidOTPException, OTPExpiredException, MaxOTPAttemptsExceededException } from './auth.exceptions';

describe('OTPService', () => {
  let service: OTPService;
  let cacheManager: any;

  beforeEach(async () => {
    // Mock CACHE_MANAGER
    cacheManager = {
      get: jest.fn(),
      set: jest.fn(),
      del: jest.fn(),
    };

    const module: TestingModule = await Test.createTestingModule({
      providers: [OTPService, { provide: CACHE_MANAGER, useValue: cacheManager }],
    }).compile();

    service = module.get<OTPService>(OTPService);
  });

  afterEach(() => {
    jest.clearAllMocks();
  });

  describe('createOTP', () => {
    it('should create and store OTP successfully', async () => {
      const userId = 'test-user-123';
      const credentialType = 'password';

      cacheManager.get.mockResolvedValue(null); // No previous OTP

      const result = await service.createOTP(userId, credentialType);

      expect(result).toHaveProperty('code');
      expect(result).toHaveProperty('expiresIn');
      expect(result.code).toMatch(/^\d{6}$/); // 6-digit code
      expect(result.expiresIn).toBe(15 * 60); // 15 minutes in seconds
      expect(cacheManager.del).toHaveBeenCalled(); // Old OTP deleted
      expect(cacheManager.set).toHaveBeenCalled(); // New OTP stored
    });

    it('should throw error when rate limit exceeded', async () => {
      const userId = 'test-user-123';
      const credentialType = 'password';

      // Simulate rate limit hit
      cacheManager.get.mockResolvedValue(3); // 3 requests = limit reached

      await expect(service.createOTP(userId, credentialType)).rejects.toThrow();
    });
  });

  describe('verifyOTP', () => {
    it('should verify valid OTP successfully', async () => {
      const userId = 'test-user-123';
      const code = '123456';
      const now = Date.now();
      const otpData = {
        code,
        credentialType: 'password',
        attempts: 0,
        createdAt: now,
        expiresAt: now + 15 * 60 * 1000,
      };

      cacheManager.get.mockResolvedValue(JSON.stringify(otpData));

      const result = await service.verifyOTP(userId, code);

      expect(result).toBe(true);
      expect(cacheManager.del).toHaveBeenCalled(); // OTP deleted after verification
    });

    it('should throw InvalidOTPException for invalid code', async () => {
      const userId = 'test-user-123';
      const invalidCode = '999999';
      const now = Date.now();
      const otpData = {
        code: '123456',
        credentialType: 'password',
        attempts: 0,
        createdAt: now,
        expiresAt: now + 15 * 60 * 1000,
      };

      cacheManager.get.mockResolvedValue(JSON.stringify(otpData));

      await expect(service.verifyOTP(userId, invalidCode)).rejects.toThrow(InvalidOTPException);
    });

    it('should throw OTPExpiredException for expired code', async () => {
      const userId = 'test-user-123';
      const code = '123456';
      const now = Date.now();
      const otpData = {
        code,
        credentialType: 'password',
        attempts: 0,
        createdAt: now - 20 * 60 * 1000, // 20 minutes ago
        expiresAt: now - 5 * 60 * 1000, // Expired 5 minutes ago
      };

      cacheManager.get.mockResolvedValue(JSON.stringify(otpData));

      await expect(service.verifyOTP(userId, code)).rejects.toThrow(OTPExpiredException);
    });

    it('should throw MaxOTPAttemptsExceededException after max attempts', async () => {
      const userId = 'test-user-123';
      const code = '123456';
      const now = Date.now();
      const otpData = {
        code,
        credentialType: 'password',
        attempts: 3, // Max attempts reached
        createdAt: now,
        expiresAt: now + 15 * 60 * 1000,
      };

      cacheManager.get.mockResolvedValue(JSON.stringify(otpData));

      await expect(service.verifyOTP(userId, code)).rejects.toThrow(MaxOTPAttemptsExceededException);
    });
  });

  describe('hasOTPPending', () => {
    it('should return true when OTP is pending and not expired', async () => {
      const userId = 'test-user-123';
      const now = Date.now();
      const otpData = {
        code: '123456',
        credentialType: 'password',
        attempts: 0,
        createdAt: now,
        expiresAt: now + 15 * 60 * 1000,
      };

      cacheManager.get.mockResolvedValue(JSON.stringify(otpData));

      const result = await service.hasOTPPending(userId);

      expect(result).toBe(true);
    });

    it('should return false when no OTP exists', async () => {
      const userId = 'test-user-123';

      cacheManager.get.mockResolvedValue(null);

      const result = await service.hasOTPPending(userId);

      expect(result).toBe(false);
    });
  });

  describe('getOTPInfo', () => {
    it('should return OTP info with attempts remaining', async () => {
      const userId = 'test-user-123';
      const now = Date.now();
      const otpData = {
        code: '123456',
        credentialType: 'password',
        attempts: 1,
        createdAt: now,
        expiresAt: now + 10 * 60 * 1000, // 10 minutes remaining
      };

      cacheManager.get.mockResolvedValue(JSON.stringify(otpData));

      const result = await service.getOTPInfo(userId);

      expect(result).toHaveProperty('expiresIn');
      expect(result).toHaveProperty('attemptsRemaining');
      expect(result!.attemptsRemaining).toBe(2); // 3 max - 1 used = 2 remaining
    });

    it('should return null when no OTP pending', async () => {
      const userId = 'test-user-123';

      cacheManager.get.mockResolvedValue(null);

      const result = await service.getOTPInfo(userId);

      expect(result).toBeNull();
    });
  });
});
