import { Controller, Body } from '@nestjs/common';
import { MessagePattern } from '@nestjs/microservices';
import { NewsService } from './news.service';
import { CreateNewsDto } from './dto/create-news.dto';
import { UpdateNewsDto } from './dto/update-news.dto';
import { UploadSignatureDto } from './dto/upload-signature.dto';

@Controller()
export class NewsController {
  constructor(private readonly newsService: NewsService) {}

  @MessagePattern('create-news')
  async createNews(@Body() createNewsDto: CreateNewsDto) {
    return this.newsService.create(createNewsDto);
  }

  @MessagePattern('get-all-news')
  async getAllNews() {
    return this.newsService.findAll();
  }

  @MessagePattern('get-news-by-id')
  async getNewsById(@Body() id: string) {
    return this.newsService.findById(id);
  }

  @MessagePattern('update-news')
  async updateNews(@Body() payload: { id: string; updateNewsDto: UpdateNewsDto }) {
    return this.newsService.update(payload.id, payload.updateNewsDto);
  }

  @MessagePattern('delete-news')
  async deleteNews(@Body() id: string) {
    return this.newsService.delete(id);
  }

  @MessagePattern('news-upload-signature')
  async uploadSignature(@Body() dto: UploadSignatureDto) {
    return this.newsService.generateUploadSignature(dto);
  }
}
