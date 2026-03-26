import { Module } from '@nestjs/common';
import { MongooseModule } from '@nestjs/mongoose';
import { CombatCatalog, CombatCatalogSchema } from './combat-catalog.schema';
import { CombatCatalogService } from './combat-catalog.service';
import { CombatCatalogController } from './combat-catalog.controller';

@Module({
  imports: [
    MongooseModule.forFeature([
      { name: CombatCatalog.name, schema: CombatCatalogSchema },
    ]),
  ],
  controllers: [CombatCatalogController],
  providers: [CombatCatalogService],
  exports: [CombatCatalogService],
})
export class CombatCatalogModule {}
