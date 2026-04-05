import { Injectable } from '@nestjs/common';
import httpClient from '../../config/baseHttpClient';
import { API_ENDPOINTS } from '../../common/endpoints';
import { CreateCheckoutSessionDto, CreatePortalSessionDto, SessionStatusDto, SubscriptionClientSecretDto, SubscriptionDto } from './payment.dto';

@Injectable()
export class PaymentService {
    private resolveLookupKey(lookupKey?: string): string {
        if (lookupKey?.trim()) return lookupKey.trim();

        return (
            process.env.PAYMENT_DEFAULT_LOOKUP_KEY?.trim() ||
            process.env.STRIPE_DEFAULT_LOOKUP_KEY?.trim() ||
            'Skillmind_Premium_Plan-42a1204'
        );
    }

    async createCheckoutSession(dto: CreateCheckoutSessionDto, token: string): Promise<{ clientSecret: string }> {
        const payload: CreateCheckoutSessionDto = {
            ...dto,
            lookupKey: this.resolveLookupKey(dto.lookupKey),
        };

        return await httpClient.post<{ clientSecret: string }>(
            API_ENDPOINTS.CORE.PAYMENT_CREATE_CHECKOUT_SESSION,
            payload,
            { headers: { Authorization: token } }
        ) as unknown as { clientSecret: string };
    }

    async createSubscription(dto: CreateCheckoutSessionDto, token: string): Promise<SubscriptionClientSecretDto> {
        const payload: CreateCheckoutSessionDto = {
            ...dto,
            lookupKey: this.resolveLookupKey(dto.lookupKey),
        };

        return await httpClient.post<SubscriptionClientSecretDto>(
            API_ENDPOINTS.CORE.PAYMENT_CREATE_SUBSCRIPTION,
            payload,
            { headers: { Authorization: token } }
        ) as unknown as SubscriptionClientSecretDto;
    }

    async getSessionStatus(sessionId: string, token: string): Promise<SessionStatusDto> {
        return await httpClient.get<SessionStatusDto>(
            API_ENDPOINTS.CORE.PAYMENT_SESSION_STATUS,
            {
                params: { sessionId },
                headers: { Authorization: token },
            }
        ) as unknown as SessionStatusDto;
    }

    async createPortalSession(dto: CreatePortalSessionDto, token: string): Promise<{ url: string }> {
        return await httpClient.post<{ url: string }>(
            API_ENDPOINTS.CORE.PAYMENT_CREATE_PORTAL_SESSION,
            dto,
            { headers: { Authorization: token } }
        ) as unknown as { url: string };
    }

    async getSubscription(token: string): Promise<SubscriptionDto> {
        return await httpClient.get<SubscriptionDto>(
            API_ENDPOINTS.CORE.PAYMENT_SUBSCRIPTION,
            { headers: { Authorization: token } }
        ) as unknown as SubscriptionDto;
    }

    async forwardWebhook(rawBody: Buffer, stripeSignature: string): Promise<void> {
        await httpClient.post(
            API_ENDPOINTS.CORE.PAYMENT_WEBHOOK,
            rawBody,
            {
                headers: {
                    'Content-Type': 'application/json',
                    'Stripe-Signature': stripeSignature,
                },
            }
        );
    }
}
