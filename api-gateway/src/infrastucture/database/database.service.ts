import { DataSource } from 'typeorm';
import { AppDataSource } from '../../config/data-source';
import { Global, Injectable, OnModuleDestroy, OnModuleInit } from '@nestjs/common';

@Global()
@Injectable()
export class DatabaseService implements OnModuleInit, OnModuleDestroy {
  private AppDataSource: DataSource = AppDataSource;

  async onModuleInit() {
    if (!this.AppDataSource.isInitialized) {
      await this.AppDataSource.initialize();
    }
  }

  async onModuleDestroy() {
    if (this.AppDataSource.isInitialized) {
      await this.AppDataSource.destroy();
    }
  }

  getDataSource(): DataSource {
    return this.AppDataSource;
  }
}
