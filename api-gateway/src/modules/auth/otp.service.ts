import { Injectable, Inject } from '@nestjs/common';
import { CACHE_MANAGER } from '@nestjs/cache-manager';
import type { Cache } from 'cache-manager';
import { InvalidOTPException, OTPExpiredException, MaxOTPAttemptsExceededException } from './auth.exceptions';

interface OTPData {
  code: string;
  credentialType: 'password' | 'email';
  attempts: number;
  createdAt: number;
  expiresAt: number;
}

@Injectable()
export class OTPService {
  private readonly OTP_LENGTH = 6;
  private readonly OTP_EXPIRATION_MINUTES = 15;
  private readonly MAX_ATTEMPTS = 3;
  private readonly RATE_LIMIT_WINDOW_MINUTES = 60;
  private readonly MAX_REQUESTS_PER_HOUR = 3;

  constructor(@Inject(CACHE_MANAGER) private cacheManager: Cache) {}

  /**
   * Genera un OTP de 6 dígitos
   */
  private generateOTP(): string {
    return Math.floor(Math.random() * 1000000)
      .toString()
      .padStart(this.OTP_LENGTH, '0');
  }

  /**
   * Crea y almacena un OTP en Redis
   */
  async createOTP(userId: string, credentialType: 'password' | 'email'): Promise<{ code: string; expiresIn: number }> {
    // Verificar rate limiting
    await this.checkRateLimit(userId, credentialType);

    // Invalidar OTP anterior si existe
    const otpKey = this.getOTPKey(userId);
    await this.cacheManager.del(otpKey);

    // Generar nuevo OTP
    const code = this.generateOTP();
    const now = Date.now();
    const expiresAtMs = now + this.OTP_EXPIRATION_MINUTES * 60 * 1000;

    const otpData: OTPData = {
      code,
      credentialType,
      attempts: 0,
      createdAt: now,
      expiresAt: expiresAtMs,
    };

    // Guardar en Redis con TTL
    await this.cacheManager.set(otpKey, JSON.stringify(otpData), this.OTP_EXPIRATION_MINUTES * 60 * 1000);

    // Incrementar contador de solicitudes
    await this.incrementRequestCount(userId, credentialType);

    return {
      code,
      expiresIn: this.OTP_EXPIRATION_MINUTES * 60, // en segundos
    };
  }

  /**
   * Verifica un OTP
   */
  async verifyOTP(userId: string, code: string): Promise<boolean> {
    const otpKey = this.getOTPKey(userId);
    const otpDataStr = await this.cacheManager.get<string>(otpKey);

    if (!otpDataStr) {
      throw new OTPExpiredException('OTP has expired or does not exist');
    }

    const otpData: OTPData = JSON.parse(otpDataStr);

    // Verificar expiración
    if (Date.now() > otpData.expiresAt) {
      await this.cacheManager.del(otpKey);
      throw new OTPExpiredException();
    }

    // Verificar intentos máximos
    if (otpData.attempts >= this.MAX_ATTEMPTS) {
      await this.cacheManager.del(otpKey);
      throw new MaxOTPAttemptsExceededException();
    }

    // Verificar código
    otpData.attempts++;
    await this.cacheManager.set(otpKey, JSON.stringify(otpData), this.OTP_EXPIRATION_MINUTES * 60 * 1000);

    if (otpData.code !== code) {
      throw new InvalidOTPException(`Invalid OTP. ${this.MAX_ATTEMPTS - otpData.attempts} attempts remaining.`);
    }

    // Eliminar OTP después de verificación exitosa
    await this.cacheManager.del(otpKey);
    return true;
  }

  /**
   * Obtiene el tipo de credencial del OTP en cache
   */
  async getOTPCredentialType(userId: string): Promise<'password' | 'email' | null> {
    const otpKey = this.getOTPKey(userId);
    const otpDataStr = await this.cacheManager.get<string>(otpKey);

    if (!otpDataStr) {
      return null;
    }

    const otpData: OTPData = JSON.parse(otpDataStr);
    return otpData.credentialType;
  }

  /**
   * Obtiene información de OTP pendiente
   */
  async getOTPInfo(userId: string): Promise<{ expiresIn: number; attemptsRemaining: number } | null> {
    const otpKey = this.getOTPKey(userId);
    const otpDataStr = await this.cacheManager.get<string>(otpKey);

    if (!otpDataStr) {
      return null;
    }

    const otpData: OTPData = JSON.parse(otpDataStr);
    const expiresIn = Math.max(0, Math.ceil((otpData.expiresAt - Date.now()) / 1000));
    const attemptsRemaining = Math.max(0, this.MAX_ATTEMPTS - otpData.attempts);

    return { expiresIn, attemptsRemaining };
  }

  /**
   * Verifica si hay OTP pendiente para el usuario
   */
  async hasOTPPending(userId: string): Promise<boolean> {
    const otpKey = this.getOTPKey(userId);
    const otpDataStr = await this.cacheManager.get<string>(otpKey);

    if (!otpDataStr) {
      return false;
    }

    const otpData: OTPData = JSON.parse(otpDataStr);
    return Date.now() <= otpData.expiresAt;
  }

  /**
   * Verifica rate limiting
   */
  private async checkRateLimit(userId: string, credentialType: 'password' | 'email'): Promise<void> {
    const rateLimitKey = this.getRateLimitKey(userId, credentialType);
    const requestCount = await this.cacheManager.get<number>(rateLimitKey);

    if (requestCount && requestCount >= this.MAX_REQUESTS_PER_HOUR) {
      throw new Error(`Rate limit exceeded. Maximum ${this.MAX_REQUESTS_PER_HOUR} requests per hour.`);
    }
  }

  /**
   * Incrementa el contador de solicitudes
   */
  private async incrementRequestCount(userId: string, credentialType: 'password' | 'email'): Promise<void> {
    const rateLimitKey = this.getRateLimitKey(userId, credentialType);
    const currentCount = await this.cacheManager.get<number>(rateLimitKey);
    const newCount = (currentCount || 0) + 1;

    await this.cacheManager.set(rateLimitKey, newCount, this.RATE_LIMIT_WINDOW_MINUTES * 60 * 1000);
  }

  /**
   * Genera la clave para almacenar OTP en Redis
   */
  private getOTPKey(userId: string): string {
    return `otp:${userId}`;
  }

  /**
   * Genera la clave para rate limiting
   */
  private getRateLimitKey(userId: string, credentialType: string): string {
    return `ratelimit:otp:${userId}:${credentialType}`;
  }
}
