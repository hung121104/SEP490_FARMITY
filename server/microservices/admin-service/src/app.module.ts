import { Module } from '@nestjs/common';
import { MongooseModule } from '@nestjs/mongoose';
import { ConfigModule, ConfigService } from '@nestjs/config';
import { BlogModule } from './blog/blog.module';
import { NewsModule } from './news/news.module';
import { MediaModule } from './media/media.module';
import { CloudinaryModule } from './shared/cloudinary/cloudinary.module';
import { ItemModule } from './game-data/item/item.module';

@Module({
  imports: [
    ConfigModule.forRoot({ isGlobal: true }),
    MongooseModule.forRootAsync({
      imports: [ConfigModule],
      useFactory: async (configService: ConfigService) => ({
        uri: configService.get<string>('MONGO_URI'),
      }),
      inject: [ConfigService],
    }),
    CloudinaryModule,
    BlogModule,
    NewsModule,
    MediaModule,
    ItemModule,
  ],
})
export class AppModule {}

