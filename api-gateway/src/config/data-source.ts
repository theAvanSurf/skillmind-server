import { DataSource } from 'typeorm';
import { appConfig } from './settings';
import * as dotenv from 'dotenv';
import * as path from 'path';

// Load env vars from root .env
dotenv.config({ path: path.join(__dirname, '../../../.env') });

export const AppDataSource = new DataSource({
  type: 'postgres',
  host: process.env.DATABASE_HOST || appConfig.DATABASE_HOST,
  port: Number(process.env.DATABASE_PORT || appConfig.DATABASE_PORT),
  username: process.env.DATABASE_USERNAME || appConfig.DATABASE_USERNAME,
  password: process.env.DATABASE_PASSWORD || appConfig.DATABASE_PASSWORD,
  database: process.env.DATABASE_NAME || appConfig.DATABASE_NAME,
  synchronize: false, // Migrations should not use synchronize
  logging: true,
  entities: [path.join(__dirname, '../infrastucture/database/entities/*.entity{.ts,.js}')],
  migrations: [path.join(__dirname, '../infrastucture/database/migrations/*{.ts,.js}')],
  migrationsTableName: 'migrations',
});
