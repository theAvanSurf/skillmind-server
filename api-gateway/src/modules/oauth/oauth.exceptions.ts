import { HttpException, HttpStatus } from '@nestjs/common';

/**
 * El proveedor OAuth devolvió un error o el token-exchange falló.
 * Se traduce a 502 Bad Gateway.
 */
export class OAuthProviderException extends HttpException {
  constructor(provider: string, detail: string) {
    super(
      {
        statusCode: HttpStatus.BAD_GATEWAY,
        error: 'OAuthProviderError',
        message: `Error from OAuth provider [${provider}]: ${detail}`,
      },
      HttpStatus.BAD_GATEWAY,
    );
  }
}

/**
 * El callback llegó sin code o con state inválido.
 * Se traduce a 401 Unauthorized.
 */
export class OAuthCallbackException extends HttpException {
  constructor(reason: string) {
    super(
      {
        statusCode: HttpStatus.UNAUTHORIZED,
        error: 'OAuthCallbackError',
        message: reason,
      },
      HttpStatus.UNAUTHORIZED,
    );
  }
}

/**
 * Se solicitó un proveedor que no está configurado.
 */
export class OAuthUnsupportedProviderException extends HttpException {
  constructor(provider: string) {
    super(
      {
        statusCode: HttpStatus.BAD_REQUEST,
        error: 'OAuthUnsupportedProvider',
        message: `OAuth provider "${provider}" is not supported`,
      },
      HttpStatus.BAD_REQUEST,
    );
  }
}
