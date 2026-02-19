import { Injectable, Logger } from '@nestjs/common';
import axios from 'axios';

@Injectable()
export class WebhookService {
  private readonly logger = new Logger(WebhookService.name);

  async send(url: string, body: string, metadata?: Record<string, any>): Promise<void> {
    await axios.post(url, { body, ...metadata, timestamp: new Date().toISOString() }, {
      headers: { 'Content-Type': 'application/json', 'X-Skillmind-Webhook': 'true' },
      timeout: 5000,
    });
    this.logger.log(`Webhook sent to ${url}`);
  }
}