import { Module } from "@nestjs/common";
import { ApiHealthController } from "./health.controller";
import { ErrorTestController } from "./error-test.controller";

@Module({
    controllers: [ApiHealthController, ErrorTestController]
})

export class HealthModule {}