import { BadRequestException, Controller, Headers, Post, type RawBodyRequest, Req } from '@nestjs/common';
import { ApiOperation, ApiResponse, ApiTags } from '@nestjs/swagger';
import { Request } from 'express';
import { PaymentService } from './payment.service';

/**
 * Stripe webhook receiver.
 * This endpoint is intentionally NOT behind AuthGuard — Stripe cannot send a JWT.
 * The raw body is forwarded to the Core service which validates the Stripe-Signature.
 */
@ApiTags('Payment')
@Controller('payment')
export class PaymentWebhookController {
    constructor(private readonly paymentService: PaymentService) {}

    @Post('webhook')
    @ApiOperation({ summary: 'Stripe webhook endpoint (no auth required)' })
    @ApiResponse({ status: 200, description: 'Webhook processed' })
    @ApiResponse({ status: 400, description: 'Missing or invalid Stripe-Signature' })
    async handleWebhook(
        @Req() req: RawBodyRequest<Request>,
        @Headers('stripe-signature') stripeSignature: string
    ): Promise<{ received: boolean }> {
        if (!stripeSignature) {
            throw new BadRequestException('Missing Stripe-Signature header');
        }

        const rawBody = req.rawBody;
        if (!rawBody) {
            throw new BadRequestException('Raw body unavailable');
        }

        await this.paymentService.forwardWebhook(rawBody, stripeSignature);
        return { received: true };
    }
}
