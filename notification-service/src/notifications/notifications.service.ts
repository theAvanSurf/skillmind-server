import { Injectable, Logger } from '@nestjs/common';
import { SendNotificationDto, NotificationType } from './dto/send-notification.dto';
import { EmailService } from '../email/email.service';
import { PushService } from '../push/push.service';
import { WebhookService } from '../webhook/webhook.service';

@Injectable()
export class NotificationsService {
  private readonly logger = new Logger(NotificationsService.name);

  constructor(
    private readonly emailService: EmailService,
    private readonly pushService: PushService,
    private readonly webhookService: WebhookService,
  ) {}

  async dispatch(dto: SendNotificationDto): Promise<void> {
    switch (dto.type) {
      case NotificationType.EMAIL:
        await this.emailService.send(dto.to, dto.subject!, dto.body!);
        break;
      case NotificationType.PUSH:
        await this.pushService.send(dto.to, dto.subject!, dto.body!, dto.metadata);
        break;
      case NotificationType.WEBHOOK:
        await this.webhookService.send(dto.to, dto.body!, dto.metadata);
        break;
      default:
        this.logger.warn(`Unknown type: ${dto.type}`);
    }
  }
}