/**
 * Configuración de proveedores OAuth.
 *
 * Cada proveedor se define con sus endpoints y env-var keys.
 * El OAuthService usa este mapa para construir URLs y hacer
 * el token-exchange sin crear un service por proveedor.
 */

export interface OAuthProviderConfig {
  clientIdEnvKey: string;
  clientSecretEnvKey: string;
  redirectUriEnvKey: string;
  scopesEnvKey: string;
  authorizeUrl: string;
  tokenUrl: string;
  userInfoUrl: string;
}

export const OAUTH_PROVIDERS: Record<string, OAuthProviderConfig> = {
  google: {
    clientIdEnvKey: 'GOOGLE_CLIENT_ID',
    clientSecretEnvKey: 'GOOGLE_CLIENT_SECRET',
    redirectUriEnvKey: 'GOOGLE_REDIRECT_URI',
    scopesEnvKey: 'GOOGLE_SCOPES',
    authorizeUrl: 'https://accounts.google.com/o/oauth2/v2/auth',
    tokenUrl: 'https://oauth2.googleapis.com/token',
    userInfoUrl: 'https://www.googleapis.com/oauth2/v3/userinfo',
  },
};
