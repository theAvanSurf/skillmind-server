import { NestFactory } from '@nestjs/core';
import { AppModule } from './app.module';
import { DocumentBuilder, SwaggerModule } from '@nestjs/swagger';

async function bootstrap() {
  const app = await NestFactory.create(AppModule);
  const port = process.env.PORT || 3000;

  const config = new DocumentBuilder()
        .setTitle('SkillMind API Gateway')
        .setDescription('The SkillMind API Gateway description')
        .setVersion('1.0')
        .build();
    const document = SwaggerModule.createDocument(app, config);
    SwaggerModule.setup('api', app, document);
  
  await app.listen(port, '0.0.0.0');
  
  console.log(`Application is running on port ${port}`);
}

void bootstrap();