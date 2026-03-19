import { IsEmail, IsNotEmpty, IsString, MinLength, Matches } from 'class-validator';

// Request DTOs
export class InitiatePasswordChangeDto {
  @IsNotEmpty()
  @IsString()
  currentPassword!: string;
}

export class InitiateEmailChangeDto {
  @IsNotEmpty()
  @IsEmail()
  newEmail!: string;

  @IsNotEmpty()
  @IsString()
  currentPassword!: string;
}

export class VerifyOTPDto {
  @IsNotEmpty()
  @IsString()
  @Matches(/^\d{6}$/, { message: 'OTP must be a 6-digit number' })
  code!: string;
}

export class ConfirmPasswordChangeDto {
  @IsNotEmpty()
  @IsString()
  @Matches(/^\d{6}$/, { message: 'OTP must be a 6-digit number' })
  verificationCode!: string;

  @IsNotEmpty()
  @IsString()
  @MinLength(8, { message: 'Password must be at least 8 characters' })
  @Matches(/(?=.*[a-z])/, { message: 'Password must contain lowercase letter' })
  @Matches(/(?=.*[A-Z])/, { message: 'Password must contain uppercase letter' })
  @Matches(/(?=.*\d)/, { message: 'Password must contain number' })
  @Matches(/(?=.*[@$!%*?&])/, { message: 'Password must contain special character' })
  newPassword!: string;
}

export class ConfirmEmailChangeDto {
  @IsNotEmpty()
  @IsString()
  @Matches(/^\d{6}$/, { message: 'OTP must be a 6-digit number' })
  verificationCode!: string;
}

export class ResendOTPDto {
  @IsNotEmpty()
  @IsString()
  credentialType!: 'password' | 'email';
}

// Response DTOs
export class OTPSentResponseDto {
  success!: boolean;
  message!: string;
  expiresIn!: number; // seconds
  destination!: string; // masked email or phone
}

export class ChangeCredentialResponseDto {
  success!: boolean;
  message!: string;
  needsVerification?: boolean;
  credentialType?: string;
}

export class VerificationStatusDto {
  isVerificationPending!: boolean;
  credentialType?: string;
  attemptsRemaining!: number;
  expiresIn?: number; // seconds
}
