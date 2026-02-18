import { Controller, Get, Post, Param, Query } from '@nestjs/common';
import { ApiTags, ApiOperation, ApiResponse, ApiParam, ApiQuery } from '@nestjs/swagger';
import {
  ValidationException,
  UnauthorizedException,
  ForbiddenException,
  NotFoundException,
  ConflictException,
  InternalServerException,
  ServiceUnavailableException,
  BusinessLogicException,
} from '../common/exceptions';

/**
 * Test controller for error handling demonstration
 * DO NOT USE IN PRODUCTION - FOR TESTING PURPOSES ONLY
 */
@ApiTags('Error Testing')
@Controller('test-errors')
export class ErrorTestController {
  @Get('validation')
  @ApiOperation({ summary: 'Trigger validation error' })
  @ApiResponse({ status: 400, description: 'Validation failed' })
  testValidationError(): void {
    throw new ValidationException('Invalid request payload', {
      email: 'Invalid email format',
      password: 'Password must be at least 8 characters',
    });
  }

  @Get('unauthorized')
  @ApiOperation({ summary: 'Trigger unauthorized error' })
  @ApiResponse({ status: 401, description: 'Unauthorized' })
  testUnauthorizedError(): void {
    throw new UnauthorizedException('Invalid or expired token');
  }

  @Get('forbidden')
  @ApiOperation({ summary: 'Trigger forbidden error' })
  @ApiResponse({ status: 403, description: 'Forbidden' })
  testForbiddenError(): void {
    throw new ForbiddenException('You do not have permission to access this resource');
  }

  @Get('not-found')
  @ApiOperation({ summary: 'Trigger not found error' })
  @ApiResponse({ status: 404, description: 'Not found' })
  testNotFoundError(): void {
    throw new NotFoundException('The requested resource does not exist');
  }

  @Get('conflict')
  @ApiOperation({ summary: 'Trigger conflict error' })
  @ApiResponse({ status: 409, description: 'Conflict' })
  testConflictError(): void {
    throw new ConflictException('A resource with this identifier already exists');
  }

  @Get('internal-server')
  @ApiOperation({ summary: 'Trigger internal server error' })
  @ApiResponse({ status: 500, description: 'Internal server error' })
  testInternalServerError(): void {
    throw new InternalServerException('Database connection failed');
  }

  @Get('service-unavailable')
  @ApiOperation({ summary: 'Trigger service unavailable error' })
  @ApiResponse({ status: 503, description: 'Service unavailable' })
  testServiceUnavailableError(): void {
    throw new ServiceUnavailableException('External payment service is temporarily down');
  }

  @Get('business-logic')
  @ApiOperation({ summary: 'Trigger business logic error' })
  @ApiResponse({ status: 422, description: 'Unprocessable entity' })
  testBusinessLogicError(): void {
    throw new BusinessLogicException('Insufficient balance to complete transaction');
  }

  @Get('native-error')
  @ApiOperation({ summary: 'Trigger native JavaScript error' })
  @ApiResponse({ status: 500, description: 'Internal server error' })
  testNativeError(): void {
    throw new Error('Unexpected native JavaScript error');
  }

  @Get('unknown-error')
  @ApiOperation({ summary: 'Trigger unknown error' })
  @ApiResponse({ status: 500, description: 'Internal server error' })
  testUnknownError(): void {
    // This will throw a non-Error, non-HttpException
    throw 'This is a string error';
  }

  @Get('async-error')
  @ApiOperation({ summary: 'Trigger async error' })
  @ApiResponse({ status: 500, description: 'Internal server error' })
  async testAsyncError(): Promise<void> {
    await Promise.resolve();
    throw new Error('Error in async operation');
  }

  @Post('dynamic/:type')
  @ApiOperation({ summary: 'Trigger dynamic error based on type' })
  @ApiParam({ name: 'type', description: 'Error type to trigger' })
  @ApiQuery({ name: 'message', required: false, description: 'Custom error message' })
  @ApiResponse({ status: 400, description: 'Various error responses' })
  testDynamicError(
    @Param('type') type: string,
    @Query('message') message?: string,
  ): void {
    const customMessage = message || `Dynamic ${type} error`;

    switch (type) {
      case 'validation':
        throw new ValidationException(customMessage);
      case 'unauthorized':
        throw new UnauthorizedException(customMessage);
      case 'forbidden':
        throw new ForbiddenException(customMessage);
      case 'not-found':
        throw new NotFoundException(customMessage);
      case 'conflict':
        throw new ConflictException(customMessage);
      case 'internal':
        throw new InternalServerException(customMessage);
      case 'unavailable':
        throw new ServiceUnavailableException(customMessage);
      case 'business':
        throw new BusinessLogicException(customMessage);
      default:
        throw new NotFoundException(`Error type '${type}' not recognized`);
    }
  }

  @Get('success')
  @ApiOperation({ summary: 'Successful response (no error)' })
  @ApiResponse({ status: 200, description: 'Success' })
  testSuccess(): { message: string; timestamp: string } {
    return {
      message: 'This endpoint works correctly without errors',
      timestamp: new Date().toISOString(),
    };
  }
}
