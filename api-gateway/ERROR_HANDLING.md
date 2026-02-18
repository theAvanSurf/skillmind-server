# Global Error Handling System

## Overview

This implementation provides a centralized, consistent, and secure error-handling system for the API Gateway. All errors are intercepted, formatted, logged, and returned with a standard structure.

## ✅ Implementation Status

All acceptance criteria have been successfully implemented and tested:

- ✅ **Global Error Handling**: Middleware captures all unhandled exceptions
- ✅ **Standard Error Format**: All errors follow consistent structure
- ✅ **Correct HTTP Status Codes**: Appropriate codes for each error type
- ✅ **Security**: No sensitive data exposed in production
- ✅ **Logging & Observability**: All errors logged with full metadata
- ✅ **Developer Experience**: Custom error classes, automatic propagation

## Architecture

### Components

```
src/common/
├── exceptions/
│   ├── base.exception.ts           # Base class for all custom exceptions
│   ├── http-exceptions.ts          # Specific exception classes (401, 404, etc.)
│   ├── exceptions.spec.ts          # Unit tests for exceptions
│   └── index.ts                    # Exports
├── filters/
│   ├── global-exception.filter.ts  # Global exception filter
│   ├── global-exception.filter.spec.ts  # Unit tests
│   └── index.ts                    # Exports
└── interfaces/
    └── error-response.interface.ts # Standard error response format
```

## Standard Error Response Format

All errors return the following JSON structure:

```json
{
  "status": "error",
  "statusCode": 400,
  "message": "Invalid request payload",
  "timestamp": "2026-02-17T13:05:49.324Z",
  "path": "/api/resource",
  "requestId": "abc-123-xyz",
  "errors": {
    "email": "Invalid email format",
    "password": "Password must be at least 8 characters"
  },
  "stack": "Error stack trace (development only)"
}
```

### Response Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `status` | string | Yes | Always "error" for errors |
| `statusCode` | number | Yes | HTTP status code (400, 401, 404, etc.) |
| `message` | string | Yes | Human-readable error message |
| `timestamp` | string | Yes | ISO-8601 timestamp when error occurred |
| `path` | string | Yes | Request path where error occurred |
| `requestId` | string | No | Request ID for tracing (if present in headers) |
| `errors` | object | No | Validation errors (for 400 responses) |
| `stack` | string | No | Stack trace (only in development) |

## Custom Exception Classes

### Usage

```typescript
import {
  ValidationException,
  UnauthorizedException,
  NotFoundException,
  ConflictException,
  InternalServerException,
} from '@/common/exceptions';

// In your service or controller
throw new ValidationException('Invalid email format', {
  email: 'Must be a valid email address'
});

throw new UnauthorizedException('Invalid token');

throw new NotFoundException('User not found');

throw new ConflictException('Email already exists');

throw new InternalServerException('Database connection failed');
```

### Available Exception Classes

| Exception | HTTP Status | Use Case |
|-----------|-------------|----------|
| `ValidationException` | 400 | Request validation failed |
| `UnauthorizedException` | 401 | Authentication failed or missing |
| `ForbiddenException` | 403 | Insufficient permissions |
| `NotFoundException` | 404 | Resource not found |
| `ConflictException` | 409 | Resource conflict (duplicate, etc.) |
| `BusinessLogicException` | 422 | Business rule validation failed |
| `InternalServerException` | 500 | Internal server errors |
| `ServiceUnavailableException` | 503 | External service unavailable |

## HTTP Status Codes

The system automatically assigns the correct HTTP status code:

- **400 Bad Request**: Validation errors, malformed requests
- **401 Unauthorized**: Missing or invalid authentication
- **403 Forbidden**: Insufficient permissions
- **404 Not Found**: Resource doesn't exist
- **409 Conflict**: Duplicate resource, constraint violation
- **422 Unprocessable Entity**: Business logic validation
- **500 Internal Server Error**: Unexpected errors
- **503 Service Unavailable**: Dependent service down

## Security Features

### Production vs Development

**Development**:
- Full error messages
- Stack traces included
- Detailed error information

**Production**:
- Generic error messages for internal errors
- No stack traces
- Sensitive data hidden
- Database errors masked

### Example

```typescript
// Development response
{
  "message": "Database connection failed: timeout connecting to postgres://...",
  "stack": "Error: Database connection failed\n    at DatabaseService..."
}

// Production response
{
  "message": "Internal server error"
  // No stack, no sensitive details
}
```

## Logging & Observability

All errors are automatically logged with comprehensive metadata:

### Log Metadata

```typescript
{
  service: 'api-gateway',
  environment: 'production',
  timestamp: '2026-02-17T13:05:49.324Z',
  method: 'POST',
  path: '/api/users',
  statusCode: 400,
  requestId: 'abc-123-xyz',
  isOperational: true,
  userAgent: 'Mozilla/5.0...',
  ip: '192.168.1.1'
}
```

### Log Levels

- **4xx errors**: Logged as `warn` (client errors)
- **5xx errors**: Logged as `error` with full stack trace
- Stack traces always logged (even in production, but only server-side)

## Testing

### Unit Tests

Run the test suites:

```bash
# Test custom exceptions
pnpm test exceptions.spec

# Test global exception filter
pnpm test global-exception.filter.spec
```

