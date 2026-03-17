import { Prop, Schema, SchemaFactory } from '@nestjs/mongoose';
import { Document } from 'mongoose';

export type ItemDocument = Item & Document;

@Schema({ timestamps: true })
export class Item {
  // ── Base Properties (common to ALL items) ──────────────────────────────────

  @Prop({ required: true, unique: true })
  itemID: string;

  @Prop({ required: true })
  itemName: string;

  @Prop({ required: true })
  description: string;

  @Prop({ required: true })
  iconUrl: string;

  /** Discriminator: 0=Tool, 1=Seed, 2=Crop, 3=Pollen, 4=Consumable,
    *  5=Material, 6=Weapon, 7=Fish, 8=Cooking, 9=Forage, 10=Resource,
    *  11=Gift, 12=Quest, 13=Structure, 14=Fertilizer */
  @Prop({ required: true })
  itemType: number;

  @Prop({ required: true })
  itemCategory: number;

  @Prop({ required: true, default: 99 })
  maxStack: number;

  @Prop({ required: true, default: true })
  isStackable: boolean;

  @Prop({ required: true, default: 0 })
  basePrice: number;

  @Prop({ required: true, default: 0 })
  buyPrice: number;

  @Prop({ default: true })
  canBeSold: boolean;

  @Prop({ default: false })
  canBeBought: boolean;

  @Prop({ default: false })
  isQuestItem: boolean;

  @Prop({ default: false })
  isArtifact: boolean;

  @Prop({ default: false })
  isRareItem: boolean;

  /** SkinCatalogManager configId for the item's paper-doll spritesheet
   *  (e.g. "gold_hoe", "copper_watering_can"). Empty/null = no sprite layer. */
  @Prop({ default: '' })
  skinConfigId: string;

  @Prop({ type: [String], default: [] })
  npcPreferenceNames: string[];

  @Prop({ type: [Number], default: [] })
  npcPreferenceReactions: number[];

  // ── itemType: 1 – Seed ─────────────────────────────────────────────────────

  /** Links this seed to its corresponding PlantData in PlantCatalogService */
  @Prop()
  plantId?: string;

  // ── itemType: 0 – Tool ─────────────────────────────────────────────────────

  @Prop()
  toolType?: number;

  @Prop()
  toolLevel?: number;

  @Prop()
  toolPower?: number;

  /** References a Material document by materialId (e.g. 'mat_copper'). */
  @Prop()
  toolMaterialId?: string;

  // ── itemType: 3 – Pollen ───────────────────────────────────────────────────

  @Prop()
  sourcePlantId?: string;

  @Prop()
  pollinationSuccessChance?: number;

  @Prop()
  viabilityDays?: number;

  /** Cross-breeding results: which target plant + pollen produces which result plant. */
  @Prop({
    type: [
      {
        targetPlantId: { type: String, required: true },
        resultPlantId: { type: String, required: true },
      },
    ],
    default: undefined,
  })
  crossResults?: { targetPlantId: string; resultPlantId: string }[];

  // ── itemType: 4 – Consumable / 8 – Cooking ────────────────────────────────

  @Prop()
  energyRestore?: number;

  @Prop()
  healthRestore?: number;

  @Prop()
  bufferDuration?: number;

  // ── itemType: 6 – Weapon ───────────────────────────────────────────────────

  @Prop()
  damage?: number;

  @Prop()
  critChance?: number;

  @Prop()
  attackSpeed?: number;

  /** References a Material document by materialId (e.g. 'mat_steel'). */
  @Prop()
  weaponMaterialId?: string;

  // ── itemType: 7 – Fish ─────────────────────────────────────────────────────

  @Prop()
  difficulty?: number;

  @Prop({ type: [Number] })
  fishingSeasons?: number[];

  @Prop()
  isLegendary?: boolean;

  // ── itemType: 9 – Forage ──────────────────────────────────────────────────

  @Prop({ type: [Number] })
  foragingSeasons?: number[];

  // ── itemType: 10 – Resource ───────────────────────────────────────────────

  @Prop()
  isOre?: boolean;

  @Prop()
  requiresSmelting?: boolean;

  @Prop()
  smeltedResultId?: string;

  // ── itemType: 11 – Gift ───────────────────────────────────────────────────

  @Prop()
  isUniversalLike?: boolean;

  @Prop()
  isUniversalLove?: boolean;

  // ── itemType: 12 – Quest ──────────────────────────────────────────────────

  @Prop()
  relatedQuestID?: string;

  @Prop()
  autoConsume?: boolean;

  // ── itemType: 13 – Structure / 14 – Fertilizer ───────────────────────────
  // No additional persisted fields beyond the shared base item properties.
}

export const ItemSchema = SchemaFactory.createForClass(Item);
