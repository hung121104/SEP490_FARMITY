import { Module } from '@nestjs/common';
import { MongooseModule } from '@nestjs/mongoose';
import { ConfigModule, ConfigService } from '@nestjs/config';
import { BlogModule } from './blog/blog.module';
import { NewsModule } from './news/news.module';
import { MediaModule } from './media/media.module';
import { CloudinaryModule } from './shared/cloudinary/cloudinary.module';
import { ItemModule } from './game-data/item/item.module';
import { PlantModule } from './game-data/plant/plant.module';
import { CraftingRecipeModule } from './game-data/crafting-recipe/crafting-recipe.module';
import { GameConfigModule } from './game-config/game-config.module';
import { AchievementModule } from './game-data/achievement/achievement.module';
import { SkinConfigModule } from './game-data/skin-config/skin-config.module';
import { MaterialModule } from './game-data/material/material.module';

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
    PlantModule,
    CraftingRecipeModule,
    GameConfigModule,
    AchievementModule,
    SkinConfigModule,
    MaterialModule,
  ],
})
export class AppModule {}

