import { Inject, Injectable } from '@nestjs/common';
import { ConfigService } from '@nestjs/config';
import { HttpService } from '@nestjs/axios';
import { ClientKafka } from '@nestjs/microservices';
import { firstValueFrom } from 'rxjs';
import { randomBytes, createHmac } from 'crypto';

import {
  OAUTH_PROVIDERS,
  type OAuthProviderConfig,
} from './oauth-providers.config.js';
import {
  OAuthProviderException,
  OAuthCallbackException,
  OAuthUnsupportedProviderException,
} from './oauth.exceptions.js';

@Injectable()
export class OAuthService {
  constructor(
    private readonly config: ConfigService,
    private readonly http: HttpService,
    @Inject('KAFKA_SERVICE') private readonly kafkaClient: ClientKafka,
  ) {}



  private getProviderConfig(provider: string): OAuthProviderConfig {
    const cfg = OAUTH_PROVIDERS[provider];
    if (!cfg) throw new OAuthUnsupportedProviderException(provider);
    return cfg;
  }

  private env(key: string): string {
    const value = this.config.get<string>(key);
    if (!value) throw new Error(`Missing env variable: ${key}`);
    return value;
  }


  buildAuthorizationUrl(provider: string): { url: string; state: string } {
    const cfg = this.getProviderConfig(provider);

    const clientId = this.env(cfg.clientIdEnvKey);
    const redirectUri = this.env(cfg.redirectUriEnvKey);
    const scopes = this.env(cfg.scopesEnvKey);

    const state = this.generateState();

    const params = new URLSearchParams({
      client_id: clientId,
      redirect_uri: redirectUri,
      response_type: 'code',
      scope: scopes,
      state,
      access_type: 'offline',
      prompt: 'consent',
    });

    return { url: `${cfg.authorizeUrl}?${params.toString()}`, state };
  }



  async exchangeCodeForToken(
    provider: string,
    code: string,
  ): Promise<string> {
    const cfg = this.getProviderConfig(provider);

    const body = {
      code,
      client_id: this.env(cfg.clientIdEnvKey),
      client_secret: this.env(cfg.clientSecretEnvKey),
      redirect_uri: this.env(cfg.redirectUriEnvKey),
      grant_type: 'authorization_code',
    };

    try {
      const { data } = await firstValueFrom(
        this.http.post(cfg.tokenUrl, body, {
          headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        }),
      );
      return data.access_token as string;
    } catch (err: any) {
      const detail =
        err?.response?.data?.error_description ??
        err?.message ??
        'token exchange failed';
      throw new OAuthProviderException(provider, detail);
    }
  }



  async getUserProfile(
    provider: string,
    accessToken: string,
  ): Promise<Record<string, any>> {
    const cfg = this.getProviderConfig(provider);

    try {
      const { data } = await firstValueFrom(
        this.http.get(cfg.userInfoUrl, {
          headers: { Authorization: `Bearer ${accessToken}` },
        }),
      );
      return data;
    } catch (err: any) {
      const detail =
        err?.response?.data?.error ?? err?.message ?? 'profile fetch failed';
      throw new OAuthProviderException(provider, detail);
    }
  }



  async delegateToAuthService(
    provider: string,
    profile: Record<string, any>,
    accessToken: string,
  ): Promise<{ accessToken: string }> {
    const payload = { provider, profile, providerAccessToken: accessToken };

    try {
      const result = await firstValueFrom(
        this.kafkaClient.send('auth.oauth.login', payload),
      );
      return result as { accessToken: string };
    } catch (err: any) {
      throw new OAuthProviderException(
        provider,
        err?.message ?? 'auth microservice communication failed',
      );
    }
  }


  async handleCallback(
    provider: string,
    code: string,
    state: string,
    storedState: string | undefined,
  ): Promise<{ accessToken: string }> {

    if (!storedState || storedState !== state) {
      throw new OAuthCallbackException(
        'Invalid or missing OAuth state — possible CSRF attempt',
      );
    }

    const accessToken = await this.exchangeCodeForToken(provider, code);
    const profile = await this.getUserProfile(provider, accessToken);
    return this.delegateToAuthService(provider, profile, accessToken);
  }



  generateState(): string {
    const raw = randomBytes(32).toString('hex');
    const secret = this.config.get<string>('OAUTH_STATE_SECRET') ?? '';
    if (secret) {
      return createHmac('sha256', secret).update(raw).digest('hex');
    }
    return raw;
  }


  async onModuleInit() {
    this.kafkaClient.subscribeToResponseOf('auth.oauth.login');
    try {
      await this.kafkaClient.connect();
    } catch {
      console.warn('[OAuthService] Kafka not available — auth.oauth.login will fail until Kafka is up');
    }
  }
}
