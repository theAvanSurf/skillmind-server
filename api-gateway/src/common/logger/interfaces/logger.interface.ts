/**
 * Niveles de severidad del log
 * Cada nivel indica qué tan grave es el mensaje
 */
export enum LogLevel {
  DEBUG = 'debug',   // Información técnica para desarrollo
  INFO = 'info',     // Información general del flujo
  WARN = 'warn',     // Advertencias que no detienen la app
  ERROR = 'error',   // Errores que afectan funcionalidad
  FATAL = 'fatal',   // Errores críticos que detienen la app
}

/**
 * Metadata que acompaña cada log
 * Esta información ayuda a rastrear y debuggear problemas
 */
export interface LogMetadata {
  requestId?: string;      // ID único del request (para rastrear toda una petición)
  userId?: string;         // ID del usuario (si está autenticado)
  service: string;         // Nombre del servicio (ej: 'api-gateway')
  environment: string;     // Entorno (development, production, etc.) ← CORREGIDO
  timestamp: string;       // Fecha y hora del log
  method?: string;         // Método HTTP (GET, POST, etc.)
  path?: string;           // Ruta del endpoint (/api/users)
  statusCode?: number;     // Código de respuesta HTTP (200, 404, 500)
  duration?: number;       // Cuánto tardó el request (en milisegundos)
  [key: string]: any;      // Permite agregar campos adicionales dinámicamente
}

/**
 * Interfaz del Logger
 * Define los métodos que DEBE tener cualquier clase que implemente logging
 */
export interface ILogger {
  // Métodos para cada nivel de log
  debug(message: string, metadata?: LogMetadata): void;
  info(message: string, metadata?: LogMetadata): void;
  warn(message: string, metadata?: LogMetadata): void;
  error(message: string, metadata?: LogMetadata): void;
  fatal(message: string, metadata?: LogMetadata): void;

  // Métodos especializados
  logRequest(metadata: LogMetadata): void;           // Para loggear requests HTTP ← CORREGIDO
  logError(error: Error, metadata?: LogMetadata): void;  // Para loggear errores con stack trace ← CORREGIDO
}