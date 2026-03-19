import { HttpStatus } from '@nestjs/common';
import { BaseException } from './base.exception';

// ─────────────────────────────────────────────
// 404 – Resource Not Found
// ─────────────────────────────────────────────

export class ResourceNotFoundException extends BaseException {
  constructor(resource: string, identifier?: string) {
    const message = identifier
      ? `${resource} with identifier '${identifier}' was not found.`
      : `${resource} was not found.`;
    super(message, HttpStatus.NOT_FOUND);
  }
}

export class ProfileNotFoundException extends BaseException {
  constructor(profileId: string) {
    super(`Profile '${profileId}' does not exist.`, HttpStatus.NOT_FOUND);
  }
}

export class SessionNotFoundException extends BaseException {
  constructor() {
    super('No active session found for this user.', HttpStatus.NOT_FOUND);
  }
}

// ─────────────────────────────────────────────
// 400 – Bad Request / Validation
// ─────────────────────────────────────────────

export class ValidationException extends BaseException {
  constructor(details: string[]) {
    const detailsText = details.length > 0 ? ` Details: ${details.join(', ')}` : '';
    super(
      `The request contains invalid or missing fields.${detailsText}`,
      HttpStatus.UNPROCESSABLE_ENTITY,
    );
  }
}

export class InvalidTokenException extends BaseException {
  constructor(reason?: string) {
    super(reason ?? 'The provided token is invalid or malformed.', HttpStatus.BAD_REQUEST);
  }
}

// ─────────────────────────────────────────────
// 401 – Unauthorized
// ─────────────────────────────────────────────

export class UnauthorizedException extends BaseException {
  constructor(message?: string) {
    super(
      message ?? 'Authentication is required to access this resource.',
      HttpStatus.UNAUTHORIZED,
    );
  }
}

export class TokenExpiredException extends BaseException {
  constructor() {
    super('Your session has expired. Please log in again.', HttpStatus.UNAUTHORIZED);
  }
}

// ─────────────────────────────────────────────
// 403 – Forbidden
// ─────────────────────────────────────────────

export class ForbiddenException extends BaseException {
  constructor(action?: string) {
    const message = action
      ? `You do not have permission to ${action}.`
      : 'You do not have permission to perform this action.';
    super(message, HttpStatus.FORBIDDEN);
  }
}

// ─────────────────────────────────────────────
// 409 – Conflict
// ─────────────────────────────────────────────

export class ConflictException extends BaseException {
  constructor(resource: string, reason?: string) {
    const message = reason
      ? `${resource} conflict: ${reason}`
      : `${resource} already exists or conflicts with existing data.`;
    super(message, HttpStatus.CONFLICT);
  }
}

// ─────────────────────────────────────────────
// 503 – External Service Unavailable
// ─────────────────────────────────────────────

export class ExternalServiceException extends BaseException {
  constructor(serviceName: string, reason?: string) {
    const message = reason
      ? `External service '${serviceName}' failed: ${reason}`
      : `External service '${serviceName}' is currently unavailable.`;
    super(message, HttpStatus.SERVICE_UNAVAILABLE);
  }
}

// ─────────────────────────────────────────────
// 500 – Internal / Business Rule Violations
// ─────────────────────────────────────────────

export class BusinessRuleException extends BaseException {
  constructor(rule: string) {
    super(`Business rule violation: ${rule}`, HttpStatus.BAD_REQUEST);
  }
}