import {
    AuthenticateUserDto,
    CompleteEmailChangeRequestDto,
    CompletePasswordChangeRequestDto,
    ConfirmRequestDto,
    CredentialChangeActionResponseDto,
    CredentialSecuritySettingsDto,
    CreateUserDto,
    ForgotApiRequestDto,
    InitiateEmailChangeRequestDto,
    InitiatePasswordChangeRequestDto,
    LoginAPIResponse,
    RefreshTokenRequestDto,
    RefreshTokenResponseDto,
    RegistrerResponseDto,
    ResetPasswordRequestApiDto
} from "./auth.dto";
import httpClient from "../../config/baseHttpClient";
import { API_ENDPOINTS } from "../../common/endpoints";
import { Injectable } from "@nestjs/common";

@Injectable()
export default class AuthService {
    async authenticateUser(request: AuthenticateUserDto): Promise<LoginAPIResponse> {
        return await httpClient.post<LoginAPIResponse>(
            API_ENDPOINTS.CORE.AUTH_LOGIN,
            request
        ) as unknown as LoginAPIResponse;
    }

    async createUser(request: CreateUserDto): Promise<RegistrerResponseDto> {
        return await httpClient.post<RegistrerResponseDto>(
            API_ENDPOINTS.CORE.AUTH_REGISTRER,
            request
        ) as unknown as RegistrerResponseDto;
    }

    async getResetToken(request: ForgotApiRequestDto): Promise<any> {
        return await httpClient.post<any>(
            API_ENDPOINTS.CORE.AUTH_GET_RESET_TOKEN,
            request
        );
    }

    async confirmAccount(request: ConfirmRequestDto): Promise<any> {
        return await httpClient.post<any>(
            API_ENDPOINTS.CORE.AUTH_CONFIRM,
            request
        );
    }

    async resetPassword(request: ResetPasswordRequestApiDto): Promise<any> {
        return await httpClient.post<any>(
            API_ENDPOINTS.CORE.AUTH_RESET_PASSWORD,
            request
        );
    }

    async refreshToken(request: RefreshTokenRequestDto): Promise<RefreshTokenResponseDto> {
        return await httpClient.post<RefreshTokenResponseDto>(
            API_ENDPOINTS.CORE.AUTH_REFRESH,
            request
        ) as unknown as RefreshTokenResponseDto;
    }

    async getCredentialSecuritySettings(token: string): Promise<CredentialSecuritySettingsDto> {
        return await httpClient.get<CredentialSecuritySettingsDto>(
            API_ENDPOINTS.CORE.AUTH_SECURITY_SETTINGS,
            { headers: { Authorization: token } }
        ) as unknown as CredentialSecuritySettingsDto;
    }

    async startPasswordChange(request: InitiatePasswordChangeRequestDto, token: string): Promise<CredentialChangeActionResponseDto> {
        return await httpClient.post<CredentialChangeActionResponseDto>(
            API_ENDPOINTS.CORE.AUTH_START_PASSWORD_CHANGE,
            request,
            { headers: { Authorization: token } }
        ) as unknown as CredentialChangeActionResponseDto;
    }

    async completePasswordChange(request: CompletePasswordChangeRequestDto, token: string): Promise<CredentialChangeActionResponseDto> {
        return await httpClient.post<CredentialChangeActionResponseDto>(
            API_ENDPOINTS.CORE.AUTH_COMPLETE_PASSWORD_CHANGE,
            request,
            { headers: { Authorization: token } }
        ) as unknown as CredentialChangeActionResponseDto;
    }

    async startEmailChange(request: InitiateEmailChangeRequestDto, token: string): Promise<CredentialChangeActionResponseDto> {
        return await httpClient.post<CredentialChangeActionResponseDto>(
            API_ENDPOINTS.CORE.AUTH_START_EMAIL_CHANGE,
            request,
            { headers: { Authorization: token } }
        ) as unknown as CredentialChangeActionResponseDto;
    }

    async completeEmailChange(request: CompleteEmailChangeRequestDto, token: string): Promise<CredentialChangeActionResponseDto> {
        return await httpClient.post<CredentialChangeActionResponseDto>(
            API_ENDPOINTS.CORE.AUTH_COMPLETE_EMAIL_CHANGE,
            request,
            { headers: { Authorization: token } }
        ) as unknown as CredentialChangeActionResponseDto;
    }
}