import { ApiProperty } from "@nestjs/swagger";
import {
    IsString,
    IsEmail,
    IsNotEmpty,
    IsNumber,
    IsDateString,
    IsPhoneNumber,
    MinLength,
    MaxLength,
    Min,
    Max,
    isPhoneNumber,
} from 'class-validator';

export class AuthenticateUserDto {
    @ApiProperty({ example: 'john.doe' })
    @IsString()
    @IsNotEmpty()
    userName: string;

    @ApiProperty({ example: '123456' })
    @IsString()
    @IsNotEmpty()
    password: string;
}

export class CreateUserDto {
    @ApiProperty({ example: 'John', description: 'First name of the user' })
    @IsString()
    @IsNotEmpty()
    @MaxLength(50)
    Name: string;

    @ApiProperty({ example: 'Doe', description: 'Last name of the user' })
    @IsString()
    @IsNotEmpty()
    @MaxLength(50)
    LastName: string;

    @ApiProperty({ example: 'john.doe', description: 'Unique username' })
    @IsString()
    @IsNotEmpty()
    @MinLength(3)
    @MaxLength(30)
    UserName: string;

    @ApiProperty({ example: 'john.doe@email.com', description: 'User email address' })
    @IsEmail()
    @IsNotEmpty()
    Email: string;

    @ApiProperty({ example: 'P@ssw0rd!', description: 'Password (min 8 characters)' })
    @IsString()
    @IsNotEmpty()
    @MinLength(8)
    @MaxLength(100)
    Password: string;

    @ApiProperty({ example: '1990-05-20', description: 'Birth date in ISO format (YYYY-MM-DD)' })
    @IsDateString()
    @IsNotEmpty()
    BirthDate: string;

    @ApiProperty({ example: '+18091234567', description: 'Phone number in E.164 format' })
    @IsNotEmpty()
    PhoneNumber: string;

    @ApiProperty({ example: 'Dominican Republic', description: 'Country of residence' })
    @IsString()
    @IsNotEmpty()
    Country: string;

    @ApiProperty({ example: 1, description: 'Account type (e.g. 1 = Personal, 2 = Business)' })
    @IsNumber()
    @Min(1)
    @Max(10)
    AccountTypes: number;

    @ApiProperty({ example: 1, description: 'Role assigned to the user (e.g.0 = Professor 1 = Admin, 2 = User)' })
    @IsNumber()
    @Min(0)
    @Max(2)
    Role: number;
}

export interface LoginUserData {
    id: string
    name: string
    lastName: string
    email: string
    roles: string[]
    isVerified: string
    jwtToken: string
    refreshToken?: string
}

export interface LoginAPIResponse {
    data: LoginUserData
    hasError: boolean
    errors: string[]
}

export interface RegistrerResponseDto {
    id: string
    name: string
    lastName: string
    email: string
    username: string
    isVerified: string
    hasError: string
    errors: string[]
}

export class ForgotApiRequestDto {
    @ApiProperty({ example: 'john.doe@email.com', description: 'User email address to send the reset token' })
    @IsEmail()
    @IsNotEmpty()
    Email: string;
}

export class RefreshTokenRequestDto {
    @ApiProperty({ example: '123e4567-e89b-12d3-a456-426614174000', description: 'User ID' })
    @IsString()
    @IsNotEmpty()
    UserId: string;

    @ApiProperty({ example: 'a1b2c3d4...', description: '64-char opaque refresh token' })
    @IsString()
    @IsNotEmpty()
    RefreshToken: string;
}

export interface RefreshTokenResponseDto {
    JwtToken: string;
    RefreshToken: string;
    ExpiresAt: string;
    HasError: boolean;
    Errors: string[];
}

export class ConfirmRequestDto {
    @ApiProperty({ example: '123e4567-e89b-12d3-a456-426614174000', description: 'User ID' })
    @IsString()
    @IsNotEmpty()
    UserId: string;

    @ApiProperty({ example: '847291', description: '6-digit OTP sent to the user email' })
    @IsString()
    @IsNotEmpty()
    Code: string;
}

export class ResetPasswordRequestApiDto {
    @ApiProperty({ example: '123e4567-e89b-12d3-a456-426614174000', description: 'User ID' })
    @IsString()
    @IsNotEmpty()
    Id: string;

    @ApiProperty({ example: '847291', description: '6-digit OTP sent to the user email' })
    @IsString()
    @IsNotEmpty()
    Code: string;

    @ApiProperty({ example: 'NewP@ssw0rd!', description: 'New Password' })
    @IsString()
    @IsNotEmpty()
    @MinLength(8)
    Password: string;

    @ApiProperty({ example: 'NewP@ssw0rd!', description: 'Confirm New Password' })
    @IsString()
    @IsNotEmpty()
    @MinLength(8)
    ConfirmPassword: string;
}

export interface CredentialSecuritySettingsDto {
    maskedEmail: string;
    canChangePassword: boolean;
    canChangeEmail: boolean;
}

export class InitiatePasswordChangeRequestDto {
    @ApiProperty({ example: 'CurrentP@ssw0rd!', description: 'Current account password' })
    @IsString()
    @IsNotEmpty()
    CurrentPassword: string;
}

export class CompletePasswordChangeRequestDto {
    @ApiProperty({ example: '847291', description: '6-digit OTP code sent to the registered email' })
    @IsString()
    @IsNotEmpty()
    Code: string;

    @ApiProperty({ example: 'NewP@ssw0rd!1', description: 'New password' })
    @IsString()
    @IsNotEmpty()
    @MinLength(8)
    NewPassword: string;

    @ApiProperty({ example: 'NewP@ssw0rd!1', description: 'Confirm new password' })
    @IsString()
    @IsNotEmpty()
    @MinLength(8)
    ConfirmPassword: string;
}

export class InitiateEmailChangeRequestDto {
    @ApiProperty({ example: 'CurrentP@ssw0rd!', description: 'Current account password' })
    @IsString()
    @IsNotEmpty()
    CurrentPassword: string;

    @ApiProperty({ example: 'new.email@domain.com', description: 'New email address' })
    @IsEmail()
    @IsNotEmpty()
    NewEmail: string;
}

export class CompleteEmailChangeRequestDto {
    @ApiProperty({ example: 'new.email@domain.com', description: 'Pending email to confirm' })
    @IsEmail()
    @IsNotEmpty()
    NewEmail: string;

    @ApiProperty({ example: '123456', description: 'Code sent to current email' })
    @IsString()
    @IsNotEmpty()
    CurrentEmailCode: string;

    @ApiProperty({ example: '654321', description: 'Code sent to new email' })
    @IsString()
    @IsNotEmpty()
    NewEmailCode: string;
}

export interface CredentialChangeActionResponseDto {
    status: string;
    message: string;
}