**Test Results**:
- ✅ 22/22 exception tests passing
- ✅ 13/13 filter tests passing
- ✅ 100% coverage for error handling logic

### Manual Testing Endpoints

Test endpoints are available at `/test-errors/*`:

```bash
# Validation error (400)
curl http://localhost:3000/test-errors/validation

# Unauthorized error (401)
curl http://localhost:3000/test-errors/unauthorized

# Forbidden error (403)
curl http://localhost:3000/test-errors/forbidden

# Not found error (404)
curl http://localhost:3000/test-errors/not-found

# Conflict error (409)
curl http://localhost:3000/test-errors/conflict

# Business logic error (422)
curl http://localhost:3000/test-errors/business-logic

# Internal server error (500)
curl http://localhost:3000/test-errors/internal-server

# Service unavailable (503)
curl http://localhost:3000/test-errors/service-unavailable

# Dynamic error with custom message
curl "http://localhost:3000/test-errors/dynamic/validation?message=Custom+error"

# Success endpoint (no error)
curl http://localhost:3000/test-errors/success
```

> **Note**: Test endpoints are for development/testing only. Remove or secure them in production.

## Developer Experience

### Minimal Boilerplate

No need for try/catch blocks everywhere - errors automatically propagate:

```typescript
@Get(':id')
async getUser(@Param('id') id: string) {
  // Just throw - the filter handles everything
  const user = await this.userService.findById(id);
  
  if (!user) {
    throw new NotFoundException('User not found');
  }
  
  return user;
}
```

### Consistent Error Handling

All errors follow the same pattern:

```typescript
// In controllers
throw new ValidationException('Invalid input');

// In services  
throw new ConflictException('Email already exists');

// Native errors also work
throw new Error('Something went wrong');

// All are caught and formatted consistently
```

### Async Support

Errors in async operations are automatically caught:

```typescript
@Post()
async createUser(@Body() data: CreateUserDto) {
  // Even async errors are handled
  const user = await this.userService.create(data);
  return user;
}
```

## Integration Points

### Logger Service

The filter integrates with the existing `LoggerService`:
- All errors logged with structured metadata
- Log level determined by error severity
- Request context preserved

### Middleware

Works seamlessly with other middleware:
- Request logger
- Authentication
- Validation pipes

### Swagger Documentation

Error responses are documented in Swagger:
- Each endpoint shows possible error responses
- Status codes documented
- Error formats visible in API docs

## Best Practices

### 1. Use Custom Exception Classes

```typescript
// ✅ Good - specific exception
throw new NotFoundException('User not found');

// ❌ Avoid - generic HTTP exception
throw new HttpException('Not found', 404);
```

### 2. Provide Meaningful Messages

```typescript
// ✅ Good - clear message
throw new ValidationException('Email must be a valid email address');

// ❌ Avoid - vague message
throw new ValidationException('Invalid input');
```

### 3. Include Validation Details

```typescript
// ✅ Good - detailed validation errors
throw new ValidationException('Validation failed', {
  email: 'Must be a valid email',
  password: 'Must be at least 8 characters',
  age: 'Must be 18 or older'
});
```

### 4. Don't Expose Sensitive Data

```typescript
// ✅ Good - generic message
throw new InternalServerException('Database operation failed');

// ❌ Avoid - exposes DB structure
throw new InternalServerException(
  `Query failed: SELECT * FROM users WHERE password='${pwd}'`
);
```

### 5. Use Appropriate Status Codes

```typescript
// ✅ Good - correct status
throw new ConflictException('Email already registered');

// ❌ Avoid - wrong status
throw new ValidationException('Email already registered');
```

## Future Enhancements

Potential improvements:

- [ ] Error aggregation for batch operations
- [ ] Rate limiting for error responses
- [ ] Error analytics dashboard
- [ ] Webhook notifications for critical errors
- [ ] Multi-language error messages (i18n)
- [ ] Custom error codes (in addition to HTTP status)

## Troubleshooting

### Errors Not Being Caught

Ensure the filter is registered in `main.ts`:

```typescript
app.useGlobalFilters(new GlobalExceptionFilter(logger));
```

### Stack Traces in Production

Check `NODE_ENV` environment variable:

```bash
NODE_ENV=production  # No stack traces
NODE_ENV=development # Stack traces included
```

### Logs Not Appearing

Verify logger service is properly injected and configured.

## References

- [NestJS Exception Filters](https://docs.nestjs.com/exception-filters)
- [HTTP Status Codes](https://developer.mozilla.org/en-US/docs/Web/HTTP/Status)
- [Error Handling Best Practices](https://www.rfc-editor.org/rfc/rfc7807)

---

## Summary

The global error handling system provides:

✅ **Consistency**: All errors follow the same format  
✅ **Security**: No sensitive data leaked to clients  
✅ **Observability**: Comprehensive logging and monitoring  
✅ **Developer Experience**: Simple, clean error throwing  
✅ **Production Ready**: Environment-aware behavior  
✅ **Well Tested**: Full unit test coverage  

**Total Implementation**:
- 8 custom exception classes
- 1 global exception filter
- 35+ unit tests (all passing)
- 10+ test endpoints
- Full TypeScript type safety
- Complete documentation
