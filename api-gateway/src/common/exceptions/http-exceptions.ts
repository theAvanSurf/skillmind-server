import { HttpStatus } from '@nestjs/common';
import { BaseException } from './base.exception';

/**
 * Thrown when request validation fails
 * HTTP Status: 400 Bad Request
 */
export class ValidationException extends BaseException {
  constructor(message: string = 'Validation failed', public readonly errors?: any) {
    super(message, HttpStatus.BAD_REQUEST);
    this.name = 'ValidationException';
  }
}

/**
 * Thrown when authentication fails or token is invalid
 * HTTP Status: 401 Unauthorized
 */
export class UnauthorizedException extends BaseException {
  constructor(message: string = 'Unauthorized access') {
    super(message, HttpStatus.UNAUTHORIZED);
    this.name = 'UnauthorizedException';
  }
}

/**
 * Thrown when user lacks permission to access resource
 * HTTP Status: 403 Forbidden
 */
export class ForbiddenException extends BaseException {
  constructor(message: string = 'Access forbidden') {
    super(message, HttpStatus.FORBIDDEN);
    this.name = 'ForbiddenException';
  }
}

/**
 * Thrown when requested resource is not found
 * HTTP Status: 404 Not Found
 */
export class NotFoundException extends BaseException {
  constructor(message: string = 'Resource not found') {
    super(message, HttpStatus.NOT_FOUND);
    this.name = 'NotFoundException';
  }
}

/**
 * Thrown when there's a conflict (e.g., duplicate resource)
 * HTTP Status: 409 Conflict
 */
export class ConflictException extends BaseException {
  constructor(message: string = 'Resource conflict') {
    super(message, HttpStatus.CONFLICT);
    this.name = 'ConflictException';
  }
}

/**
 * Thrown for internal server errors
 * HTTP Status: 500 Internal Server Error
 */
export class InternalServerException extends BaseException {
  constructor(message: string = 'Internal server error') {
    super(message, HttpStatus.INTERNAL_SERVER_ERROR, false);
    this.name = 'InternalServerException';
  }
}

/**
 * Thrown when external service is unavailable
 * HTTP Status: 503 Service Unavailable
 */
export class ServiceUnavailableException extends BaseException {
  constructor(message: string = 'Service temporarily unavailable') {
    super(message, HttpStatus.SERVICE_UNAVAILABLE);
    this.name = 'ServiceUnavailableException';
  }
}

/**
 * Thrown when business logic validation fails
 * HTTP Status: 422 Unprocessable Entity
 */
export class BusinessLogicException extends BaseException {
  constructor(message: string = 'Business logic validation failed') {
    super(message, HttpStatus.UNPROCESSABLE_ENTITY);
    this.name = 'BusinessLogicException';
  }
}
