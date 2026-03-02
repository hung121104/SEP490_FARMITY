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
   *  11=Gift, 12=Quest */
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

  @Prop({ type: [String], default: [] })
  npcPreferenceNames: string[];

  @Prop({ type: [Number], default: [] })
  npcPreferenceReactions: number[];

  // ── itemType: 0 – Tool ─────────────────────────────────────────────────────

  @Prop()
  toolType?: number;

  @Prop()
  toolLevel?: number;

  @Prop()
  toolPower?: number;

  @Prop()
  toolMaterial?: number;

  // ── itemType: 3 – Pollen ───────────────────────────────────────────────────

  @Prop()
  pollinationSuccessChance?: number;

  @Prop()
  viabilityDays?: number;

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

  @Prop()
  weaponMaterial?: number;

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
}

export const ItemSchema = SchemaFactory.createForClass(Item);
