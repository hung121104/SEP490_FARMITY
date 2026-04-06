import { Injectable } from '@nestjs/common';
import { InjectModel } from '@nestjs/mongoose';
import { Model } from 'mongoose';
import { GameConfig, GameConfigDocument } from './game-config.schema';

const MAIN_MENU_KEY = 'main_menu_background';

@Injectable()
export class GameConfigService {
  constructor(
    @InjectModel(GameConfig.name)
    private configModel: Model<GameConfigDocument>,
  ) {}

  /** Return the current main-menu background config, or null if never set. */
  async getMainMenuConfig(): Promise<{
    currentBackgroundUrl: string;
    version: number;
  } | null> {
    const doc = await this.configModel
      .findOne({ configKey: MAIN_MENU_KEY })
      .exec();
    if (!doc) return null;
    return {
      currentBackgroundUrl: doc.currentBackgroundUrl,
      version: doc.version,
    };
  }

  /** Upsert the main-menu background URL and atomically increment version. */
  async updateMainMenuBackground(
    url: string,
  ): Promise<{ currentBackgroundUrl: string; version: number }> {
    const doc = await this.configModel
      .findOneAndUpdate(
        { configKey: MAIN_MENU_KEY },
        {
          $set: { currentBackgroundUrl: url },
          $inc: { version: 1 },
        },
        { upsert: true, new: true },
      )
      .exec();
    return {
      currentBackgroundUrl: doc.currentBackgroundUrl,
      version: doc.version,
    };
  }
}
