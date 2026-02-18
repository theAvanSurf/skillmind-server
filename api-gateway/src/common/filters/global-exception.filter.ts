import {
  ExceptionFilter,
  Catch,
  ArgumentsHost,
  HttpException,
  HttpStatus,
  Injectable,
} from '@nestjs/common';
import { Request, Response } from 'express';
import { LoggerService } from '../logger/logger.service';
import { ErrorResponse } from '../interfaces/error-response.interface';
import { BaseException } from '../exceptions/base.exception';
import { ValidationException } from '../exceptions/http-exceptions';

/**
 * Global Exception Filter
 * Catches all exceptions thrown in the application and formats them consistently
 */
@Catch()
@Injectable()
export class GlobalExceptionFilter implements ExceptionFilter {
  constructor(private readonly logger: LoggerService) {}

  catch(exception: unknown, host: ArgumentsHost): void {
    const ctx = host.switchToHttp();
    const response = ctx.getResponse<Response>();
    const request = ctx.getRequest<Request>();

    const errorResponse = this.buildErrorResponse(exception, request);

    // Log the error
    this.logError(exception, request, errorResponse);

    // Send response to client
    response.status(errorResponse.statusCode).json(errorResponse);
  }

  /**
   * Builds a standardized error response
   */
  private buildErrorResponse(exception: unknown, request: Request): ErrorResponse {
    const isProduction = process.env.NODE_ENV === 'production';
    const timestamp = new Date().toISOString();
    const path = request.url;
    const requestId = request.headers['x-request-id'] as string | undefined;

    // Handle HttpException (including our custom exceptions)
    if (exception instanceof HttpException) {
      const status = exception.getStatus();
      const exceptionResponse = exception.getResponse();

      let message: string;
      let errors: any;

      // Handle different response formats
      if (typeof exceptionResponse === 'string') {
        message = exceptionResponse;
      } else if (typeof exceptionResponse === 'object' && exceptionResponse !== null) {
        const responseObj = exceptionResponse as any;
        message = responseObj.message || responseObj.error || 'An error occurred';
        errors = responseObj.errors || responseObj.validationErrors;
      } else {
        message = 'An error occurred';
      }

      // Check if it's our ValidationException with custom errors property
      if (exception instanceof ValidationException && exception.errors) {
        errors = exception.errors;
      }

      const errorResponse: ErrorResponse = {
        status: 'error',
        statusCode: status,
        message,
        timestamp,
        path,
        requestId,
      };

      // Add validation errors if present
      if (errors) {
        errorResponse.errors = errors;
      }

      // Add stack trace in development
      if (!isProduction && exception.stack) {
        errorResponse.stack = exception.stack;
      }

      return errorResponse;
    }

    // Handle native JavaScript errors
    if (exception instanceof Error) {
      const errorResponse: ErrorResponse = {
        status: 'error',
        statusCode: HttpStatus.INTERNAL_SERVER_ERROR,
        message: isProduction
          ? 'Internal server error'
          : exception.message || 'An unexpected error occurred',
        timestamp,
        path,
        requestId,
      };

      // Add stack trace in development
      if (!isProduction && exception.stack) {
        errorResponse.stack = exception.stack;
      }

      return errorResponse;
    }

    // Handle unknown errors
    return {
      status: 'error',
      statusCode: HttpStatus.INTERNAL_SERVER_ERROR,
      message: isProduction
        ? 'Internal server error'
        : 'An unexpected error occurred',
      timestamp,
      path,
      requestId,
    };
  }

  /**
   * Logs the error with appropriate metadata
   */
  private logError(
    exception: unknown,
    request: Request,
    errorResponse: ErrorResponse,
  ): void {
    const { statusCode, message, path, requestId } = errorResponse;
    const method = request.method;
    const isOperational = exception instanceof BaseException
      ? exception.isOperational
      : false;

    // Prepare log metadata
    const metadata = {
      service: 'api-gateway',
      environment: process.env.NODE_ENV || 'development',
      timestamp: errorResponse.timestamp,
      method,
      path,
      statusCode,
      requestId,
      isOperational,
      userAgent: request.headers['user-agent'],
      ip: request.ip || request.socket.remoteAddress,
    };

    // Log based on severity
    if (statusCode >= 500) {
      // Server errors - log as error with full details
      if (exception instanceof Error) {
        this.logger.logError(exception, metadata);
      } else {
        this.logger.error(message, metadata);
      }
    } else if (statusCode >= 400) {
      // Client errors - log as warning
      this.logger.warn(`Client error: ${message}`, metadata);
    } else {
      // Unexpected status codes
      this.logger.info(`Request completed with status ${statusCode}: ${message}`, metadata);
    }
  }
}
