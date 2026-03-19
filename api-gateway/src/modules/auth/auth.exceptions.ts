import { HttpException, HttpStatus } from '@nestjs/common';

export class InvalidOTPException extends HttpException {
  constructor(message: string = 'Invalid or expired OTP') {
    super(message, HttpStatus.BAD_REQUEST);
  }
}

export class OTPExpiredException extends HttpException {
  constructor(message: string = 'OTP has expired. Please request a new code') {
    super(message, HttpStatus.GONE);
  }
}

export class MaxOTPAttemptsExceededException extends HttpException {
  constructor(message: string = 'Maximum OTP attempts exceeded. Please request a new code') {
    super(message, HttpStatus.TOO_MANY_REQUESTS);
  }
}

export class CredentialChangeException extends HttpException {
  constructor(message: string = 'Failed to change credential', statusCode: HttpStatus = HttpStatus.BAD_REQUEST) {
    super(message, statusCode);
  }
}

export class RateLimitExceededException extends HttpException {
  constructor(message: string = 'Too many requests. Please try again later') {
    super(message, HttpStatus.TOO_MANY_REQUESTS);
  }
}

export class VerificationCodeRequiredException extends HttpException {
  constructor(message: string = 'Verification code is required') {
    super(message, HttpStatus.ACCEPTED);
  }
}

export class InvalidCurrentPasswordException extends HttpException {
  constructor(message: string = 'Current password is incorrect') {
    super(message, HttpStatus.UNAUTHORIZED);
  }
}
