import { Controller, Get, Inject } from '@nestjs/common';
import { CACHE_MANAGER } from '@nestjs/cache-manager';
import type { Cache } from 'cache-manager';
import { ClientKafka } from '@nestjs/microservices';

@Controller('api-health')
export class ApiHealthController{
    constructor(
        @Inject(CACHE_MANAGER) private cacheManager: Cache,
        @Inject('KAFKA_SERVICE') private kafkaClient: ClientKafka
    ) {}

    @Get()
    health() {
        return{
            apiStatus: "ok",
            upTime: process.uptime(),
            timeStamp: new Date().toISOString()
        }
    }

    @Get('redis-check')
    async checkRedis() {
        await this.cacheManager.set('test-key', 'Redis is working perfectly!', 10000);
        const value = await this.cacheManager.get('test-key');
        return {
            message: 'Redis check completed',
            storedValue: value, 
        };
    }

    @Get('kafka-check')
    async checkKafka() {
        try {
            await this.kafkaClient.connect();
            this.kafkaClient.emit('health_check_topic', { data: 'Health check works' });
            return {
                message: 'Kafka connection successful',
                status: 'ok' 
            };
        } catch (error) {
            return {
                message: 'Kafka connection failed',
                error: error.message
            };
        }
    }
}
