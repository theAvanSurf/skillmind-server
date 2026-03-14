import {
  ArgumentsHost,
  Catch,
  ExceptionFilter,
  HttpException,
  HttpStatus,
  Inject,
} from '@nestjs/common';
import { Request, Response } from 'express';
import { v4 as uuidv4 } from 'uuid';
import { BaseException, ApiErrorResponse } from '../exceptions/base.exception';
import { LoggerService } from '../logger/logger.service';

/**
 * Filtro global de excepciones.
 *
 * Captura TODA excepción lanzada en la aplicación y la convierte
 * en una respuesta JSON estandarizada (ApiErrorResponse).
 *
 * Registrar en main.ts con:
 *   app.useGlobalFilters(new GlobalExceptionFilter(loggerService));
 */
@Catch()
export class GlobalExceptionFilter implements ExceptionFilter {
  constructor(
    @Inject(LoggerService) private readonly logger: LoggerService,
  ) {}

  catch(exception: unknown, host: ArgumentsHost): void {
    const ctx = host.switchToHttp();
    const response = ctx.getResponse<Response>();
    const request = ctx.getRequest<Request>();

    // Intentar leer el traceId/requestId que el middleware de request ya añadió
    const traceId: string =
      (request as any).requestId ?? uuidv4();

    const { status, errorCode, message, details } =
      this.resolveException(exception);

    const errorResponse: ApiErrorResponse = {
      status,
      errorCode,
      message,
      traceId,
      timestamp: new Date().toISOString(),
      ...(details && details.length > 0 ? { details } : {}),
    };

    // Log con el servicio centralizado
    this.logException(exception, request, traceId, status);

    response.status(status).json(errorResponse);
  }

  // ─────────────────────────────────────────────
  // Resolución del tipo de excepción
  // ─────────────────────────────────────────────

  private resolveException(exception: unknown): {
    status: number;
    errorCode: string;
    message: string;
    details?: string[];
  } {
    // 1. Nuestra excepción de dominio (BaseException)
    if (exception instanceof BaseException) {
      return {
        status: exception.getStatus(),
        errorCode: exception.errorCode,
        message: exception.message,
        details: exception.details,
      };
    }

    // 2. HttpException genérica de NestJS (ValidationPipe, guards, etc.)
    if (exception instanceof HttpException) {
      const status = exception.getStatus();
      const exceptionBody = exception.getResponse();

      // ValidationPipe lanza objetos con { message: string[] }
      if (
        typeof exceptionBody === 'object' &&
        exceptionBody !== null
      ) {
        const body = exceptionBody as Record<string, any>;

        // Si el httpClient ya formateó el error (viene del Core .NET)
        if (body.errorCode) {
          return {
            status,
            errorCode: String(body.errorCode),
            message: String(body.message ?? 'An error occurred.'),
          };
        }

        // ValidationPipe con array de mensajes
        if (Array.isArray(body.message)) {
          return {
            status,
            errorCode: this.httpStatusToErrorCode(status),
            message: 'The request contains validation errors.',
            details: body.message as string[],
          };
        }

        return {
          status,
          errorCode: this.httpStatusToErrorCode(status),
          message: String(body.message ?? exception.message),
        };
      }

      return {
        status,
        errorCode: this.httpStatusToErrorCode(status),
        message: String(exceptionBody),
      };
    }

    // 3. Error nativo de JS (TypeError, ReferenceError, etc.)
    if (exception instanceof Error) {
      return {
        status: HttpStatus.INTERNAL_SERVER_ERROR,
        errorCode: 'INTERNAL_SERVER_ERROR',
        message: 'An unexpected error occurred. Please try again later.',
      };
    }

    // 4. Cualquier otro valor lanzado (string, number, etc.)
    return {
      status: HttpStatus.INTERNAL_SERVER_ERROR,
      errorCode: 'INTERNAL_SERVER_ERROR',
      message: 'An unexpected error occurred. Please try again later.',
    };
  }

  // ─────────────────────────────────────────────
  // Logging del error
  // ─────────────────────────────────────────────

  private logException(
    exception: unknown,
    request: Request,
    traceId: string,
    status: number,
  ): void {
    const baseMetadata = {
      service: 'api-gateway',
      environment: process.env.NODE_ENV ?? 'development',
      timestamp: new Date().toISOString(),
      requestId: traceId,
      method: request.method,
      path: request.url,
      statusCode: status,
    };

    if (status >= 500) {
      // Error interno: loguear con stack trace completo
      if (exception instanceof Error) {
        this.logger.logError(exception, baseMetadata);
      } else {
        this.logger.error('Unhandled non-Error exception', baseMetadata);
      }
    } else if (status >= 400) {
      // Error del cliente: solo WARN, sin stack trace
      const message =
        exception instanceof Error ? exception.message : String(exception);
      this.logger.warn(`Client error [${status}]: ${message}`, baseMetadata);
    }
  }

  // ─────────────────────────────────────────────
  // Mapeo de código HTTP a errorCode legible
  // ─────────────────────────────────────────────

  private httpStatusToErrorCode(status: number): string {
    const map: Record<number, string> = {
      400: 'BAD_REQUEST',
      401: 'UNAUTHORIZED',
      403: 'FORBIDDEN',
      404: 'NOT_FOUND',
      409: 'CONFLICT',
      422: 'VALIDATION_ERROR',
      429: 'TOO_MANY_REQUESTS',
      500: 'INTERNAL_SERVER_ERROR',
      503: 'SERVICE_UNAVAILABLE',
    };
    return map[status] ?? 'UNKNOWN_ERROR';
  }
}