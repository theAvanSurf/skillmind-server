import { Injectable, Logger } from '@nestjs/common';
import * as nodemailer from 'nodemailer';

@Injectable()
export class EmailService {
  private readonly logger = new Logger(EmailService.name);
  private transporter = nodemailer.createTransport({
    host: process.env.SMTP_HOST ?? 'smtp.gmail.com',
    port: parseInt(process.env.SMTP_PORT ?? '587'),
    secure: false,
    auth: {
      user: process.env.SMTP_USER,
      pass: process.env.SMTP_PASS,
    },
  });

  async send(to: string, subject: string, body: string): Promise<void> {
    await this.transporter.sendMail({
      from: `"Skillmind" <${process.env.SMTP_USER}>`,
      to, subject, html: body,
    });
    this.logger.log(`Email sent to ${to}`);
  }
}