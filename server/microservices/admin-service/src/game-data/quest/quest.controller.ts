import { Controller } from '@nestjs/common';
import { MessagePattern, Payload } from '@nestjs/microservices';
import { QuestService } from './quest.service';
import { CreateQuestDto } from './dto/create-quest.dto';
import { UpdateQuestDto } from './dto/update-quest.dto';

@Controller()
export class QuestController {
  constructor(private readonly questService: QuestService) {}

  /** Create a new quest definition */
  @MessagePattern('create-quest')
  async createQuest(@Payload() createQuestDto: CreateQuestDto) {
    return this.questService.create(createQuestDto);
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
  async updateQuest(@Payload() payload: { questId: string; dto: UpdateQuestDto }) {
    return this.questService.update(payload.questId, payload.dto);
  }

  /** Delete a quest by game-side questId string */
  @MessagePattern('delete-quest')
  async deleteQuest(@Payload() questId: string) {
    return this.questService.delete(questId);
  }
}
