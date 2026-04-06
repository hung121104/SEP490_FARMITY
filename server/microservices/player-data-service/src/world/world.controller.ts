import { Controller, Body } from '@nestjs/common';
import { MessagePattern } from '@nestjs/microservices';
import { WorldService } from './world.service';
import { CreateWorldDto } from './dto/create-world.dto';
import { GetWorldDto } from './dto/get-world.dto';
import { GetWorldsByOwnerDto } from './dto/get-worlds-by-owner.dto';
import { UpdateWorldDto } from './dto/update-world.dto';
import {
  AddWorldBlacklistDto,
  GetWorldBlacklistDto,
  RemoveWorldBlacklistDto,
} from './dto/world-blacklist.dto';

@Controller()
export class WorldController {
  constructor(private readonly worldService: WorldService) {}

  @MessagePattern('create-world')
  async createWorld(@Body() createWorldDto: CreateWorldDto) {
    return this.worldService.createWorld(createWorldDto);
  }

  @MessagePattern('get-world')
  async getWorld(@Body() getWorldDto: GetWorldDto) {
    return this.worldService.getWorld(getWorldDto);
  }

  @MessagePattern('get-worlds-by-owner')
  async getWorldsByOwner(@Body() dto: GetWorldsByOwnerDto) {
    return this.worldService.getWorldsByOwner(dto);
  }

  @MessagePattern('update-world')
  async updateWorld(@Body() dto: UpdateWorldDto) {
    return this.worldService.updateWorld(dto);
  }

  /**
   * save-world — unified auto-save / quit-flush.
   * Accepts the same DTO as update-world but also processes tile deltas
   * inside a MongoDB session/transaction.
   */
  @MessagePattern('save-world')
  async saveWorld(@Body() dto: UpdateWorldDto) {
    return this.worldService.saveWorld(dto);
  }

  @MessagePattern('delete-world')
  async deleteWorld(@Body() getWorldDto: GetWorldDto) {
    return this.worldService.deleteWorld(getWorldDto);
  }

  @MessagePattern('get-world-blacklist')
  async getWorldBlacklist(@Body() dto: GetWorldBlacklistDto) {
    return this.worldService.getWorldBlacklist(dto);
  }

  @MessagePattern('add-world-blacklist-player')
  async addWorldBlacklistPlayer(@Body() dto: AddWorldBlacklistDto) {
    return this.worldService.addWorldBlacklistPlayer(dto);
  }

  @MessagePattern('remove-world-blacklist-player')
  async removeWorldBlacklistPlayer(@Body() dto: RemoveWorldBlacklistDto) {
    return this.worldService.removeWorldBlacklistPlayer(dto);
  }
}
