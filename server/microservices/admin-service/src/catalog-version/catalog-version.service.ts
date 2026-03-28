import { Injectable } from '@nestjs/common';
import { InjectModel } from '@nestjs/mongoose';
import { Model } from 'mongoose';
import {
  CatalogVersion,
  CatalogVersionDocument,
} from './catalog-version.schema';

const CATALOG_KEY = 'catalog';

@Injectable()
export class CatalogVersionService {
  constructor(
    @InjectModel(CatalogVersion.name)
    private catalogVersionModel: Model<CatalogVersionDocument>,
  ) {}

  /** Atomically increment the catalog version and return the new value. */
  async increment(): Promise<number> {
    const doc = await this.catalogVersionModel
      .findOneAndUpdate(
        { configKey: CATALOG_KEY },
        { $inc: { version: 1 } },
        { upsert: true, new: true },
      )
      .exec();
    return doc.version;
  }

  /** Return the current catalog version (0 if never set). */
  async getVersion(): Promise<number> {
    const doc = await this.catalogVersionModel
      .findOne({ configKey: CATALOG_KEY })
      .exec();
    return doc?.version ?? 0;
  }
}
