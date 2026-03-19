import { Prop, Schema, SchemaFactory } from '@nestjs/mongoose';
import { Document, Types } from 'mongoose';

export type ChunkDocument = Chunk & Document;

/**
 * TileData sub-document stored inside the `tiles` Map.
 * `strict: true` ensures only declared fields are persisted, preventing
 * duplicate/mis-cased keys (e.g. "CropStage" vs "cropStage") in MongoDB.
 */
@Schema({ _id: false, strict: true })
export class TileData {
  /** Tile category: 'empty' | 'tilled' | 'crop' | 'resource' */
  @Prop({
    default: 'empty',
    enum: ['empty', 'tilled', 'crop', 'resource'],
  })
  type: string;

  // ── Crop fields ──────────────────────────────────────────────────────────

  /** Game-side plant ID string, e.g. 'plant_corn' */
  @Prop({ default: null })
  plantId: string | null;

  /** Current growth stage index (0-based) */
  @Prop({ default: 0 })
  cropStage: number;

  /** Accumulated growth time in real-time seconds toward next stage */
  @Prop({ default: 0 })
  growthTimer: number;

  /** How many times pollen has been harvested this stage */
  @Prop({ default: 0 })
  pollenHarvestCount: number;

  @Prop({ default: false })
  isWatered: boolean;

  @Prop({ default: false })
  isFertilized: boolean;

  @Prop({ default: false })
  isPollinated: boolean;

  // ── Resource fields (trees, rocks, etc.) ─────────────────────────────────

  /** Catalog ID of the spawned resource, e.g. 'oak_tree', 'iron_rock' */
  @Prop({ default: null })
  resourceId: string | null;

  /** Remaining hit-points; when 0 the resource is destroyed */
  @Prop({ default: 0 })
  currentHp: number;
}

export const TileDataSchema = SchemaFactory.createForClass(TileData);

// ────────────────────────────────────────────────────────────────────────────
//  Chunk document
//  One document = one 30×30 chunk, identified by (worldId, sectionId, chunkX, chunkY).
//  `tiles` is a MongoDB Map: key = local tile index "0"–"899" (string),
//  value = TileData sub-document.
//  Using a Map (instead of an array) lets us use targeted $set operators
//  like  { $set: { "tiles.42": { ... } } }  without touching other tiles.
// ────────────────────────────────────────────────────────────────────────────
@Schema({ collection: 'chunks' })
export class Chunk {
  @Prop({ type: Types.ObjectId, ref: 'World', required: true, index: true })
  worldId: Types.ObjectId;

  @Prop({ required: true })
  sectionId: number;

  @Prop({ required: true })
  chunkX: number;

  @Prop({ required: true })
  chunkY: number;

  /**
   * Map<tileIndex, TileData>.
   * tileIndex = localX + localY * 30  (0 … 899)
   * Stored as a plain BSON object; Mongoose Map type serialises correctly.
   */
  @Prop({ type: Map, of: TileDataSchema, default: () => new Map() })
  tiles: Map<string, TileData>;
}

export const ChunkSchema = SchemaFactory.createForClass(Chunk);

// Compound unique index: one chunk per (world, section, x, y)
ChunkSchema.index({ worldId: 1, sectionId: 1, chunkX: 1, chunkY: 1 }, { unique: true });
