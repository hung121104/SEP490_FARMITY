import { Controller } from '@nestjs/common';
import { MessagePattern, Payload } from '@nestjs/microservices';
import { QuestService } from './quest.service';
import { CreateQuestDto } from './dto/create-quest.dto';
import { UpdateQuestDto } from './dto/update-quest.dto';
import { CatalogVersionService } from '../../catalog-version/catalog-version.service';
import { CatalogChange } from '../../catalog-version/catalog-change.types';

@Controller()
export class QuestController {
  constructor(
    private readonly questService: QuestService,
    private readonly catalogVersionService: CatalogVersionService,
  ) {}

  /** Create a new quest definition */
  @MessagePattern('create-quest')
  async createQuest(
    @Payload() createQuestDto: CreateQuestDto,
  ): Promise<CatalogChange> {
    const data = await this.questService.create(createQuestDto);
    const catalogVersion = await this.catalogVersionService.increment();
    return {
      data,
      catalogVersion,
      changeType: 'create',
      entityType: 'quest',
    };
  }

  /** Return full catalog: { quests: [...] } – consumed by Unity client */
  @MessagePattern('get-quest-catalog')
  async getQuestCatalog() {
    return this.questService.getCatalog();
  }

  /** Return flat array of all quests */
  @MessagePattern('get-all-quests')
  async getAllQuests() {
    return this.questService.findAll();
  }

  /** Find one quest by MongoDB _id */
  @MessagePattern('get-quest-by-id')
  async getQuestById(@Payload() id: string) {
    return this.questService.findById(id);
  }

  /** Find one quest by the game-side questId string */
  @MessagePattern('get-quest-by-quest-id')
  async getQuestByQuestId(@Payload() questId: string) {
    return this.questService.findByQuestId(questId);
  }

  /** Update a quest by game-side questId string */
  @MessagePattern('update-quest')
  async updateQuest(
    @Payload() payload: { questId: string; dto: UpdateQuestDto },
  ): Promise<CatalogChange> {
    const data = await this.questService.update(
      payload.questId,
      payload.dto,
    );
    const catalogVersion = await this.catalogVersionService.increment();
    return {
      data,
      catalogVersion,
      changeType: 'update',
      entityType: 'quest',
    };
  }

  /** Delete a quest by game-side questId string */
  @MessagePattern('delete-quest')
  async deleteQuest(@Payload() questId: string): Promise<CatalogChange> {
    const data = await this.questService.delete(questId);
    const catalogVersion = await this.catalogVersionService.increment();
    return {
      data,
      catalogVersion,
      changeType: 'delete',
      entityType: 'quest',
    };
  }
}
