import { ArgumentsHost, HttpException, HttpStatus } from '@nestjs/common';
import { GlobalExceptionFilter } from '../src/common/filters/global-exception.filter';
import { BaseException } from '../src/common/exceptions/base.exception';
import { LoggerService } from '../src/common/logger/logger.service';

describe('GlobalExceptionFilter', () => {
    let filter: GlobalExceptionFilter;
    let mockLoggerService: jest.Mocked<LoggerService>;
    let mockResponse: any;
    let mockRequest: any;
    let mockArgumentsHost: ArgumentsHost;

    beforeEach(() => {
        mockLoggerService = {
            logError: jest.fn(),
            error: jest.fn(),
            warn: jest.fn(),
            info: jest.fn(),
            debug: jest.fn(),
        } as any;

        mockResponse = {
            status: jest.fn().mockReturnThis(),
            json: jest.fn().mockReturnThis(),
        };

        mockRequest = {
            method: 'POST',
            url: '/api/v1/auth/login',
            requestId: 'test-trace-id-123',
        };

        mockArgumentsHost = {
            switchToHttp: () => ({
                getResponse: () => mockResponse,
                getRequest: () => mockRequest,
            }),
        } as ArgumentsHost;

        filter = new GlobalExceptionFilter(mockLoggerService);
    });

    describe('catch', () => {
        it('should handle BaseException and return proper error response', () => {
            // Constructor: (message, errorCode, statusCode, details)
            const exception = new BaseException(
                'User not found',
                'USER_NOT_FOUND',
                HttpStatus.NOT_FOUND
            );

            filter.catch(exception, mockArgumentsHost);

            expect(mockResponse.status).toHaveBeenCalledWith(HttpStatus.NOT_FOUND);
            expect(mockResponse.json).toHaveBeenCalledWith(
                expect.objectContaining({
                    status: HttpStatus.NOT_FOUND,
                    errorCode: 'USER_NOT_FOUND',
                    message: 'User not found',
                    traceId: 'test-trace-id-123',
                })
            );
        });

        it('should handle BaseException with details', () => {
            // Constructor: (message, errorCode, statusCode, details)
            const exception = new BaseException(
                'Validation failed',
                'VALIDATION_ERROR',
                HttpStatus.BAD_REQUEST,
                ['Field email is required', 'Password too short']
            );

            filter.catch(exception, mockArgumentsHost);

            expect(mockResponse.json).toHaveBeenCalledWith(
                expect.objectContaining({
                    status: HttpStatus.BAD_REQUEST,
                    errorCode: 'VALIDATION_ERROR',
                    message: 'Validation failed',
                    details: ['Field email is required', 'Password too short'],
                })
            );
        });

        it('should handle HttpException and extract error info', () => {
            const exception = new HttpException(
                { message: 'Bad request', errorCode: 'BAD_REQUEST' },
                HttpStatus.BAD_REQUEST
            );

            filter.catch(exception, mockArgumentsHost);

            expect(mockResponse.status).toHaveBeenCalledWith(HttpStatus.BAD_REQUEST);
            expect(mockResponse.json).toHaveBeenCalledWith(
                expect.objectContaining({
                    status: HttpStatus.BAD_REQUEST,
                    errorCode: 'BAD_REQUEST',
                    message: 'Bad request',
                })
            );
        });

        it('should handle HttpException with validation errors array', () => {
            const exception = new HttpException(
                { message: ['email must be valid', 'password is required'] },
                HttpStatus.BAD_REQUEST
            );

            filter.catch(exception, mockArgumentsHost);

            expect(mockResponse.json).toHaveBeenCalledWith(
                expect.objectContaining({
                    status: HttpStatus.BAD_REQUEST,
                    errorCode: 'BAD_REQUEST',
                    message: 'The request contains validation errors.',
                    details: ['email must be valid', 'password is required'],
                })
            );
        });

        it('should handle HttpException with string response', () => {
            const exception = new HttpException('Unauthorized', HttpStatus.UNAUTHORIZED);

            filter.catch(exception, mockArgumentsHost);

            expect(mockResponse.status).toHaveBeenCalledWith(HttpStatus.UNAUTHORIZED);
            expect(mockResponse.json).toHaveBeenCalledWith(
                expect.objectContaining({
                    status: HttpStatus.UNAUTHORIZED,
                    errorCode: 'UNAUTHORIZED',
                    message: 'Unauthorized',
                })
            );
        });

        it('should handle generic Error and return 500', () => {
            const exception = new Error('Something went wrong');

            filter.catch(exception, mockArgumentsHost);

            expect(mockResponse.status).toHaveBeenCalledWith(HttpStatus.INTERNAL_SERVER_ERROR);
            expect(mockResponse.json).toHaveBeenCalledWith(
                expect.objectContaining({
                    status: HttpStatus.INTERNAL_SERVER_ERROR,
                    errorCode: 'INTERNAL_SERVER_ERROR',
                    message: 'An unexpected error occurred. Please try again later.',
                })
            );
        });

        it('should handle unknown exception types and return 500', () => {
            const exception = 'string exception';

            filter.catch(exception, mockArgumentsHost);

            expect(mockResponse.status).toHaveBeenCalledWith(HttpStatus.INTERNAL_SERVER_ERROR);
            expect(mockResponse.json).toHaveBeenCalledWith(
                expect.objectContaining({
                    status: HttpStatus.INTERNAL_SERVER_ERROR,
                    errorCode: 'INTERNAL_SERVER_ERROR',
                    message: 'An unexpected error occurred. Please try again later.',
                })
            );
        });

        it('should generate traceId when requestId is not present', () => {
            mockRequest.requestId = undefined;

            const exception = new HttpException('Test', HttpStatus.BAD_REQUEST);

            filter.catch(exception, mockArgumentsHost);

            expect(mockResponse.json).toHaveBeenCalledWith(
                expect.objectContaining({
                    traceId: expect.any(String),
                })
            );
        });

        it('should include timestamp in error response', () => {
            const exception = new HttpException('Test', HttpStatus.BAD_REQUEST);

            filter.catch(exception, mockArgumentsHost);

            expect(mockResponse.json).toHaveBeenCalledWith(
                expect.objectContaining({
                    timestamp: expect.any(String),
                })
            );
        });

        it('should log error for 5xx status codes', () => {
            const exception = new Error('Internal error');

            filter.catch(exception, mockArgumentsHost);

            expect(mockLoggerService.logError).toHaveBeenCalledWith(
                exception,
                expect.objectContaining({
                    service: 'api-gateway',
                    statusCode: 500,
                    method: 'POST',
                    path: '/api/v1/auth/login',
                })
            );
        });

        it('should log warning for 4xx status codes', () => {
            const exception = new HttpException('Not found', HttpStatus.NOT_FOUND);

            filter.catch(exception, mockArgumentsHost);

            expect(mockLoggerService.warn).toHaveBeenCalledWith(
                expect.stringContaining('Client error [404]'),
                expect.objectContaining({
                    statusCode: 404,
                })
            );
        });

        it('should map HTTP status codes to error codes correctly', () => {
            const testCases = [
                { status: HttpStatus.BAD_REQUEST, expectedCode: 'BAD_REQUEST' },
                { status: HttpStatus.UNAUTHORIZED, expectedCode: 'UNAUTHORIZED' },
                { status: HttpStatus.FORBIDDEN, expectedCode: 'FORBIDDEN' },
                { status: HttpStatus.NOT_FOUND, expectedCode: 'NOT_FOUND' },
                { status: HttpStatus.CONFLICT, expectedCode: 'CONFLICT' },
                { status: HttpStatus.TOO_MANY_REQUESTS, expectedCode: 'TOO_MANY_REQUESTS' },
            ];

            testCases.forEach(({ status, expectedCode }) => {
                jest.clearAllMocks();
                const exception = new HttpException('Test', status);

                filter.catch(exception, mockArgumentsHost);

                expect(mockResponse.json).toHaveBeenCalledWith(
                    expect.objectContaining({
                        errorCode: expectedCode,
                    })
                );
            });
        });

        it('should handle error from .NET Core service with errorCode', () => {
            const exception = new HttpException(
                {
                    errorCode: 'INVALID_CREDENTIALS',
                    message: 'The provided credentials are invalid',
                },
                HttpStatus.UNAUTHORIZED
            );

            filter.catch(exception, mockArgumentsHost);

            expect(mockResponse.json).toHaveBeenCalledWith(
                expect.objectContaining({
                    errorCode: 'INVALID_CREDENTIALS',
                    message: 'The provided credentials are invalid',
                })
            );
        });
    });
});