import { Module } from '@nestjs/common';
import { MongooseModule } from '@nestjs/mongoose';
import { ResourceConfig, ResourceConfigSchema } from './resource-config.schema';
import { ResourceConfigController } from './resource-config.controller';
import { ResourceConfigService } from './resource-config.service';

@Module({
  imports: [
    MongooseModule.forFeature([
      { name: ResourceConfig.name, schema: ResourceConfigSchema },
    ]),
  ],
  controllers: [ResourceConfigController],
  providers: [ResourceConfigService],
  exports: [ResourceConfigService],
})
export class ResourceConfigModule {}
