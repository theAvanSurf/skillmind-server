import { Injectable, CanActivate, ExecutionContext, HttpException, HttpStatus, Inject } from '@nestjs/common';
import { CACHE_MANAGER } from '@nestjs/cache-manager';
import type { Cache } from 'cache-manager';

@Injectable()
export class CredentialChangeRateLimitGuard implements CanActivate {
  private readonly MAX_REQUESTS_PER_HOUR = 3;
  private readonly RATE_LIMIT_WINDOW_MS = 60 * 60 * 1000; // 1 hora

  constructor(@Inject(CACHE_MANAGER) private cacheManager: Cache) {}

  async canActivate(context: ExecutionContext): Promise<boolean> {
    const request = context.switchToHttp().getRequest();
    const userId = request.user?.sub;
    const credentialType = this.extractCredentialType(request);

    if (!userId) {
      throw new HttpException('Unauthorized', HttpStatus.UNAUTHORIZED);
    }

    const rateLimitKey = `rate_limit:${userId}:${credentialType}`;
    const requestCount = (await this.cacheManager.get<number>(rateLimitKey)) || 0;

    if (requestCount >= this.MAX_REQUESTS_PER_HOUR) {
      throw new HttpException(
        {
          statusCode: HttpStatus.TOO_MANY_REQUESTS,
          message: `Too many ${credentialType} change requests. Maximum ${this.MAX_REQUESTS_PER_HOUR} requests per hour. Try again later.`,
          retryAfter: this.RATE_LIMIT_WINDOW_MS / 1000,
        },
        HttpStatus.TOO_MANY_REQUESTS,
      );
    }

    // Incrementar contador
    await this.cacheManager.set(rateLimitKey, requestCount + 1, this.RATE_LIMIT_WINDOW_MS);

    return true;
  }

  private extractCredentialType(request: any): string {
    const path = request.route?.path || request.path || '';

    if (path.includes('password')) {
      return 'password';
    }

    if (path.includes('email')) {
      return 'email';
    }

    return 'credential';
  }
}
