import { NestFactory } from '@nestjs/core';
import { AppModule } from './app.module';
import { DocumentBuilder, SwaggerModule } from '@nestjs/swagger';
import { LoggerService } from './common/logger/logger.service';
import { GlobalExceptionFilter } from './common/filters/global-exception.filter';

async function bootstrap() {
  const app = await NestFactory.create(AppModule, {
    logger: false,
  });

  const logger = app.get(LoggerService);
  app.useLogger(logger);

  // Register global exception filter
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
