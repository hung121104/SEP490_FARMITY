import { Global, Module } from '@nestjs/common';
import { MongooseModule } from '@nestjs/mongoose';
import {
  CatalogVersion,
  CatalogVersionSchema,
} from './catalog-version.schema';
import { CatalogVersionService } from './catalog-version.service';
import { CatalogVersionController } from './catalog-version.controller';

@Global()
@Module({
  imports: [
    MongooseModule.forFeature([
      { name: CatalogVersion.name, schema: CatalogVersionSchema },
    ]),
  ],
  controllers: [CatalogVersionController],
  providers: [CatalogVersionService],
  exports: [CatalogVersionService],
})
export class CatalogVersionModule {}
