import { Injectable, Logger, OnModuleInit } from '@nestjs/common';
import * as admin from 'firebase-admin';

@Injectable()
export class PushService implements OnModuleInit {
  private readonly logger = new Logger(PushService.name);

  onModuleInit() {
    if (!process.env.FIREBASE_PROJECT_ID) {
      this.logger.warn('Firebase credentials not set, push notifications disabled');
      return;
    }

    if (!admin.apps.length) {
      admin.initializeApp({
        credential: admin.credential.cert({
          projectId: process.env.FIREBASE_PROJECT_ID,
          clientEmail: process.env.FIREBASE_CLIENT_EMAIL,
          privateKey: process.env.FIREBASE_PRIVATE_KEY?.replace(/\\n/g, '\n'),
        }),
      });
      this.logger.log('Firebase Admin initialized');
    }
  }

  async send(fcmToken: string, title: string, body: string, data?: Record<string, any>): Promise<void> {
    if (!admin.apps.length) {
      this.logger.warn('Firebase not initialized, skipping push notification');
      return;
    }

    await admin.messaging().send({
      token: fcmToken,
      notification: { title, body },
      data: data ? Object.fromEntries(Object.entries(data).map(([k, v]) => [k, String(v)])) : {},
    });
    this.logger.log(`Push sent to ${fcmToken.slice(0, 20)}...`);
  }
}