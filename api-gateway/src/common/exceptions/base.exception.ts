import { HttpException, HttpStatus } from '@nestjs/common';

/**
 * Base exception class for all custom exceptions
 * Extends NestJS HttpException to maintain framework compatibility
 */
export class BaseException extends HttpException {
  public readonly timestamp: string;
  public readonly isOperational: boolean;

  constructor(
    message: string,
    statusCode: HttpStatus = HttpStatus.INTERNAL_SERVER_ERROR,
    isOperational: boolean = true,
  ) {
    super(message, statusCode);
    this.timestamp = new Date().toISOString();
    this.isOperational = isOperational;

    // Maintains proper stack trace for where error was thrown
    Error.captureStackTrace(this, this.constructor);
  }
}
