import { NestFactory } from '@nestjs/core';
import { Transport } from '@nestjs/microservices';
import { ValidationPipe } from '@nestjs/common';
import * as dotenv from 'dotenv';
import { AppModule } from './app.module';

dotenv.config();

async function bootstrap() {
  const app = await NestFactory.createMicroservice(AppModule, {
    transport: Transport.TCP,
    options: { host: 'localhost', port: parseInt(process.env.PORT || '8878') },
  });
  app.useGlobalPipes(new ValidationPipe());
  await app.listen();
  console.log(`Player Data TCP Microservice listening on port ${process.env.PORT || '8878'}`);
}
bootstrap();