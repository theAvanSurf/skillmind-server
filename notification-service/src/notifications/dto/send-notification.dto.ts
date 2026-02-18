import { IsEnum, IsString, IsOptional, IsObject } from 'class-validator';

export enum NotificationType {
  EMAIL = 'email',
  PUSH = 'push',
  WEBHOOK = 'webhook',
}

export class SendNotificationDto {
  @IsEnum(NotificationType)
  type: NotificationType;

  @IsString()
  to: string;

  @IsOptional()
  @IsString()
  subject?: string;

  @IsOptional()
  @IsString()
  body?: string;

  @IsOptional()
  @IsObject()
  metadata?: Record<string, any>;
}