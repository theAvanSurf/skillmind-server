import { Test, TestingModule } from '@nestjs/testing';
import { AuthController } from '../src/modules/auth/auth.controller';
import AuthService from '../src/modules/auth/auth.service';
import { AuthGuard } from '../src/common/guards/auth.guard';
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
} from '../src/modules/auth/auth.dto';

describe('AuthController', () => {
    let controller: AuthController;
    let authService: jest.Mocked<AuthService>;

    const mockAuthService = {
        authenticateUser: jest.fn(),
        createUser: jest.fn(),
        getResetToken: jest.fn(),
        confirmAccount: jest.fn(),
        resetPassword: jest.fn(),
        refreshToken: jest.fn(),
        getCredentialSecuritySettings: jest.fn(),
        startPasswordChange: jest.fn(),
        completePasswordChange: jest.fn(),
        startEmailChange: jest.fn(),
        completeEmailChange: jest.fn(),
    };

    beforeEach(async () => {
        const module: TestingModule = await Test.createTestingModule({
            controllers: [AuthController],
            providers: [
                {
                    provide: AuthService,
                    useValue: mockAuthService,
                },
            ],
        })
            .overrideGuard(AuthGuard)
            .useValue({ canActivate: () => true })
            .compile();

        controller = module.get<AuthController>(AuthController);
        authService = module.get(AuthService);
        jest.clearAllMocks();
    });

    describe('login', () => {
        const loginRequest: AuthenticateUserDto = {
            userName: 'john.doe',
            password: 'P@ssw0rd!',
        };

        it('should return mapped user data on successful login', async () => {
            const serviceResponse = {
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
            mockAuthService.authenticateUser.mockResolvedValue(serviceResponse);

            const result = await controller.login(loginRequest);

            expect(authService.authenticateUser).toHaveBeenCalledWith(loginRequest);
            expect(result).toEqual({
                id: '123',
                name: 'John',
                lastName: 'Doe',
                email: 'john@example.com',
                roles: ['User'],
                isVerified: 'true',
                jwtToken: 'jwt-token',
                refreshToken: 'refresh-token',
            });
        });

        it('should return error response when login fails', async () => {
            const errorResponse = {
                data: null,
                hasError: true,
                errors: ['Invalid credentials'],
            };
            mockAuthService.authenticateUser.mockResolvedValue(errorResponse);

            const result = await controller.login(loginRequest);

            expect(result).toEqual(errorResponse);
        });

        it('should call authenticateUser with correct parameters', async () => {
            mockAuthService.authenticateUser.mockResolvedValue({
                data: {},
                hasError: false,
                errors: [],
            });

            await controller.login(loginRequest);

            expect(authService.authenticateUser).toHaveBeenCalledTimes(1);
            expect(authService.authenticateUser).toHaveBeenCalledWith({
                userName: 'john.doe',
                password: 'P@ssw0rd!',
            });
        });
    });

    describe('signUp', () => {
        const signUpRequest: CreateUserDto = {
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

        it('should call createUser and return response', async () => {
            const expectedResponse = {
                id: '123',
                name: 'John',
                lastName: 'Doe',
                email: 'john@example.com',
                username: 'john.doe',
                isVerified: 'false',
                hasError: 'false',
                errors: [],
            };
            mockAuthService.createUser.mockResolvedValue(expectedResponse);

            const result = await controller.signUp(signUpRequest);

            expect(authService.createUser).toHaveBeenCalledWith(signUpRequest);
            expect(result).toEqual(expectedResponse);
        });
    });

    describe('getResetToken', () => {
        const forgotRequest: ForgotApiRequestDto = {
            Email: 'john@example.com',
        };

        it('should call getResetToken with email', async () => {
            const expectedResponse = { success: true };
            mockAuthService.getResetToken.mockResolvedValue(expectedResponse);

            const result = await controller.getResetToken(forgotRequest);

            expect(authService.getResetToken).toHaveBeenCalledWith(forgotRequest);
            expect(result).toEqual(expectedResponse);
        });
    });

    describe('confirmAccount', () => {
        const confirmRequest: ConfirmRequestDto = {
            UserId: '123',
            Code: '847291',
        };

        it('should call confirmAccount with request', async () => {
            const expectedResponse = { success: true };
            mockAuthService.confirmAccount.mockResolvedValue(expectedResponse);

            const result = await controller.confirmAccount(confirmRequest);

            expect(authService.confirmAccount).toHaveBeenCalledWith(confirmRequest);
            expect(result).toEqual(expectedResponse);
        });
    });

    describe('resetPassword', () => {
        const resetRequest: ResetPasswordRequestApiDto = {
            Id: '123',
            Code: '847291',
            Password: 'NewP@ssw0rd!',
            ConfirmPassword: 'NewP@ssw0rd!',
        };

        it('should call resetPassword with request', async () => {
            const expectedResponse = { success: true };
            mockAuthService.resetPassword.mockResolvedValue(expectedResponse);

            const result = await controller.resetPassword(resetRequest);

            expect(authService.resetPassword).toHaveBeenCalledWith(resetRequest);
            expect(result).toEqual(expectedResponse);
        });
    });

    describe('refreshToken', () => {
        const refreshRequest: RefreshTokenRequestDto = {
            UserId: '123',
            RefreshToken: 'old-refresh-token',
        };

        it('should call refreshToken and return new tokens', async () => {
            const expectedResponse = {
                JwtToken: 'new-jwt-token',
                RefreshToken: 'new-refresh-token',
                ExpiresAt: '2026-03-26T12:00:00Z',
                HasError: false,
                Errors: [],
            };
            mockAuthService.refreshToken.mockResolvedValue(expectedResponse);

            const result = await controller.refreshToken(refreshRequest);

            expect(authService.refreshToken).toHaveBeenCalledWith(refreshRequest);
            expect(result).toEqual(expectedResponse);
        });
    });

    describe('getCredentialSecuritySettings', () => {
        const token = 'Bearer jwt-token';

        it('should call service with authorization token', async () => {
            const expectedResponse = {
                maskedEmail: 'j***@example.com',
                canChangePassword: true,
                canChangeEmail: true,
            };
            mockAuthService.getCredentialSecuritySettings.mockResolvedValue(expectedResponse);

            const result = await controller.getCredentialSecuritySettings(token);

            expect(authService.getCredentialSecuritySettings).toHaveBeenCalledWith(token);
            expect(result).toEqual(expectedResponse);
        });
    });

    describe('startPasswordChange', () => {
        const request: InitiatePasswordChangeRequestDto = {
            CurrentPassword: 'CurrentP@ssw0rd!',
        };
        const token = 'Bearer jwt-token';

        it('should call startPasswordChange with request and token', async () => {
            const expectedResponse = {
                status: 'OTP_SENT',
                message: 'Verification code sent',
            };
            mockAuthService.startPasswordChange.mockResolvedValue(expectedResponse);

            const result = await controller.startPasswordChange(request, token);

            expect(authService.startPasswordChange).toHaveBeenCalledWith(request, token);
            expect(result).toEqual(expectedResponse);
        });
    });

    describe('completePasswordChange', () => {
        const request: CompletePasswordChangeRequestDto = {
            Code: '123456',
            NewPassword: 'NewP@ssw0rd!',
            ConfirmPassword: 'NewP@ssw0rd!',
        };
        const token = 'Bearer jwt-token';

        it('should call completePasswordChange with request and token', async () => {
            const expectedResponse = {
                status: 'SUCCESS',
                message: 'Password changed successfully',
            };
            mockAuthService.completePasswordChange.mockResolvedValue(expectedResponse);

            const result = await controller.completePasswordChange(request, token);

            expect(authService.completePasswordChange).toHaveBeenCalledWith(request, token);
            expect(result).toEqual(expectedResponse);
        });
    });

    describe('startEmailChange', () => {
        const request: InitiateEmailChangeRequestDto = {
            CurrentPassword: 'CurrentP@ssw0rd!',
            NewEmail: 'new.email@example.com',
        };
        const token = 'Bearer jwt-token';

        it('should call startEmailChange with request and token', async () => {
            const expectedResponse = {
                status: 'OTP_SENT',
                message: 'Verification codes sent',
            };
            mockAuthService.startEmailChange.mockResolvedValue(expectedResponse);

            const result = await controller.startEmailChange(request, token);

            expect(authService.startEmailChange).toHaveBeenCalledWith(request, token);
            expect(result).toEqual(expectedResponse);
        });
    });

    describe('completeEmailChange', () => {
        const request: CompleteEmailChangeRequestDto = {
            NewEmail: 'new.email@example.com',
            CurrentEmailCode: '123456',
            NewEmailCode: '654321',
        };
        const token = 'Bearer jwt-token';

        it('should call completeEmailChange with request and token', async () => {
            const expectedResponse = {
                status: 'SUCCESS',
                message: 'Email changed successfully',
            };
            mockAuthService.completeEmailChange.mockResolvedValue(expectedResponse);

            const result = await controller.completeEmailChange(request, token);

            expect(authService.completeEmailChange).toHaveBeenCalledWith(request, token);
            expect(result).toEqual(expectedResponse);
        });
    });
});