 
 
 
 

import { Injectable, LoggerService as NestLoggerService } from '@nestjs/common';
import * as winston from 'winston';
import DailyRotateFile from 'winston-daily-rotate-file';
import { ILogger, LogLevel, LogMetadata } from './interfaces/logger.interface';
import { redactSensitiveData } from './utils/redact.util'; // ← Asegúrate que el archivo se llame redact.util.ts

/**
 * Servicio centralizado de logging
 * Implementa la interfaz ILogger y NestLoggerService
 */
@Injectable()
export class LoggerService implements ILogger, NestLoggerService {
  private logger: winston.Logger;
  private context: string = 'Application';

  constructor() {
    this.logger = this.createLogger();
  }

  /**
   * Crea y configura el logger de Winston
   */
  private createLogger(): winston.Logger {
    const isProduction = process.env.NODE_ENV === 'production';

// Formato para desarrollo (legible para humanos)
const devFormat = winston.format.combine(
  winston.format.colorize(),
  winston.format.timestamp({ format: 'YYYY-MM-DD HH:mm:ss' }),
  winston.format.printf(({ timestamp, level, message, context, ...meta }) => {
    const metaString = Object.keys(meta).length ? JSON.stringify(meta, null, 2) : '';
    const ctx = typeof context === 'string' ? context : (this.context || 'Application');
    return `${String(timestamp)} [${ctx}] ${String(level)}: ${String(message)} ${metaString}`;
  }),
);

    // Formato para producción (JSON estructurado)
    const prodFormat = winston.format.combine(
      winston.format.timestamp(),
      winston.format.errors({ stack: true }),
      winston.format.json(),
    );

    // Transportes (dónde se guardan los logs)
    const transports: winston.transport[] = [
      // Consola (siempre activa)
      new winston.transports.Console({
        format: isProduction ? prodFormat : devFormat,
      }),
    ];

    // En producción, agregar archivos rotativos
    if (isProduction) {
      // Logs generales
      transports.push(
        new DailyRotateFile({
          filename: 'logs/application-%DATE%.log',
          datePattern: 'YYYY-MM-DD',
          maxSize: '20m',
          maxFiles: '14d',
          format: prodFormat,
        }) as winston.transport,
      );

      // Logs de errores separados
      transports.push(
        new DailyRotateFile({
          filename: 'logs/error-%DATE%.log',
          datePattern: 'YYYY-MM-DD',
          level: 'error',
          maxSize: '20m',
          maxFiles: '30d',
          format: prodFormat,
        }) as winston.transport,
      );
    }

    return winston.createLogger({
      level: isProduction ? 'info' : 'debug',
      transports,
    });
  }

  /**
   * Prepara la metadata para el log (agrega valores por defecto y redacta datos sensibles)
   */
  private prepareMetadata(metadata?: LogMetadata): LogMetadata {
    const baseMetadata: LogMetadata = {
      service: 'api-gateway',
      environment: process.env.NODE_ENV || 'development',
      timestamp: new Date().toISOString(),
      ...metadata,
    };

    return redactSensitiveData(baseMetadata) as LogMetadata;
  }

  /**
   * Método genérico para loggear con cualquier nivel
   */
  private logInternal(level: LogLevel, message: string, metadata?: LogMetadata): void {
    const preparedMetadata = this.prepareMetadata(metadata);
    this.logger.log(level, message, preparedMetadata);
  }

  // Implementación de ILogger
  debug(message: string, metadata?: LogMetadata): void {
    this.logInternal(LogLevel.DEBUG, message, metadata);
  }

  info(message: string, metadata?: LogMetadata): void {
    this.logInternal(LogLevel.INFO, message, metadata);
  }

  warn(message: string, metadata?: LogMetadata): void {
    this.logInternal(LogLevel.WARN, message, metadata);
  }

  error(message: string, metadata?: LogMetadata): void {
    this.logInternal(LogLevel.ERROR, message, metadata);
  }

  fatal(message: string, metadata?: LogMetadata): void {
    this.logInternal(LogLevel.FATAL, message, metadata);
  }

  /**
   * Loggea una petición HTTP completa
   */
  logRequest(metadata: LogMetadata): void {
    this.info('Incoming request', metadata);
  }

  /**
   * Loggea un error con stack trace
   */
  logError(error: Error, metadata?: LogMetadata): void {
    const errorMetadata: LogMetadata = {
      service: metadata?.service || 'api-gateway',
      environment: metadata?.environment || process.env.NODE_ENV || 'development',
      timestamp: metadata?.timestamp || new Date().toISOString(),
      ...metadata,
      errorName: error.name,
      errorMessage: error.message,
      stack: error.stack,
    };

    this.error(error.message, errorMetadata);
  }

  // Implementación de NestLoggerService (para reemplazar el logger por defecto de NestJS)
  log(message: any, context?: string): void {
    const metadata: LogMetadata = {
      service: 'api-gateway',
      environment: process.env.NODE_ENV || 'development',
      timestamp: new Date().toISOString(),
      context,
    };
    this.info(String(message), metadata);
  }

  verbose(message: any, context?: string): void {
    const metadata: LogMetadata = {
      service: 'api-gateway',
      environment: process.env.NODE_ENV || 'development',
      timestamp: new Date().toISOString(),
      context,
    };
    this.debug(String(message), metadata);
  }

  // Estos métodos ya existen arriba, pero NestJS los requiere con diferentes firmas
  // Por eso los redefinimos aquí para compatibilidad
  setContext(context: string): void {
    this.context = context;
  }
}
