// @ts-nocheck
import { HttpStatus } from '@nestjs/common';
import {
  BaseException,
  ValidationException,
  UnauthorizedException,
  ForbiddenException,
  NotFoundException,
  ConflictException,
  InternalServerException,
  ServiceUnavailableException,
  BusinessLogicException,
} from './index';

describe('Custom Exceptions', () => {
  describe('BaseException', () => {
    it('should create a base exception with default values', () => {
      const exception = new BaseException('Test error');

      expect(exception.message).toBe('Test error');
      expect(exception.getStatus()).toBe(HttpStatus.INTERNAL_SERVER_ERROR);
      expect(exception.isOperational).toBe(true);
      expect(exception.timestamp).toBeDefined();
      expect(new Date(exception.timestamp).getTime()).toBeLessThanOrEqual(Date.now());
    });

    it('should create a base exception with custom status code', () => {
      const exception = new BaseException('Test error', HttpStatus.BAD_REQUEST);

      expect(exception.getStatus()).toBe(HttpStatus.BAD_REQUEST);
    });

    it('should mark exception as non-operational when specified', () => {
      const exception = new BaseException(
        'Test error',
        HttpStatus.INTERNAL_SERVER_ERROR,
        false,
      );

      expect(exception.isOperational).toBe(false);
    });

    it('should capture stack trace', () => {
      const exception = new BaseException('Test error');

      expect(exception.stack).toBeDefined();
      expect(exception.stack).toContain('BaseException');
    });
  });

  describe('ValidationException', () => {
    it('should create validation exception with default message', () => {
      const exception = new ValidationException();

      expect(exception.message).toBe('Validation failed');
      expect(exception.getStatus()).toBe(HttpStatus.BAD_REQUEST);
      expect(exception.name).toBe('ValidationException');
    });

    it('should create validation exception with custom message', () => {
      const exception = new ValidationException('Invalid email format');

      expect(exception.message).toBe('Invalid email format');
    });

    it('should include validation errors', () => {
      const errors = {
        email: 'Invalid format',
        password: 'Too short',
      };
      const exception = new ValidationException('Validation failed', errors);

      expect(exception.errors).toEqual(errors);
    });
  });

  describe('UnauthorizedException', () => {
    it('should create unauthorized exception with default message', () => {
      const exception = new UnauthorizedException();

      expect(exception.message).toBe('Unauthorized access');
      expect(exception.getStatus()).toBe(HttpStatus.UNAUTHORIZED);
      expect(exception.name).toBe('UnauthorizedException');
    });

    it('should create unauthorized exception with custom message', () => {
      const exception = new UnauthorizedException('Invalid token');

      expect(exception.message).toBe('Invalid token');
    });
  });

  describe('ForbiddenException', () => {
    it('should create forbidden exception with default message', () => {
      const exception = new ForbiddenException();

      expect(exception.message).toBe('Access forbidden');
      expect(exception.getStatus()).toBe(HttpStatus.FORBIDDEN);
      expect(exception.name).toBe('ForbiddenException');
    });

    it('should create forbidden exception with custom message', () => {
      const exception = new ForbiddenException('Insufficient permissions');

      expect(exception.message).toBe('Insufficient permissions');
    });
  });

  describe('NotFoundException', () => {
    it('should create not found exception with default message', () => {
      const exception = new NotFoundException();

      expect(exception.message).toBe('Resource not found');
      expect(exception.getStatus()).toBe(HttpStatus.NOT_FOUND);
      expect(exception.name).toBe('NotFoundException');
    });

    it('should create not found exception with custom message', () => {
      const exception = new NotFoundException('User not found');

      expect(exception.message).toBe('User not found');
    });
  });

  describe('ConflictException', () => {
    it('should create conflict exception with default message', () => {
      const exception = new ConflictException();

      expect(exception.message).toBe('Resource conflict');
      expect(exception.getStatus()).toBe(HttpStatus.CONFLICT);
      expect(exception.name).toBe('ConflictException');
    });

    it('should create conflict exception with custom message', () => {
      const exception = new ConflictException('Email already exists');

      expect(exception.message).toBe('Email already exists');
    });
  });

  describe('InternalServerException', () => {
    it('should create internal server exception with default message', () => {
      const exception = new InternalServerException();

      expect(exception.message).toBe('Internal server error');
      expect(exception.getStatus()).toBe(HttpStatus.INTERNAL_SERVER_ERROR);
      expect(exception.name).toBe('InternalServerException');
      expect(exception.isOperational).toBe(false);
    });

    it('should create internal server exception with custom message', () => {
      const exception = new InternalServerException('Database connection failed');

      expect(exception.message).toBe('Database connection failed');
    });

    it('should be marked as non-operational', () => {
      const exception = new InternalServerException();

      expect(exception.isOperational).toBe(false);
    });
  });

  describe('ServiceUnavailableException', () => {
    it('should create service unavailable exception with default message', () => {
      const exception = new ServiceUnavailableException();

      expect(exception.message).toBe('Service temporarily unavailable');
      expect(exception.getStatus()).toBe(HttpStatus.SERVICE_UNAVAILABLE);
      expect(exception.name).toBe('ServiceUnavailableException');
    });

    it('should create service unavailable exception with custom message', () => {
      const exception = new ServiceUnavailableException('Payment service down');

      expect(exception.message).toBe('Payment service down');
    });
  });

  describe('BusinessLogicException', () => {
    it('should create business logic exception with default message', () => {
      const exception = new BusinessLogicException();

      expect(exception.message).toBe('Business logic validation failed');
      expect(exception.getStatus()).toBe(HttpStatus.UNPROCESSABLE_ENTITY);
      expect(exception.name).toBe('BusinessLogicException');
    });

    it('should create business logic exception with custom message', () => {
      const exception = new BusinessLogicException('Insufficient balance');

      expect(exception.message).toBe('Insufficient balance');
    });
  });
});
