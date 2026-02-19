import { Module } from '@nestjs/common';
import { ConfigModule } from '@nestjs/config';
import { NotificationsModule } from './notifications/notifications.module';
import { KafkaModule } from './kafka/kafka.module';
import { EmailModule } from './email/email.module';
import { PushModule } from './push/push.module';
import { WebhookModule } from './webhook/webhook.module';

@Module({
  imports: [
    ConfigModule.forRoot({ isGlobal: true }),
    KafkaModule,
    EmailModule,
    PushModule,
    WebhookModule,
    NotificationsModule,
  ],
})
export class AppModule {}