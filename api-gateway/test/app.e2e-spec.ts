import { Test, TestingModule } from '@nestjs/testing';
import { INestApplication, ValidationPipe, VersioningType, Module } from '@nestjs/common';
import request from 'supertest';
import { AppModule } from '../src/app.module';
import { GlobalExceptionFilter } from '../src/common/filters/global-exception.filter';
import { LoggerService } from '../src/common/logger/logger.service';
import { RedisModule } from '../src/infrastucture/cache/redis.module';
import { KafkaModule } from '../src/infrastucture/messaging/kafka.module';
import { HealthModule } from '../src/health/health.module';
import { CACHE_MANAGER } from '@nestjs/cache-manager';
import { ApiHealthController } from '../src/health/health.controller';

@Module({
  controllers: [ApiHealthController],
  providers: [
    {
      provide: CACHE_MANAGER,
      useValue: { get: jest.fn(), set: jest.fn(), del: jest.fn() },
    },
    {
      provide: 'KAFKA_SERVICE',
      useValue: { connect: jest.fn(), emit: jest.fn(), close: jest.fn() },
    },
  ],
})
class FakeHealthModule {}

describe('Error Handling (e2e)', () => {
  let app: INestApplication;

  beforeAll(async () => {
    const moduleFixture: TestingModule = await Test.createTestingModule({
      imports: [AppModule],
    })
      .overrideModule(HealthModule)
      .useModule(FakeHealthModule)
      .overrideModule(RedisModule)
      .useModule(class FakeRedisModule {})
      .overrideModule(KafkaModule)
      .useModule(class FakeKafkaModule {})
      .compile();

    app = moduleFixture.createNestApplication();

    // ✅ Replicar exactamente lo que hace main.ts
    app.setGlobalPrefix('api');
    app.enableVersioning({
      type: VersioningType.URI,
      prefix: 'v',
      defaultVersion: '1',
    });

    const logger = app.get(LoggerService);
    app.useGlobalFilters(new GlobalExceptionFilter(logger));
    app.useGlobalPipes(new ValidationPipe({
      whitelist: true,
      transform: true,
    }));

    await app.init();
  }, 30000);

  afterAll(async () => {
    if (app) {
      await app.close();
    }
  });

  it('GET /api/v1/nonexistent - should return 404 with standard error format', async () => {
    const response = await request(app.getHttpServer())
      .get('/api/v1/nonexistent')
      .expect(404);

    expect(response.body).toHaveProperty('status', 404);
    expect(response.body).toHaveProperty('errorCode', 'NOT_FOUND');
    expect(response.body).toHaveProperty('message');
    expect(response.body).toHaveProperty('traceId');
    expect(response.body).toHaveProperty('timestamp');

    expect(response.body.traceId).toMatch(
      /^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i
    );
  });

  it('POST /api/v1/auth/login (empty body) - should return 400 with validation errors', async () => {
    const response = await request(app.getHttpServer())
      .post('/api/v1/auth/login')
      .send({})
      .expect(400);

    expect(response.body).toHaveProperty('status', 400);
    expect(response.body).toHaveProperty('errorCode', 'BAD_REQUEST');
    expect(response.body).toHaveProperty('message');
    expect(response.body).toHaveProperty('details');
    expect(response.body).toHaveProperty('traceId');

    expect(Array.isArray(response.body.details)).toBe(true);
    expect(response.body.details.length).toBeGreaterThan(0);
  });

  it('GET /api/v1/profiles (no auth) - should return 401', async () => {
    const response = await request(app.getHttpServer())
      .get('/api/v1/profiles')
      .expect(401);

    expect(response.body).toHaveProperty('status', 401);
    expect(response.body).toHaveProperty('errorCode', 'UNAUTHORIZED');
    expect(response.body).toHaveProperty('message');
    expect(response.body).toHaveProperty('traceId');
  });

  it('GET /api/v1/api-health - should return 200 (valid endpoint)', async () => {
    await request(app.getHttpServer())
      .get('/api/v1/api-health')
      .expect(200);
  });
});