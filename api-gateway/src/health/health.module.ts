import { Module } from "@nestjs/common";
import { ApiHealthController } from "./health.controller";
import { RedisModule } from "../infrastucture/cache/redis.module";
import { KafkaModule } from "../infrastucture/messaging/kafka.module";

@Module({
    imports: [RedisModule, KafkaModule],
    controllers: [ApiHealthController]
})
export class HealthModule {}