import { Test, TestingModule } from '@nestjs/testing';
import { INestApplication, HttpStatus } from '@nestjs/common';
import request from 'supertest';
import { AppModule } from '../src/app.module';
import { AuthService } from '../src/modules/auth/auth.service';

describe('Auth Credential Change E2E Tests', () => {
  let app: INestApplication;
  let authService: AuthService;
  const mockJwtToken = 'Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ1c2VyLTEyMyIsImVtYWlsIjoidXNlckBleGFtcGxlLmNvbSIsImlhdCI6MTUxNjIzOTAyMn0.mock';

  beforeAll(async () => {
    const moduleFixture: TestingModule = await Test.createTestingModule({
      imports: [AppModule],
    }).compile();

    app = moduleFixture.createNestApplication();
    await app.init();

    authService = moduleFixture.get<AuthService>(AuthService);
  });

  afterAll(async () => {
    await app.close();
  });

  describe('Password Change Flow', () => {
    it('POST /api/v1/auth/password/change/initiate - should initiate password change with valid credentials', () => {
      return request(app.getHttpServer())
        .post('/api/v1/auth/password/change/initiate')
        .set('Authorization', mockJwtToken)
        .send({
          currentPassword: 'CurrentPassword123!',
        })
        .expect(HttpStatus.ACCEPTED)
        .expect((res) => {
          expect(res.body).toHaveProperty('success');
          expect(res.body).toHaveProperty('message');
          expect(res.body).toHaveProperty('expiresIn');
          expect(res.body).toHaveProperty('destination');
        });
    });

    it('POST /api/v1/auth/password/change/initiate - should return 401 without authentication token', () => {
      return request(app.getHttpServer())
        .post('/api/v1/auth/password/change/initiate')
        .send({
          currentPassword: 'CurrentPassword123!',
        })
        .expect(HttpStatus.UNAUTHORIZED);
    });

    it('POST /api/v1/auth/password/change/confirm - should confirm password change with valid OTP', () => {
      return request(app.getHttpServer())
        .post('/api/v1/auth/password/change/confirm')
        .set('Authorization', mockJwtToken)
        .send({
          verificationCode: '123456',
          newPassword: 'NewPassword123!',
        })
        .expect(HttpStatus.OK)
        .expect((res) => {
          expect(res.body).toHaveProperty('success');
          expect(res.body).toHaveProperty('message');
          expect(res.body).toHaveProperty('credentialType', 'password');
        });
    });
  });

  describe('Email Change Flow', () => {
    it('POST /api/v1/auth/email/change/initiate - should initiate email change', () => {
      return request(app.getHttpServer())
        .post('/api/v1/auth/email/change/initiate')
        .set('Authorization', mockJwtToken)
        .send({
          newEmail: 'newemail@example.com',
          currentPassword: 'CurrentPassword123!',
        })
        .expect(HttpStatus.ACCEPTED)
        .expect((res) => {
          expect(res.body).toHaveProperty('success');
          expect(res.body).toHaveProperty('message');
          expect(res.body).toHaveProperty('destination');
        });
    });

    it('POST /api/v1/auth/email/change/confirm - should confirm email change with valid OTP', () => {
      return request(app.getHttpServer())
        .post('/api/v1/auth/email/change/confirm')
        .set('Authorization', mockJwtToken)
        .send({
          verificationCode: '654321',
        })
        .expect(HttpStatus.OK)
        .expect((res) => {
          expect(res.body).toHaveProperty('success');
          expect(res.body).toHaveProperty('credentialType', 'email');
        });
    });
  });

  describe('Verification Status', () => {
    it('GET /api/v1/auth/verification/status - should return verification status', () => {
      return request(app.getHttpServer())
        .get('/api/v1/auth/verification/status')
        .set('Authorization', mockJwtToken)
        .expect(HttpStatus.OK)
        .expect((res) => {
          expect(res.body).toHaveProperty('isVerificationPending');
          expect(res.body).toHaveProperty('attemptsRemaining');
        });
    });

    it('GET /api/v1/auth/verification/status - should return 401 without authentication', () => {
      return request(app.getHttpServer())
        .get('/api/v1/auth/verification/status')
        .expect(HttpStatus.UNAUTHORIZED);
    });
  });

  describe('OTP Resend', () => {
    it('POST /api/v1/auth/otp/resend - should resend OTP for password change', () => {
      return request(app.getHttpServer())
        .post('/api/v1/auth/otp/resend')
        .set('Authorization', mockJwtToken)
        .send({
          credentialType: 'password',
        })
        .expect(HttpStatus.ACCEPTED)
        .expect((res) => {
          expect(res.body).toHaveProperty('success');
          expect(res.body).toHaveProperty('expiresIn');
        });
    });

    it('POST /api/v1/auth/otp/resend - should resend OTP for email change', () => {
      return request(app.getHttpServer())
        .post('/api/v1/auth/otp/resend')
        .set('Authorization', mockJwtToken)
        .send({
          credentialType: 'email',
        })
        .expect(HttpStatus.ACCEPTED);
    });
  });

  describe('Rate Limiting', () => {
    it('POST /api/v1/auth/password/change/initiate - should enforce rate limiting after max requests', async () => {
      // Simular 3 solicitudes exitosas
      for (let i = 0; i < 3; i++) {
        await request(app.getHttpServer())
          .post('/api/v1/auth/password/change/initiate')
          .set('Authorization', mockJwtToken)
          .send({ currentPassword: 'CurrentPassword123!' });
      }

      // La 4a solicitud debe ser rechazada
      return request(app.getHttpServer())
        .post('/api/v1/auth/password/change/initiate')
        .set('Authorization', mockJwtToken)
        .send({ currentPassword: 'CurrentPassword123!' })
        .expect(HttpStatus.TOO_MANY_REQUESTS);
    });
  });

  describe('Error Handling', () => {
    it('POST /api/v1/auth/password/change/confirm - should return 400 for invalid OTP format', () => {
      return request(app.getHttpServer())
        .post('/api/v1/auth/password/change/confirm')
        .set('Authorization', mockJwtToken)
        .send({
          verificationCode: 'invalid', // Not 6 digits
          newPassword: 'NewPassword123!',
        })
        .expect(HttpStatus.BAD_REQUEST);
    });

    it('POST /api/v1/auth/password/change/confirm - should return 400 for invalid password format', () => {
      return request(app.getHttpServer())
        .post('/api/v1/auth/password/change/confirm')
        .set('Authorization', mockJwtToken)
        .send({
          verificationCode: '123456',
          newPassword: 'weak', // Missing uppercase, number, special char
        })
        .expect(HttpStatus.BAD_REQUEST);
    });

    it('POST /api/v1/auth/email/change/initiate - should return 400 for invalid email format', () => {
      return request(app.getHttpServer())
        .post('/api/v1/auth/email/change/initiate')
        .set('Authorization', mockJwtToken)
        .send({
          newEmail: 'invalid-email',
          currentPassword: 'CurrentPassword123!',
        })
        .expect(HttpStatus.BAD_REQUEST);
    });
  });
});
