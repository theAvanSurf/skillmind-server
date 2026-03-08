import { Body, Controller, Get, Headers, Post, Query, UseGuards } from '@nestjs/common';
import { ApiBearerAuth, ApiBody, ApiQuery, ApiResponse, ApiTags } from '@nestjs/swagger';
import { PaymentService } from './payment.service';
import { AuthGuard } from '../../common/guards/auth.guard';
import { CreateCheckoutSessionDto, CreatePortalSessionDto, SessionStatusDto, SubscriptionClientSecretDto, SubscriptionDto } from './payment.dto';

@ApiTags('Payment')
@ApiBearerAuth()
@UseGuards(AuthGuard)
@Controller('payment')
export class PaymentController {
    constructor(private readonly paymentService: PaymentService) {}

    @Post('create-subscription')
    @ApiBody({ type: CreateCheckoutSessionDto })
    @ApiResponse({ status: 200, type: SubscriptionClientSecretDto, description: 'PaymentIntent client secret for Stripe Elements' })
    @ApiResponse({ status: 401, description: 'Unauthorized' })
    async createSubscription(
        @Body() dto: CreateCheckoutSessionDto,
        @Headers('authorization') token: string
    ): Promise<SubscriptionClientSecretDto> {
        return this.paymentService.createSubscription(dto, token);
    }

    @Post('create-checkout-session')
    @ApiBody({ type: CreateCheckoutSessionDto })
    @ApiResponse({ status: 200, description: 'Stripe client secret for Embedded Checkout' })
    @ApiResponse({ status: 401, description: 'Unauthorized' })
    async createCheckoutSession(
        @Body() dto: CreateCheckoutSessionDto,
        @Headers('authorization') token: string
    ): Promise<{ clientSecret: string }> {
        return this.paymentService.createCheckoutSession(dto, token);
    }

    @Get('session-status')
    @ApiQuery({ name: 'sessionId', type: String, required: true })
    @ApiResponse({ status: 200, type: SessionStatusDto })
    async getSessionStatus(
        @Query('sessionId') sessionId: string,
        @Headers('authorization') token: string
    ): Promise<SessionStatusDto> {
        return this.paymentService.getSessionStatus(sessionId, token);
    }

    @Post('create-portal-session')
    @ApiBody({ type: CreatePortalSessionDto })
    @ApiResponse({ status: 200, description: 'Stripe Billing Portal URL' })
    async createPortalSession(
        @Body() dto: CreatePortalSessionDto,
        @Headers('authorization') token: string
    ): Promise<{ url: string }> {
        return this.paymentService.createPortalSession(dto, token);
    }

    @Get('subscription')
    @ApiResponse({ status: 200, type: SubscriptionDto })
    @ApiResponse({ status: 404, description: 'No subscription found' })
    async getSubscription(
        @Headers('authorization') token: string
    ): Promise<SubscriptionDto> {
        return this.paymentService.getSubscription(token);
    }
}
