import { Prop, Schema, SchemaFactory } from '@nestjs/mongoose';
import { Document } from 'mongoose';

export type PlantDocument = Plant & Document;

// ── Embedded sub-document ─────────────────────────────────────────────────────

export class PlantGrowthStage {
  /** Stage index (0-based). */
  stageNum: number;
  /** Total in-game days required to reach this stage. */
  age: number;
  /** CDN URL of the sprite for this stage. */
  stageIconUrl: string;
}

// ── Root document ─────────────────────────────────────────────────────────────

@Schema({ timestamps: true })
export class Plant {
  // ── Identity ─────────────────────────────────────────────────────────────

  /** Unique game-side identifier, e.g. "plant_corn". */
  @Prop({ required: true, unique: true })
  plantId: string;

  @Prop({ required: true })
  plantName: string;

  // ── Growth Stages ─────────────────────────────────────────────────────────

  /**
   * Ordered list of growth stage entries.
   * Each entry: { stageNum, age, stageIconUrl }
   */
  @Prop({
    type: [
      {
        stageNum: { type: Number, required: true },
        age: { type: Number, required: true },
        stageIconUrl: { type: String, required: true },
      },
    ],
    default: [],
  })
  growthStages: PlantGrowthStage[];

  // ── Harvest Info ──────────────────────────────────────────────────────────

  /** itemID (from ItemCatalog) of the item dropped when this plant is harvested. */
  @Prop({ required: true })
  harvestedItemId: string;

  // ── Pollen / Crossbreeding ────────────────────────────────────────────────

  @Prop({ default: false })
  canProducePollen: boolean;

  /** Growth stage index at which pollen can be collected. */
  @Prop({ default: 3 })
  pollenStage: number;

  /** itemID of the pollen item given when pollen is collected. */
  @Prop()
  pollenItemId?: string;

  /** 0 = unlimited. */
  @Prop({ default: 1 })
  maxPollenHarvestsPerStage: number;

  // ── Season ────────────────────────────────────────────────────────────────

  /** 0 = Sunny, 1 = Rainy */
  @Prop({ required: true, default: 0 })
  growingSeason: number;

  // ── Hybrid flags ──────────────────────────────────────────────────────────

  /** True for hybrid plants produced by cross-breeding. */
  @Prop({ default: false })
  isHybrid: boolean;

  /** plantId of the plant that received pollen (hybrid only). */
  @Prop()
  receiverPlantId?: string;

  /** plantId of the plant whose pollen was applied (hybrid only). */
  @Prop()
  pollenPlantId?: string;

  /** CDN URL for the sprite at pollenStage (hybrid only). */
  @Prop()
  hybridFlowerIconUrl?: string;

  /** CDN URL for the sprite at pollenStage+1 (hybrid only). */
  @Prop()
  hybridMatureIconUrl?: string;

  /** When false, harvest never generates seeds (hybrid only). */
  @Prop({ default: false })
  dropSeeds: boolean;
}

export const PlantSchema = SchemaFactory.createForClass(Plant);
