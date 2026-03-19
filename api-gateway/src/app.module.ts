import { Module, NestModule, MiddlewareConsumer } from "@nestjs/common";
import { ConfigModule, ConfigService } from "@nestjs/config";
import { JwtModule } from "@nestjs/jwt";
import { MailerModule } from "@nestjs-modules/mailer";
import { HandlebarsAdapter } from "@nestjs-modules/mailer/dist/adapters/handlebars.adapter";
import { CacheModule } from "@nestjs/cache-manager";
import { HealthModule } from "./health/health.module";
import { RedisModule } from "./infrastucture/cache/redis.module";
import { KafkaModule } from "./infrastucture/messaging/kafka.module";
import { LoggerModule } from "./common/logger/logger.module";
import { RequestLoggerMiddleware } from "./common/middleware/request-logger.middleware";
import { UploaderModule } from "./infrastucture/media/cloudinary.module";
import { OAuthModule } from "./modules/oauth/oauth.module.js";
import { AuthModule } from "./modules/auth/auth.module";
import * as path from "path";
// import { DatabaseModule } from "./infrastucture/database/database.module";

@Module({
    imports: [
        ConfigModule.forRoot({ isGlobal: true }),
        CacheModule.register({ isGlobal: true }),
        JwtModule.registerAsync({
            global: true,
            useFactory: async (configService: ConfigService) => ({
                secret: configService.get<string>("JWT_SECRET") || "your-secret-key",
                signOptions: {
                    expiresIn: 86400, // 24 hours in seconds
                },
            }),
            inject: [ConfigService],
        }),
        MailerModule.forRootAsync({
            useFactory: async (configService: ConfigService) => ({
                transport: {
                    host: configService.get<string>("SMTP_HOST") || "localhost",
                    port: parseInt(configService.get<string>("SMTP_PORT") || "587"),
                    secure: configService.get<string>("SMTP_SECURE") === "true",
                    auth: {
                        user: configService.get<string>("SMTP_USER"),
                        pass: configService.get<string>("SMTP_PASSWORD"),
                    },
                },
                defaults: {
                    from: configService.get<string>("SMTP_FROM") || "noreply@skillmind.com",
                },
                template: {
                    dir: path.join(__dirname, "../templates/emails"),
                    adapter: new HandlebarsAdapter(),
                    options: {
                        strict: true,
                    },
                },
            }),
            inject: [ConfigService],
        }),
        LoggerModule,
        HealthModule,
        RedisModule,
        KafkaModule,
        UploaderModule,
        OAuthModule,
        AuthModule,
        // DatabaseModule
    ]
})

export class AppModule implements NestModule{
    configure(consumer: MiddlewareConsumer) {
        consumer
        .apply(RequestLoggerMiddleware)
        .forRoutes('*') //'*' Aplicar a todas las rutas 
    }
}