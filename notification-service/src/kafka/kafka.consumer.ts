import { Controller, Logger } from '@nestjs/common';
import { EventPattern, Payload } from '@nestjs/microservices';
import { NotificationsService } from '../notifications/notifications.service';
import { SendNotificationDto, NotificationType } from '../notifications/dto/send-notification.dto';

@Controller()
export class KafkaConsumer {
  private readonly logger = new Logger(KafkaConsumer.name);

  constructor(private readonly notificationsService: NotificationsService) {}

  @EventPattern('notification.send')
  async handleNotification(@Payload() payload: SendNotificationDto) {
    this.logger.log(`Received: ${JSON.stringify(payload)}`);
    await this.notificationsService.dispatch(payload);
  }

  @EventPattern('user.registered')
  async handleUserRegistered(@Payload() payload: any) {
    await this.notificationsService.dispatch({
      type: NotificationType.EMAIL,
      to: payload.email,
      subject: 'Bienvenido a Skillmind 🎉',
      body: `Hola ${payload.name}, gracias por registrarte.`,
    });
  }
}