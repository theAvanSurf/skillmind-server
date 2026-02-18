import { Module } from '@nestjs/common';
import { HttpModule } from '@nestjs/axios';
import { ConfigModule } from '@nestjs/config';

import { OAuthController } from './oauth.controller.js';
import { OAuthService } from './oauth.service.js';

@Module({
  imports: [
    ConfigModule,
    HttpModule.register({ timeout: 10_000 }),
  ],
  controllers: [OAuthController],
  providers: [OAuthService],
})
export class OAuthModule {}
