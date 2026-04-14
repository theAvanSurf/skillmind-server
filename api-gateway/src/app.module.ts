import { Module, NestModule, MiddlewareConsumer } from "@nestjs/common";
import { ConfigModule } from "@nestjs/config";
import { HealthModule } from "./health/health.module";
import { RedisModule } from "./infrastucture/cache/redis.module";
import { KafkaModule } from "./infrastucture/messaging/kafka.module";
import { LoggerModule } from "./common/logger/logger.module";
import { RequestLoggerMiddleware } from "./common/middleware/request-logger.middleware";
import { UploaderModule } from "./infrastucture/media/cloudinary.module";
import { AuthModule } from "./modules/auth/auth.module";
import { ProfilesModule } from "./modules/profiles/profiles.module";
import { SessionsModule } from "./modules/sessions/sessions.module";
import { PaymentModule } from "./modules/payment/payment.module";
import { CoursesModule } from "./modules/courses/courses.module";
import { RecommendationsModule } from "./modules/recommendations/recommendations.module";
import { ProfessorModule } from "./modules/professor/professor.module";


@Module({
    imports: [
        ConfigModule.forRoot({ isGlobal: true }),
        LoggerModule,
        HealthModule,
        RedisModule,
        KafkaModule,
        UploaderModule,
        AuthModule,
        ProfilesModule,
        SessionsModule,
        PaymentModule,
        CoursesModule,
        RecommendationsModule,
        ProfessorModule,
    ]
})

export class AppModule implements NestModule {
    configure(consumer: MiddlewareConsumer) {
        consumer
            .apply(RequestLoggerMiddleware)
            .forRoutes('*') //'*' Aplicar a todas las rutas 
    }
}