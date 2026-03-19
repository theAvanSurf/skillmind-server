/* eslint-disable @typescript-eslint/no-unsafe-assignment */
/* eslint-disable @typescript-eslint/no-unsafe-call */
/* eslint-disable @typescript-eslint/no-unsafe-member-access */

import { Injectable, NestMiddleware } from '@nestjs/common';
import { Request, Response, NextFunction } from 'express';
import { v4 as uuidv4 } from 'uuid';
import { LoggerService } from '../logger/logger.service';

/**
 * Middleware para loggear todas las peticiones HTTP
 *
 * Este middleware:
 * 1. Genera un requestId único para cada petición
 * 2. Loggea cuando llega la petición
 * 3. Loggea cuando termina la petición (con duración y código de estado)
 */
@Injectable()
export class RequestLoggerMiddleware implements NestMiddleware {
  constructor(private readonly logger: LoggerService) {}

  use(req: Request, res: Response, next: NextFunction): void {
    // Generar ID único para esta petición (para rastrearla en todos los logs)
    const requestId = uuidv4();

    // Guardar requestId en el objeto request para usarlo después
    (req as any).requestId = requestId;

    // Devolver el traceId en la respuesta para que el cliente pueda rastrearlo
    res.setHeader('X-Request-ID', requestId);

    // Timestamp de inicio (para calcular duración)
    const startTime = Date.now();

    // Loggear petición entrante
    this.logger.logRequest({
      requestId,
      method: req.method,
      path: req.path,
      service: 'api-gateway',
      environment: process.env.NODE_ENV || 'development',
      timestamp: new Date().toISOString(),
    });

    // Guardar referencia al logger y requestId para usar en el callback
    const logger = this.logger;

    // Interceptar el evento 'finish' de la respuesta
    res.on('finish', () => {
      // Calcular duración de la petición
      const duration = Date.now() - startTime;

      // Loggear respuesta completada
      logger.info('Request completed', {
        requestId,
        method: req.method,
        path: req.path,
        statusCode: res.statusCode,
        duration,
        service: 'api-gateway',
        environment: process.env.NODE_ENV || 'development',
        timestamp: new Date().toISOString(),
      });
    });

    // Continuar con el siguiente middleware/controller
    next();
  }
}