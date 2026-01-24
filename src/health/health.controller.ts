import { Controller, Get } from '@nestjs/common';

@Controller('api-health')
export class ApiHealthController{
    @Get()
    health() {
        return{
            apiStatus: "ok",
            upTime: process.uptime(),
            timeStamp: new Date().toISOString()
        }
    }
}
