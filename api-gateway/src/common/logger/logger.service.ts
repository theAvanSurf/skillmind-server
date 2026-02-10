import { Injectable, LoggerService as NestLoggerService } from '@nestjs/common';

@Injectable()
export class LoggerService implements NestLoggerService {
  log(message: string, context?: string) {
    this.logMessage('LOG', message, context);
  }

  info(message: string, meta?: any) {
    const metaStr = meta ? ` ${JSON.stringify(meta, null, 2)}` : '';
    this.logMessage('INFO', `${message}${metaStr}`);
  }

  error(message: string, trace?: string, context?: string) {
    this.logMessage('ERROR', message, context, trace);
  }

  warn(message: string, context?: string) {
    this.logMessage('WARN', message, context);
  }

  debug(message: string, context?: string) {
    this.logMessage('DEBUG', message, context);
  }

  verbose(message: string, context?: string) {
    this.logMessage('VERBOSE', message, context);
  }

  private logMessage(level: string, message: string, context?: string, trace?: string) {
    const timestamp = new Date().toISOString();
    const contextStr = context ? ` [${context}]` : '';
    const traceStr = trace ? `\n${trace}` : '';
    
    console.log(`[${timestamp}] [${level}]${contextStr} ${message}${traceStr}`);
  }
}
