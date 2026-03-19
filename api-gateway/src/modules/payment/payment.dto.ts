import { IsNotEmpty, IsString } from 'class-validator';
import { ApiProperty } from '@nestjs/swagger';

export class CreateCheckoutSessionDto {
    @ApiProperty({ example: 'pro_monthly', description: 'Stripe price lookup key' })
    @IsString()
    @IsNotEmpty()
    lookupKey: string;
}

export class SubscriptionClientSecretDto {
    clientSecret: string;
    subscriptionId: string;
}

export class CreatePortalSessionDto {
    @ApiProperty({ example: 'cs_test_xxx', description: 'Stripe Checkout Session ID' })
    @IsString()
    @IsNotEmpty()
    sessionId: string;
}

export class SessionStatusDto {
    status: string;
    customerEmail: string;
}

export class SubscriptionDto {
    id: string;
    userId: string;
    stripeSubscriptionId: string;
    plan: string;
    subscriptionStatus: string;
    currentPeriodEnd: string;
    isInGracePeriod: boolean;
    gracePeriodEnd?: string;
    /** Set when the user started checkout but hasn't completed payment. Used to resume the flow on login. */
    intendedPlan?: string;
}
