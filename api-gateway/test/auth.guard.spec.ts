import { ExecutionContext, UnauthorizedException } from '@nestjs/common';
import { AuthGuard } from '../src/common/guards/auth.guard';
import httpClient from '../src/config/baseHttpClient';
import { API_ENDPOINTS } from '../src/common/endpoints';

// Mock httpClient
jest.mock('../src/config/baseHttpClient', () => ({
    __esModule: true,
    default: {
        get: jest.fn(),
    },
}));

describe('AuthGuard', () => {
    let authGuard: AuthGuard;
    const mockHttpClient = httpClient as jest.Mocked<typeof httpClient>;

    beforeEach(() => {
        authGuard = new AuthGuard();
        jest.clearAllMocks();
    });

    const createMockExecutionContext = (authHeader?: string): ExecutionContext => {
        return {
            switchToHttp: () => ({
                getRequest: () => ({
                    headers: {
                        authorization: authHeader,
                    },
                }),
            }),
        } as ExecutionContext;
    };

    describe('canActivate', () => {
        it('should throw UnauthorizedException when authorization header is missing', async () => {
            const context = createMockExecutionContext(undefined);

            await expect(authGuard.canActivate(context)).rejects.toThrow(
                UnauthorizedException
            );
            await expect(authGuard.canActivate(context)).rejects.toThrow(
                'Missing authorization header'
            );
        });

        it('should throw UnauthorizedException when authorization header is empty', async () => {
            const context = createMockExecutionContext('');

            await expect(authGuard.canActivate(context)).rejects.toThrow(
                UnauthorizedException
            );
        });

        it('should return true when token verification succeeds', async () => {
            const validToken = 'Bearer valid-jwt-token';
            const context = createMockExecutionContext(validToken);
            mockHttpClient.get.mockResolvedValue({ valid: true });

            const result = await authGuard.canActivate(context);

            expect(result).toBe(true);
            expect(mockHttpClient.get).toHaveBeenCalledWith(
                API_ENDPOINTS.CORE.AUTH_VERIFY,
                {
                    headers: {
                        Authorization: validToken,
                    },
                }
            );
        });

        it('should call core service with correct authorization header', async () => {
            const token = 'Bearer my-secret-token';
            const context = createMockExecutionContext(token);
            mockHttpClient.get.mockResolvedValue({});

            await authGuard.canActivate(context);

            expect(mockHttpClient.get).toHaveBeenCalledTimes(1);
            expect(mockHttpClient.get).toHaveBeenCalledWith(
                API_ENDPOINTS.CORE.AUTH_VERIFY,
                expect.objectContaining({
                    headers: {
                        Authorization: token,
                    },
                })
            );
        });

        it('should propagate error when token verification fails', async () => {
            const invalidToken = 'Bearer invalid-token';
            const context = createMockExecutionContext(invalidToken);
            const authError = new UnauthorizedException('Invalid token');
            mockHttpClient.get.mockRejectedValue(authError);

            await expect(authGuard.canActivate(context)).rejects.toThrow(
                UnauthorizedException
            );
        });

        it('should propagate error when core service is unavailable', async () => {
            const token = 'Bearer valid-token';
            const context = createMockExecutionContext(token);
            const networkError = new Error('Network error');
            mockHttpClient.get.mockRejectedValue(networkError);

            await expect(authGuard.canActivate(context)).rejects.toThrow('Network error');
        });

        it('should handle token expired error from core service', async () => {
            const expiredToken = 'Bearer expired-token';
            const context = createMockExecutionContext(expiredToken);
            const tokenExpiredError = new UnauthorizedException('Token expired');
            mockHttpClient.get.mockRejectedValue(tokenExpiredError);

            await expect(authGuard.canActivate(context)).rejects.toThrow(
                'Token expired'
            );
        });
    });
});