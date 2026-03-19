import { Test, TestingModule } from '@nestjs/testing';
import { MailerService } from '@nestjs-modules/mailer';
import { EmailService } from './email.service';

describe('EmailService', () => {
  let service: EmailService;
  let mailerService: MailerService;

  beforeEach(async () => {
    const module: TestingModule = await Test.createTestingModule({
      providers: [
        EmailService,
        {
          provide: MailerService,
          useValue: {
            sendMail: jest.fn().mockResolvedValue(true),
          },
        },
      ],
    }).compile();

    service = module.get<EmailService>(EmailService);
    mailerService = module.get<MailerService>(MailerService);
  });

  afterEach(() => {
    jest.clearAllMocks();
  });

  describe('sendOTPEmail', () => {
    it('should send OTP email for password change', async () => {
      const toEmail = 'user@example.com';
      const code = '123456';

      const result = await service.sendOTPEmail(toEmail, code, 'password', 15);

      expect(result).toBe(true);
      expect(mailerService.sendMail).toHaveBeenCalledWith(
        expect.objectContaining({
          to: toEmail,
          subject: expect.stringContaining('Password Change'),
          template: 'otp-verification',
        }),
      );
    });

    it('should send OTP email for email change', async () => {
      const toEmail = 'user@example.com';
      const code = '654321';

      const result = await service.sendOTPEmail(toEmail, code, 'email', 15);

      expect(result).toBe(true);
      expect(mailerService.sendMail).toHaveBeenCalledWith(
        expect.objectContaining({
          to: toEmail,
          subject: expect.stringContaining('Email Change'),
          template: 'otp-verification',
        }),
      );
    });

    it('should throw error when email sending fails', async () => {
      const toEmail = 'invalid@example.com';
      const code = '123456';

      jest
        .spyOn(mailerService, 'sendMail')
        .mockRejectedValue(new Error('SMTP Error'));

      await expect(service.sendOTPEmail(toEmail, code, 'password', 15)).rejects.toThrow();
    });
  });

  describe('sendCredentialChangeConfirmation', () => {
    it('should send password change confirmation', async () => {
      const toEmail = 'user@example.com';

      const result = await service.sendCredentialChangeConfirmation(toEmail, 'password');

      expect(result).toBe(true);
      expect(mailerService.sendMail).toHaveBeenCalledWith(
        expect.objectContaining({
          to: toEmail,
          subject: expect.stringContaining('Password'),
          template: 'credential-change-confirmation',
        }),
      );
    });

    it('should send email change confirmation with new email', async () => {
      const toEmail = 'user@example.com';
      const newEmail = 'newemail@example.com';

      const result = await service.sendCredentialChangeConfirmation(toEmail, 'email', newEmail);

      expect(result).toBe(true);
      expect(mailerService.sendMail).toHaveBeenCalledWith(
        expect.objectContaining({
          to: toEmail,
          template: 'credential-change-confirmation',
          context: expect.objectContaining({
            newValue: newEmail,
          }),
        }),
      );
    });
  });

  describe('sendFailedAttemptAlert', () => {
    it('should send failed attempt alert', async () => {
      const toEmail = 'user@example.com';
      const reason = 'Invalid OTP code';

      const result = await service.sendFailedAttemptAlert(toEmail, 'password', reason);

      expect(result).toBe(true);
      expect(mailerService.sendMail).toHaveBeenCalledWith(
        expect.objectContaining({
          to: toEmail,
          subject: expect.stringContaining('Security Alert'),
          template: 'security-alert',
        }),
      );
    });
  });
});
