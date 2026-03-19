import { Controller, Post, Body, UseGuards, Request, Logger, Get, HttpCode, HttpStatus } from '@nestjs/common';
import { JwtAuthGuard } from '../../common/guards/jwt-auth.guard';
import { CredentialChangeRateLimitGuard } from './rate-limit-guard';
import { AuthService } from './auth.service';
import {
  InitiatePasswordChangeDto,
  InitiateEmailChangeDto,
  ConfirmPasswordChangeDto,
  ConfirmEmailChangeDto,
  ResendOTPDto,
  OTPSentResponseDto,
  ChangeCredentialResponseDto,
  VerificationStatusDto,
} from './auth.dto';

@Controller('api/v1/auth')
export class AuthController {
  private readonly logger = new Logger(AuthController.name);

  constructor(private readonly authService: AuthService) {}

  /**
   * Inicia proceso de cambio de contraseña
   * POST /api/v1/auth/password/change/initiate
   */
  @UseGuards(JwtAuthGuard, CredentialChangeRateLimitGuard)
  @Post('password/change/initiate')
  @HttpCode(HttpStatus.ACCEPTED)
  async initiatePasswordChange(
    @Request() req: any,
    @Body() dto: InitiatePasswordChangeDto,
  ): Promise<OTPSentResponseDto> {
    this.logger.log(`Password change initiated by user ${req.user.sub}`);
    return this.authService.initiatePasswordChange(req.user.sub, req.user.email, dto);
  }

  /**
   * Confirma cambio de contraseña con OTP
   * POST /api/v1/auth/password/change/confirm
   */
  @UseGuards(JwtAuthGuard)
  @Post('password/change/confirm')
  @HttpCode(HttpStatus.OK)
  async confirmPasswordChange(
    @Request() req: any,
    @Body() dto: ConfirmPasswordChangeDto,
  ): Promise<ChangeCredentialResponseDto> {
    this.logger.log(`Password change confirmation by user ${req.user.sub}`);
    return this.authService.confirmPasswordChange(req.user.sub, req.user.email, dto);
  }

  /**
   * Inicia proceso de cambio de email
   * POST /api/v1/auth/email/change/initiate
   */
  @UseGuards(JwtAuthGuard, CredentialChangeRateLimitGuard)
  @Post('email/change/initiate')
  @HttpCode(HttpStatus.ACCEPTED)
  async initiateEmailChange(
    @Request() req: any,
    @Body() dto: InitiateEmailChangeDto,
  ): Promise<OTPSentResponseDto> {
    this.logger.log(`Email change initiated by user ${req.user.sub}`);
    return this.authService.initiateEmailChange(req.user.sub, req.user.email, dto);
  }

  /**
   * Confirma cambio de email con OTP
   * POST /api/v1/auth/email/change/confirm
   */
  @UseGuards(JwtAuthGuard)
  @Post('email/change/confirm')
  @HttpCode(HttpStatus.OK)
  async confirmEmailChange(
    @Request() req: any,
    @Body() dto: ConfirmEmailChangeDto,
  ): Promise<ChangeCredentialResponseDto> {
    this.logger.log(`Email change confirmation by user ${req.user.sub}`);

    // Obtener el nuevo email del request (debe venir de la sesión anterior)
    // En caso real, esto debe almacenarse en sesión o Redis durante la iniciación
    const newEmail = req.body.newEmail || req.user.pendingEmail;

    return this.authService.confirmEmailChange(req.user.sub, req.user.email, newEmail, dto);
  }

  /**
   * Obtiene estado de verificación pendiente
   * GET /api/v1/auth/verification/status
   */
  @UseGuards(JwtAuthGuard)
  @Get('verification/status')
  async getVerificationStatus(@Request() req: any): Promise<VerificationStatusDto> {
    return this.authService.getVerificationStatus(req.user.sub);
  }

  /**
   * Reenvía código OTP
   * POST /api/v1/auth/otp/resend
   */
  @UseGuards(JwtAuthGuard)
  @Post('otp/resend')
  @HttpCode(HttpStatus.ACCEPTED)
  async resendOTP(
    @Request() req: any,
    @Body() dto: ResendOTPDto,
  ): Promise<OTPSentResponseDto> {
    this.logger.log(`OTP resend requested by user ${req.user.sub} for ${dto.credentialType}`);
    return this.authService.resendOTP(req.user.sub, req.user.email, dto.credentialType);
  }
}
