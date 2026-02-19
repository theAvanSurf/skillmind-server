import { Module } from '@nestjs/common';
import { ClientsModule, Transport } from '@nestjs/microservices';
import { KafkaConsumer } from './kafka.consumer';
import { NotificationsModule } from '../notifications/notifications.module';

@Module({
  imports: [
    ClientsModule.register([
      {
        name: 'KAFKA_CLIENT',
        transport: Transport.KAFKA,
        options: {
          client: {
            clientId: 'notification-service',
            brokers: [process.env.KAFKA_BROKER ?? 'localhost:9092'],
          },
          consumer: { groupId: 'notification-consumer-group' },
        },
      },
    ]),
    NotificationsModule,
  ],
  controllers: [KafkaConsumer],
})
export class KafkaModule { }