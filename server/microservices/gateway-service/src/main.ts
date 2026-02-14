import { NestFactory } from '@nestjs/core';
import { ValidationPipe } from '@nestjs/common';
import { AppModule } from './app.module';
import * as fs from 'fs';
import * as path from 'path';
import * as dotenv from 'dotenv';

dotenv.config();

async function bootstrap() {
  const httpsOptions = {
    key: fs.readFileSync(path.join(process.cwd(), 'certs', 'localhost-key.pem')),
    cert: fs.readFileSync(path.join(process.cwd(), 'certs', 'localhost.pem')),
  };

  const app = await NestFactory.create(AppModule, { httpsOptions });
  app.useGlobalPipes(new ValidationPipe());
  await app.listen(process.env.PORT || 3000, '0.0.0.0');
  console.log(`Gateway HTTPS API listening on https://0.0.0.0:${process.env.PORT || 3000}`);
}
bootstrap();