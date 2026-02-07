import { Injectable, BadRequestException } from '@nestjs/common';
import { InjectModel } from '@nestjs/mongoose';
import { ConfigService } from '@nestjs/config';
import { Model } from 'mongoose';
import * as crypto from 'crypto';
import { News, NewsDocument } from './news.schema';
import { CreateNewsDto } from './dto/create-news.dto';
import { UpdateNewsDto } from './dto/update-news.dto';
import { UploadSignatureDto } from './dto/upload-signature.dto';

@Injectable()
export class NewsService {
  constructor(
    @InjectModel(News.name) private newsModel: Model<NewsDocument>,
    private configService: ConfigService,
  ) {}

  async create(createNewsDto: CreateNewsDto): Promise<News> {
    const news = new this.newsModel({
      ...createNewsDto,
      publishDate: createNewsDto.publishDate || new Date(),
    });
    return news.save();
  }

  async findAll(): Promise<News[]> {
    return this.newsModel.find().sort({ publishDate: -1 }).exec();
  }

  async findById(id: string): Promise<News | null> {
    return this.newsModel.findById(id).exec();
  }

  async update(id: string, updateNewsDto: UpdateNewsDto): Promise<News | null> {
    return this.newsModel.findByIdAndUpdate(id, updateNewsDto, { new: true }).exec();
  }

  async delete(id: string): Promise<News | null> {
    return this.newsModel.findByIdAndDelete(id).exec();
  }

  generateUploadSignature(dto: UploadSignatureDto) {
    const cloudName = this.configService.get<string>('CLOUDINARY_CLOUD_NAME');
    const apiKey = this.configService.get<string>('CLOUDINARY_API_KEY');
    const apiSecret = this.configService.get<string>('CLOUDINARY_API_SECRET');
    if (!cloudName || !apiKey || !apiSecret) {
      throw new BadRequestException('Cloudinary configuration is missing');
    }

    const timestamp = Math.floor(Date.now() / 1000);
    const folder = dto.folder || 'news';
    const params: Record<string, string | number> = { folder, timestamp };

    if (dto.publicId) {
      params.public_id = dto.publicId;
    }

    const signature = this.buildSignature(params, apiSecret);

    return {
      cloudName,
      apiKey,
      timestamp,
      signature,
      folder,
      publicId: dto.publicId,
    };
  }

  private buildSignature(params: Record<string, string | number>, apiSecret: string) {
    const paramString = Object.keys(params)
      .sort()
      .map((key) => `${key}=${params[key]}`)
      .join('&');

    return crypto.createHash('sha1').update(paramString + apiSecret).digest('hex');
  }
}