/**
 * Standard error response format
 * All API errors follow this structure
 */
export interface ErrorResponse {
  /**
   * Status indicator (always "error" for errors)
   */
  status: 'error';

  /**
   * HTTP status code
   */
  statusCode: number;

  /**
   * Human-readable error message
   */
  message: string;

  /**
   * ISO-8601 timestamp when error occurred
   */
  timestamp: string;

  /**
   * Request path where error occurred
   */
  path: string;

  /**
   * Optional: Request ID for tracing
   */
  requestId?: string;

  /**
   * Optional: Validation errors (for 400 responses)
   */
  errors?: any;

  /**
   * Optional: Stack trace (only in development)
   */
  stack?: string;
}
