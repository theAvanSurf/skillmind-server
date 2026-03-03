import { Body, Controller, Post } from "@nestjs/common";
import { ApiBody, ApiResponse, ApiTags } from "@nestjs/swagger";
import AuthService from "./auth.service";
import { AuthenticateUserDto, CreateUserDto, LoginAPIResponse, RegistrerResponseDto, ForgotApiRequestDto, ConfirmRequestDto, ResetPasswordRequestApiDto, RefreshTokenRequestDto, RefreshTokenResponseDto } from "./auth.dto";

@ApiTags('Auth')
@Controller("auth")
export class AuthController {

    constructor(private readonly authServices: AuthService) { }

    @Post('login')
    @ApiBody({ type: AuthenticateUserDto })
    @ApiResponse({ status: 200, description: 'User login data', type: () => AuthenticateUserDto })
    async login(@Body() request: AuthenticateUserDto): Promise<any> {
        const response = await this.authServices.authenticateUser(request);

        if (response.hasError) {
            return response;
        }

        // Map .NET camelCase response back to what the Next.js frontend expects
        return {
            id: response.data.id,
            name: response.data.name,
            lastName: response.data.lastName,
            email: response.data.email,
            roles: response.data.roles,
            isVerified: response.data.isVerified,
            jwtToken: response.data.jwtToken,
            refreshToken: response.data.refreshToken
        };
    }

    @Post('sign-up')
    @ApiBody({ type: CreateUserDto })
    @ApiResponse({ status: 201, description: 'User created' })
    async signUp(@Body() request: CreateUserDto): Promise<RegistrerResponseDto> {
        return this.authServices.createUser(request)
    }

    @Post('get-reset-token')
    @ApiBody({ type: ForgotApiRequestDto })
    @ApiResponse({ status: 204, description: 'Reset Token email has been dispatched (if standard user existed)' })
    @ApiResponse({ status: 400, description: 'User does not exist, or unconfirmed email' })
    async getResetToken(@Body() request: ForgotApiRequestDto): Promise<any> {
        return this.authServices.getResetToken(request)
    }

    @Post('confirm')
    @ApiBody({ type: ConfirmRequestDto })
    @ApiResponse({ status: 200, description: 'Account successfully confirmed' })
    @ApiResponse({ status: 400, description: 'Invalid token or user ID' })
    async confirmAccount(@Body() request: ConfirmRequestDto): Promise<any> {
        return this.authServices.confirmAccount(request)
    }

    @Post('reset-password')
    @ApiBody({ type: ResetPasswordRequestApiDto })
    @ApiResponse({ status: 204, description: 'Password successfully reset' })
    @ApiResponse({ status: 400, description: 'Invalid token, user ID, or password mismatch' })
    async resetPassword(@Body() request: ResetPasswordRequestApiDto): Promise<any> {
        return this.authServices.resetPassword(request)
    }

    @Post('refresh')
    @ApiBody({ type: RefreshTokenRequestDto })
    @ApiResponse({ status: 200, description: 'New JWT and rotated refresh token issued' })
    @ApiResponse({ status: 401, description: 'Invalid or expired refresh token' })
    async refreshToken(@Body() request: RefreshTokenRequestDto): Promise<RefreshTokenResponseDto> {
        return this.authServices.refreshToken(request)
    }
}