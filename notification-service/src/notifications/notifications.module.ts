import { Module } from '@nestjs/common';
import { NotificationsController } from './notifications.controller';
import { NotificationsService } from './notifications.service';
import { EmailModule } from '../email/email.module';
import { PushModule } from '../push/push.module';
import { WebhookModule } from '../webhook/webhook.module';

@Module({
  imports: [EmailModule, PushModule, WebhookModule],
  controllers: [NotificationsController],
  providers: [NotificationsService],
  exports: [NotificationsService],
})
export class NotificationsModule {}