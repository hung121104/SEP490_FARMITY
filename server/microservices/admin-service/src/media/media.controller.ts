import { Controller, Body } from '@nestjs/common';
import { MessagePattern } from '@nestjs/microservices';
import { MediaService } from './media.service';
import { CreateMediaDto } from './dto/create-media.dto';
import { UpdateMediaDto } from './dto/update-media.dto';
import { UploadSignatureDto } from './dto/upload-signature.dto';

@Controller()
export class MediaController {
  constructor(private readonly mediaService: MediaService) {}

  @MessagePattern('create-media')
  async createMedia(@Body() createMediaDto: CreateMediaDto) {
    return this.mediaService.create(createMediaDto);
  }

  @MessagePattern('get-all-media')
  async getAllMedia() {
    return this.mediaService.findAll();
  }

  @MessagePattern('get-media-by-id')
  async getMediaById(@Body() id: string) {
    return this.mediaService.findById(id);
  }

  @MessagePattern('update-media')
  async updateMedia(@Body() payload: { id: string; updateMediaDto: UpdateMediaDto }) {
    return this.mediaService.update(payload.id, payload.updateMediaDto);
  }

  @MessagePattern('delete-media')
  async deleteMedia(@Body() id: string) {
    return this.mediaService.delete(id);
  }

  @MessagePattern('media-upload-signature')
  async uploadSignature(@Body() dto: UploadSignatureDto) {
    return this.mediaService.generateUploadSignature(dto);
  }
}
