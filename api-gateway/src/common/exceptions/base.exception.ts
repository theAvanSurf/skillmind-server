import { HttpException, HttpStatus } from '@nestjs/common';

/**
 * Estructura estándar de error que devuelve la API
 */
export interface ApiErrorResponse {
  status: number;
  errorCode: string;
  message: string;
  traceId: string;
  timestamp: string;
  details?: string[];
}

/**
 * Excepción base para todos los errores del dominio de SkillMind.
 * Extiende HttpException de NestJS para integrarse con el sistema de filtros.
 */
export class BaseException extends HttpException {
  public readonly errorCode: string;
  public readonly details?: string[];

  constructor(
    message: string,
    errorCode: string,
    statusCode: HttpStatus,
    details?: string[],
  ) {
    super({ message, errorCode, statusCode, details }, statusCode);
    this.errorCode = errorCode;
    this.details = details;
  }
}