import { Module } from '@nestjs/common';
import { HttpModule } from '@nestjs/axios';
import { CacheModule } from '@nestjs/cache-manager';
import { AuthController } from './auth.controller';
import { AuthService } from './auth.service';
import { OTPService } from './otp.service';
import { EmailService } from './email.service';
import { CredentialChangeRateLimitGuard } from './rate-limit-guard';

@Module({
  imports: [HttpModule, CacheModule.register()],
  controllers: [AuthController],
  providers: [AuthService, OTPService, EmailService, CredentialChangeRateLimitGuard],
  exports: [AuthService, OTPService, EmailService, CredentialChangeRateLimitGuard],
})
export class AuthModule {}
