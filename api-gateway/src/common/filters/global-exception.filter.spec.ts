// @ts-nocheck
import { GlobalExceptionFilter } from './global-exception.filter';
import { LoggerService } from '../logger/logger.service';
import { HttpException, HttpStatus, ArgumentsHost } from '@nestjs/common';
import { Request, Response } from 'express';
import {
  ValidationException,
  UnauthorizedException,
  NotFoundException,
  InternalServerException,
} from '../exceptions';

describe('GlobalExceptionFilter', () => {
  let filter: GlobalExceptionFilter;
  let mockLogger: jest.Mocked<LoggerService>;
  let mockResponse: Partial<Response>;
  let mockRequest: Partial<Request>;
  let mockArgumentsHost: ArgumentsHost;

  beforeEach(() => {
    // Mock logger
    mockLogger = {
      error: jest.fn(),
      warn: jest.fn(),
      info: jest.fn(),
      logError: jest.fn(),
    } as any;

    // Mock response
    mockResponse = {
      status: jest.fn().mockReturnThis(),
      json: jest.fn().mockReturnThis(),
    };

    // Mock request
    mockRequest = {
      url: '/api/test',
      method: 'GET',
      headers: {
        'user-agent': 'test-agent',
        'x-request-id': 'test-request-id',
      },
      ip: '127.0.0.1',
      socket: {
        remoteAddress: '127.0.0.1',
      } as any,
    };

    // Mock ArgumentsHost
    mockArgumentsHost = {
      switchToHttp: jest.fn().mockReturnValue({
        getResponse: () => mockResponse,
        getRequest: () => mockRequest,
      }),
    } as any;

    // Create filter instance
    filter = new GlobalExceptionFilter(mockLogger);

    // Set environment to test
    process.env.NODE_ENV = 'test';
  });

  afterEach(() => {
    jest.clearAllMocks();
  });

  describe('catch', () => {
    it('should handle ValidationException correctly', () => {
      const exception = new ValidationException('Invalid input', {
        field: 'email',
        error: 'Invalid email format',
      });

      filter.catch(exception, mockArgumentsHost);

      expect(mockResponse.status).toHaveBeenCalledWith(HttpStatus.BAD_REQUEST);
      expect(mockResponse.json).toHaveBeenCalledWith(
        expect.objectContaining({
          status: 'error',
          statusCode: HttpStatus.BAD_REQUEST,
          message: 'Invalid input',
          path: '/api/test',
          requestId: 'test-request-id',
          timestamp: expect.any(String),
        }),
      );
      expect(mockLogger.warn).toHaveBeenCalled();
    });

    it('should handle UnauthorizedException correctly', () => {
      const exception = new UnauthorizedException('Invalid token');

      filter.catch(exception, mockArgumentsHost);

      expect(mockResponse.status).toHaveBeenCalledWith(HttpStatus.UNAUTHORIZED);
      expect(mockResponse.json).toHaveBeenCalledWith(
        expect.objectContaining({
          status: 'error',
          statusCode: HttpStatus.UNAUTHORIZED,
          message: 'Invalid token',
          path: '/api/test',
        }),
      );
      expect(mockLogger.warn).toHaveBeenCalled();
    });

    it('should handle NotFoundException correctly', () => {
      const exception = new NotFoundException('User not found');

      filter.catch(exception, mockArgumentsHost);

      expect(mockResponse.status).toHaveBeenCalledWith(HttpStatus.NOT_FOUND);
      expect(mockResponse.json).toHaveBeenCalledWith(
        expect.objectContaining({
          status: 'error',
          statusCode: HttpStatus.NOT_FOUND,
          message: 'User not found',
        }),
      );
      expect(mockLogger.warn).toHaveBeenCalled();
    });

    it('should handle InternalServerException correctly', () => {
      const exception = new InternalServerException('Database connection failed');

      filter.catch(exception, mockArgumentsHost);

      expect(mockResponse.status).toHaveBeenCalledWith(HttpStatus.INTERNAL_SERVER_ERROR);
      expect(mockResponse.json).toHaveBeenCalledWith(
        expect.objectContaining({
          status: 'error',
          statusCode: HttpStatus.INTERNAL_SERVER_ERROR,
          message: 'Database connection failed',
        }),
      );
      expect(mockLogger.logError).toHaveBeenCalled();
    });

    it('should handle standard HttpException', () => {
      const exception = new HttpException('Forbidden resource', HttpStatus.FORBIDDEN);

      filter.catch(exception, mockArgumentsHost);

      expect(mockResponse.status).toHaveBeenCalledWith(HttpStatus.FORBIDDEN);
      expect(mockResponse.json).toHaveBeenCalledWith(
        expect.objectContaining({
          status: 'error',
          statusCode: HttpStatus.FORBIDDEN,
          message: 'Forbidden resource',
        }),
      );
      expect(mockLogger.warn).toHaveBeenCalled();
    });

    it('should handle native JavaScript Error', () => {
      const exception = new Error('Unexpected error');

      filter.catch(exception, mockArgumentsHost);

      expect(mockResponse.status).toHaveBeenCalledWith(HttpStatus.INTERNAL_SERVER_ERROR);
      expect(mockResponse.json).toHaveBeenCalledWith(
        expect.objectContaining({
          status: 'error',
          statusCode: HttpStatus.INTERNAL_SERVER_ERROR,
          message: 'Unexpected error',
        }),
      );
      expect(mockLogger.logError).toHaveBeenCalled();
    });

    it('should handle unknown exceptions', () => {
      const exception = 'Some string error';

      filter.catch(exception, mockArgumentsHost);

      expect(mockResponse.status).toHaveBeenCalledWith(HttpStatus.INTERNAL_SERVER_ERROR);
      expect(mockResponse.json).toHaveBeenCalledWith(
        expect.objectContaining({
          status: 'error',
          statusCode: HttpStatus.INTERNAL_SERVER_ERROR,
          message: 'An unexpected error occurred',
        }),
      );
      expect(mockLogger.error).toHaveBeenCalled();
    });

    it('should include stack trace in non-production environment', () => {
      process.env.NODE_ENV = 'development';
      const exception = new Error('Test error with stack');

      filter.catch(exception, mockArgumentsHost);

      const jsonCall = (mockResponse.json as jest.Mock).mock.calls[0][0];
      expect(jsonCall.stack).toBeDefined();
    });

    it('should NOT include stack trace in production environment', () => {
      process.env.NODE_ENV = 'production';
      const exception = new Error('Test error without stack');

      filter.catch(exception, mockArgumentsHost);

      const jsonCall = (mockResponse.json as jest.Mock).mock.calls[0][0];
      expect(jsonCall.stack).toBeUndefined();
    });

    it('should use generic message in production for internal errors', () => {
      process.env.NODE_ENV = 'production';
      const exception = new Error('Sensitive database error details');

      filter.catch(exception, mockArgumentsHost);

      const jsonCall = (mockResponse.json as jest.Mock).mock.calls[0][0];
      expect(jsonCall.message).toBe('Internal server error');
    });

    it('should include validation errors when present', () => {
      const validationErrors = {
        email: 'Must be a valid email',
        password: 'Must be at least 8 characters',
      };
      const exception = new ValidationException('Validation failed', validationErrors);

      filter.catch(exception, mockArgumentsHost);

      const jsonCall = (mockResponse.json as jest.Mock).mock.calls[0][0];
      expect(jsonCall.errors).toEqual(validationErrors);
    });

    it('should include requestId when present in headers', () => {
      mockRequest.headers = {
        'x-request-id': 'custom-request-id-123',
      };

      const exception = new NotFoundException();

      filter.catch(exception, mockArgumentsHost);

      const jsonCall = (mockResponse.json as jest.Mock).mock.calls[0][0];
      expect(jsonCall.requestId).toBe('custom-request-id-123');
    });

    it('should log with correct metadata', () => {
      const exception = new NotFoundException('Resource not found');

      filter.catch(exception, mockArgumentsHost);

      expect(mockLogger.warn).toHaveBeenCalledWith(
        expect.stringContaining('Client error'),
        expect.objectContaining({
          service: 'api-gateway',
          method: 'GET',
          path: '/api/test',
          statusCode: HttpStatus.NOT_FOUND,
          requestId: 'test-request-id',
        }),
      );
    });
  });
});
