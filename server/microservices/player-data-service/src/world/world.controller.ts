import { Controller, Body } from '@nestjs/common';
import { MessagePattern } from '@nestjs/microservices';
import { WorldService } from './world.service';
import { CreateWorldDto } from './dto/create-world.dto';
import { GetWorldDto } from './dto/get-world.dto';
import { GetWorldsByOwnerDto } from './dto/get-worlds-by-owner.dto';
import { GetCharacterInWorldDto } from './dto/get-character-in-world.dto';

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

  @MessagePattern('delete-world')
  async deleteWorld(@Body() getWorldDto: GetWorldDto) {
    return this.worldService.deleteWorld(getWorldDto);
  }

  @MessagePattern('get-character-in-world')
  async getCharacterInWorld(@Body() dto: GetCharacterInWorldDto) {
    return this.worldService.getCharacterInWorld(dto);
  }
}
