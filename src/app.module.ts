import { Module } from "@nestjs/common";
import { HealthModule } from "./health/health.module";
import { RedisModule } from "./infrastucture/cache/redis.module";
import { KafkaModule } from "./infrastucture/messaging/kafka.module";

@Module({
    imports: [
        HealthModule,
        RedisModule,
        KafkaModule
    ]
})

export class AppModule {}