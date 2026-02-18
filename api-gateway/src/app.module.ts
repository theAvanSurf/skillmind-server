import { Module, NestModule, MiddlewareConsumer } from "@nestjs/common";
import { ConfigModule } from "@nestjs/config";
import { HealthModule } from "./health/health.module";
import { RedisModule } from "./infrastucture/cache/redis.module";
import { KafkaModule } from "./infrastucture/messaging/kafka.module";
import { LoggerModule } from "./common/logger/logger.module";
import { RequestLoggerMiddleware } from "./common/middleware/request-logger.middleware";
import { UploaderModule } from "./infrastucture/media/cloudinary.module";
<<<<<<< Updated upstream
import { OAuthModule } from "./modules/oauth/oauth.module.js";
=======
>>>>>>> Stashed changes

@Module({
    imports: [
        ConfigModule.forRoot({ isGlobal: true }),
        LoggerModule,
        HealthModule,
        RedisModule,
        KafkaModule,
        UploaderModule,
<<<<<<< Updated upstream
        OAuthModule,
=======
>>>>>>> Stashed changes
    ]
})

export class AppModule implements NestModule{
    configure(consumer: MiddlewareConsumer) {
        consumer
        .apply(RequestLoggerMiddleware)
        .forRoutes('*') //'*' Aplicar a todas las rutas 
    }
}