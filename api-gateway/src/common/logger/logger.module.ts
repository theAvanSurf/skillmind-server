import { Global, Module } from '@nestjs/common';
import { LoggerService } from './logger.service';

/**
 * Módulo global de logging
 * 
 * @Global() hace que LoggerService esté disponible en TODOS los módulos
 * sin necesidad de importar LoggerModule en cada uno
 */
@Global()
@Module({
  providers: [LoggerService],
  exports: [LoggerService],
})
export class LoggerModule {}