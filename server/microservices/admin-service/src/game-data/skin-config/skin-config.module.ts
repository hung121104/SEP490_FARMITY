import { Module } from '@nestjs/common';
import { MongooseModule } from '@nestjs/mongoose';
import { SkinConfig, SkinConfigSchema } from './skin-config.schema';
import { SkinConfigService } from './skin-config.service';
import { SkinConfigController } from './skin-config.controller';

@Module({
  imports: [
    MongooseModule.forFeature([
      { name: SkinConfig.name, schema: SkinConfigSchema },
    ]),
  ],
  controllers: [SkinConfigController],
  providers: [SkinConfigService],
  exports: [SkinConfigService],
})
export class SkinConfigModule {}
