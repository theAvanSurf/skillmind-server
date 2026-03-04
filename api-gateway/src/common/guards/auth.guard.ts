import { CanActivate, ExecutionContext, Injectable, UnauthorizedException } from '@nestjs/common';
import httpClient from '../../config/baseHttpClient';
import { API_ENDPOINTS } from '../endpoints';
import { Request } from 'express';

@Injectable()
export class AuthGuard implements CanActivate {
    async canActivate(context: ExecutionContext): Promise<boolean> {
        const request = context.switchToHttp().getRequest<Request>();
        const authHeader = request.headers.authorization;

        if (!authHeader) {
            throw new UnauthorizedException('Missing authorization header');
        }

        try {
            // Ping the core service to verify the token.
            // If the token is invalid or expired, httpClient intercepts the 401 
            // and throws an HttpException, which automatically denies access here.
            await httpClient.get(API_ENDPOINTS.CORE.AUTH_VERIFY, {
                headers: {
                    Authorization: authHeader,
                },
            });

            return true;
        } catch (error) {
            // If the request fails for any reason (including 401 interceptor), deny access
            throw error;
        }
    }
}
