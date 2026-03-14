import { NestFactory } from '@nestjs/core';
import { AppModule } from './app.module';
import { DocumentBuilder, SwaggerModule } from '@nestjs/swagger';
import { LoggerService } from './common/logger/logger.service';
import { GlobalExceptionFilter } from './common/filters/global-exception.filter';
import { ValidationPipe, VersioningType } from '@nestjs/common';
import cookieParser from 'cookie-parser';
import { appConfig } from './config/settings';

async function bootstrap() {
  const app = await NestFactory.create(AppModule, {
    logger: false,
    rawBody: true,
  });

  app.enableVersioning({
    type: VersioningType.URI,
    prefix: 'v',
    defaultVersion: appConfig.API_GLOBAL_VERSION
  })

  app.setGlobalPrefix('api')
  app.use(cookieParser());
  app.useGlobalPipes(new ValidationPipe({ whitelist: true, transform: true }));

  app.enableCors({
    origin: process.env.FRONTEND_URL || 'http://localhost:5173',
    credentials: true,
    methods: ['GET', 'POST', 'PUT', 'PATCH', 'DELETE', 'OPTIONS'],
    allowedHeaders: ['Content-Type', 'Authorization'],
  });

  const logger = app.get(LoggerService);
  app.useLogger(logger);
  app.useGlobalFilters(new GlobalExceptionFilter(logger));

  const config = new DocumentBuilder()
    .setTitle('SkillMind API Gateway')
    .setDescription('API Gateway for SkillMind platform')
    .setVersion('1.0')
    .addBearerAuth()
    .build();

  const document = SwaggerModule.createDocument(app, config);
  SwaggerModule.setup('api', app, document);

  const port = process.env.PORT || 3000;
  await app.listen(port);

  logger.info('Application started successfully', {
    service: 'api-gateway',
    environment: process.env.NODE_ENV || 'development',
    timestamp: new Date().toISOString(),
    port,
    swaggerURL: `http://localhost:${port}/api`,
  });
}

void bootstrap();