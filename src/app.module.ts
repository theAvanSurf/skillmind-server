import { Module, NestModule, MiddlewareConsumer } from "@nestjs/common";
import { HealthModule } from "./health/health.module";
import { RedisModule } from "./infrastucture/cache/redis.module";
import { KafkaModule } from "./infrastucture/messaging/kafka.module";
import { LoggerModule } from "./common/logger/logger.module";
import { RequestLoggerMiddleware } from "./common/middleware/request-logger.middleware";

@Module({
    imports: [
        LoggerModule,
        HealthModule,
        RedisModule,
        KafkaModule
    ]
})

export class AppModule implements NestModule{
    configure(consumer: MiddlewareConsumer) {
        consumer
        .apply(RequestLoggerMiddleware)
        .forRoutes('*') //'*' Aplicar a todas las rutas 
    }


}