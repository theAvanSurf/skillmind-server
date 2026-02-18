import { Controller, Post, Body, Get } from '@nestjs/common';
import { NotificationsService } from './notifications.service';
import { SendNotificationDto } from './dto/send-notification.dto';

@Controller('notifications')
export class NotificationsController {
  constructor(private readonly notificationsService: NotificationsService) {}

  @Post('send')
  async send(@Body() dto: SendNotificationDto) {
    await this.notificationsService.dispatch(dto);
    return { success: true, message: 'Notification dispatched' };
  }

  @Get('health')
  health() {
    return { status: 'ok', service: 'notification-service' };
  }
}