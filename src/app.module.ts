import { Module } from "@nestjs/common";
import { HealthModule } from "./health/health.module";
import { RedisModule } from "./infrastucture/cache/redis.module";
import { KafkaModule } from "./infrastucture/messaging/kafka.module";
import { UploaderModule } from "./infrastucture/media/cloudinary.module";

@Module({
    imports: [
        HealthModule,
        RedisModule,
        KafkaModule,
        UploaderModule
    ]
})

export class AppModule {}