import AuthService from '../../modules/auth/auth.service';
import httpClient from '../../config/baseHttpClient';
import { API_ENDPOINTS } from '../../common/endpoints';
import {
    AuthenticateUserDto,
    CreateUserDto,
    ForgotApiRequestDto,
    ConfirmRequestDto,
    ResetPasswordRequestApiDto,
    RefreshTokenRequestDto,
    InitiatePasswordChangeRequestDto,
    CompletePasswordChangeRequestDto,
    InitiateEmailChangeRequestDto,
    CompleteEmailChangeRequestDto,
} from '../../modules/auth/auth.dto';

// Mock httpClient
jest.mock('../../config/baseHttpClient', () => ({
    __esModule: true,
    default: {
        post: jest.fn(),
        get: jest.fn(),
    },
}));

describe('AuthService', () => {
    let authService: AuthService;
    const mockHttpClient = httpClient as jest.Mocked<typeof httpClient>;

    beforeEach(() => {
        authService = new AuthService();
        jest.clearAllMocks();
    });

    describe('authenticateUser', () => {
        const loginRequest: AuthenticateUserDto = {
            userName: 'john.doe',
            password: 'P@ssw0rd!',
        };

        it('should call httpClient.post with correct endpoint and payload', async () => {
            const mockResponse = {
                data: {
                    id: '123',
                    name: 'John',
                    lastName: 'Doe',
                    email: 'john@example.com',
                    roles: ['User'],
                    isVerified: 'true',
                    jwtToken: 'jwt-token',
                    refreshToken: 'refresh-token',
                },
                hasError: false,
                errors: [],
            };
            mockHttpClient.post.mockResolvedValue(mockResponse);

            const result = await authService.authenticateUser(loginRequest);

            expect(mockHttpClient.post).toHaveBeenCalledWith(
                API_ENDPOINTS.CORE.AUTH_LOGIN,
                loginRequest
            );
            expect(result).toEqual(mockResponse);
        });

        it('should return error response when authentication fails', async () => {
            const mockErrorResponse = {
                data: null,
                hasError: true,
                errors: ['Invalid credentials'],
            };
            mockHttpClient.post.mockResolvedValue(mockErrorResponse);

            const result = await authService.authenticateUser(loginRequest);

            expect(result.hasError).toBe(true);
            expect(result.errors).toContain('Invalid credentials');
        });

        it('should propagate httpClient exceptions', async () => {
            mockHttpClient.post.mockRejectedValue(new Error('Network error'));

            await expect(authService.authenticateUser(loginRequest)).rejects.toThrow('Network error');
        });
    });

    describe('createUser', () => {
        const createUserRequest: CreateUserDto = {
            Name: 'John',
            LastName: 'Doe',
            UserName: 'john.doe',
            Email: 'john@example.com',
            Password: 'P@ssw0rd!',
            BirthDate: '1990-05-20',
            PhoneNumber: '+18091234567',
            Country: 'Dominican Republic',
            AccountTypes: 1,
            Role: 2,
        };

        it('should call httpClient.post with correct endpoint and payload', async () => {
            const mockResponse = {
                id: '123',
                name: 'John',
                lastName: 'Doe',
                email: 'john@example.com',
                username: 'john.doe',
                isVerified: 'false',
                hasError: 'false',
                errors: [],
            };
            mockHttpClient.post.mockResolvedValue(mockResponse);

            const result = await authService.createUser(createUserRequest);

            expect(mockHttpClient.post).toHaveBeenCalledWith(
                API_ENDPOINTS.CORE.AUTH_REGISTRER,
                createUserRequest
            );
            expect(result).toEqual(mockResponse);
        });

        it('should return error when user creation fails', async () => {
            const mockErrorResponse = {
                id: '',
                hasError: 'true',
                errors: ['Username already exists'],
            };
            mockHttpClient.post.mockResolvedValue(mockErrorResponse);

            const result = await authService.createUser(createUserRequest);

            expect(result.hasError).toBe('true');
        });
    });

    describe('getResetToken', () => {
        const forgotRequest: ForgotApiRequestDto = {
            Email: 'john@example.com',
        };

        it('should call httpClient.post with correct endpoint', async () => {
            const mockResponse = { success: true };
            mockHttpClient.post.mockResolvedValue(mockResponse);

            await authService.getResetToken(forgotRequest);

            expect(mockHttpClient.post).toHaveBeenCalledWith(
                API_ENDPOINTS.CORE.AUTH_GET_RESET_TOKEN,
                forgotRequest
            );
        });
    });

    describe('confirmAccount', () => {
        const confirmRequest: ConfirmRequestDto = {
            UserId: '123',
            Code: '847291',
        };

        it('should call httpClient.post with correct endpoint and payload', async () => {
            const mockResponse = { success: true };
            mockHttpClient.post.mockResolvedValue(mockResponse);

            await authService.confirmAccount(confirmRequest);

            expect(mockHttpClient.post).toHaveBeenCalledWith(
                API_ENDPOINTS.CORE.AUTH_CONFIRM,
                confirmRequest
            );
        });
    });

    describe('resetPassword', () => {
        const resetRequest: ResetPasswordRequestApiDto = {
            Id: '123',
            Code: '847291',
            Password: 'NewP@ssw0rd!',
            ConfirmPassword: 'NewP@ssw0rd!',
        };

        it('should call httpClient.post with correct endpoint and payload', async () => {
            const mockResponse = { success: true };
            mockHttpClient.post.mockResolvedValue(mockResponse);

            await authService.resetPassword(resetRequest);

            expect(mockHttpClient.post).toHaveBeenCalledWith(
                API_ENDPOINTS.CORE.AUTH_RESET_PASSWORD,
                resetRequest
            );
        });
    });

    describe('refreshToken', () => {
        const refreshRequest: RefreshTokenRequestDto = {
            UserId: '123',
            RefreshToken: 'old-refresh-token',
        };

        it('should call httpClient.post and return new tokens', async () => {
            const mockResponse = {
                JwtToken: 'new-jwt-token',
                RefreshToken: 'new-refresh-token',
                ExpiresAt: '2026-03-26T12:00:00Z',
                HasError: false,
                Errors: [],
            };
            mockHttpClient.post.mockResolvedValue(mockResponse);

            const result = await authService.refreshToken(refreshRequest);

            expect(mockHttpClient.post).toHaveBeenCalledWith(
                API_ENDPOINTS.CORE.AUTH_REFRESH,
                refreshRequest
            );
            expect(result.JwtToken).toBe('new-jwt-token');
            expect(result.RefreshToken).toBe('new-refresh-token');
        });

        it('should return error when refresh token is invalid', async () => {
            const mockErrorResponse = {
                JwtToken: '',
                RefreshToken: '',
                ExpiresAt: '',
                HasError: true,
                Errors: ['Invalid refresh token'],
            };
            mockHttpClient.post.mockResolvedValue(mockErrorResponse);

            const result = await authService.refreshToken(refreshRequest);

            expect(result.HasError).toBe(true);
        });
    });

    describe('getCredentialSecuritySettings', () => {
        const token = 'Bearer jwt-token';

        it('should call httpClient.get with authorization header', async () => {
            const mockResponse = {
                maskedEmail: 'j***@example.com',
                canChangePassword: true,
                canChangeEmail: true,
            };
            mockHttpClient.get.mockResolvedValue(mockResponse);

            const result = await authService.getCredentialSecuritySettings(token);

            expect(mockHttpClient.get).toHaveBeenCalledWith(
                API_ENDPOINTS.CORE.AUTH_SECURITY_SETTINGS,
                { headers: { Authorization: token } }
            );
            expect(result.maskedEmail).toBe('j***@example.com');
        });
    });

    describe('startPasswordChange', () => {
        const request: InitiatePasswordChangeRequestDto = {
            CurrentPassword: 'CurrentP@ssw0rd!',
        };
        const token = 'Bearer jwt-token';

        it('should call httpClient.post with correct endpoint and headers', async () => {
            const mockResponse = {
                status: 'OTP_SENT',
                message: 'Verification code sent to your email',
            };
            mockHttpClient.post.mockResolvedValue(mockResponse);

            const result = await authService.startPasswordChange(request, token);

            expect(mockHttpClient.post).toHaveBeenCalledWith(
                API_ENDPOINTS.CORE.AUTH_START_PASSWORD_CHANGE,
                request,
                { headers: { Authorization: token } }
            );
            expect(result.status).toBe('OTP_SENT');
        });
    });

    describe('completePasswordChange', () => {
        const request: CompletePasswordChangeRequestDto = {
            Code: '123456',
            NewPassword: 'NewP@ssw0rd!',
            ConfirmPassword: 'NewP@ssw0rd!',
        };
        const token = 'Bearer jwt-token';

        it('should call httpClient.post with correct endpoint and headers', async () => {
            const mockResponse = {
                status: 'SUCCESS',
                message: 'Password changed successfully',
            };
            mockHttpClient.post.mockResolvedValue(mockResponse);

            const result = await authService.completePasswordChange(request, token);

            expect(mockHttpClient.post).toHaveBeenCalledWith(
                API_ENDPOINTS.CORE.AUTH_COMPLETE_PASSWORD_CHANGE,
                request,
                { headers: { Authorization: token } }
            );
            expect(result.status).toBe('SUCCESS');
        });
    });

    describe('startEmailChange', () => {
        const request: InitiateEmailChangeRequestDto = {
            CurrentPassword: 'CurrentP@ssw0rd!',
            NewEmail: 'new.email@example.com',
        };
        const token = 'Bearer jwt-token';

        it('should call httpClient.post with correct endpoint and headers', async () => {
            const mockResponse = {
                status: 'OTP_SENT',
                message: 'Verification codes sent to both emails',
            };
            mockHttpClient.post.mockResolvedValue(mockResponse);

            const result = await authService.startEmailChange(request, token);

            expect(mockHttpClient.post).toHaveBeenCalledWith(
                API_ENDPOINTS.CORE.AUTH_START_EMAIL_CHANGE,
                request,
                { headers: { Authorization: token } }
            );
            expect(result.status).toBe('OTP_SENT');
        });
    });

    describe('completeEmailChange', () => {
        const request: CompleteEmailChangeRequestDto = {
            NewEmail: 'new.email@example.com',
            CurrentEmailCode: '123456',
            NewEmailCode: '654321',
        };
        const token = 'Bearer jwt-token';

        it('should call httpClient.post with correct endpoint and headers', async () => {
            const mockResponse = {
                status: 'SUCCESS',
                message: 'Email changed successfully',
            };
            mockHttpClient.post.mockResolvedValue(mockResponse);

            const result = await authService.completeEmailChange(request, token);

            expect(mockHttpClient.post).toHaveBeenCalledWith(
                API_ENDPOINTS.CORE.AUTH_COMPLETE_EMAIL_CHANGE,
                request,
                { headers: { Authorization: token } }
            );
            expect(result.status).toBe('SUCCESS');
        });
    });
});
