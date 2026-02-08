import { Injectable, BadRequestException } from '@nestjs/common';
import { InjectModel } from '@nestjs/mongoose';
import { ConfigService } from '@nestjs/config';
import { Model } from 'mongoose';
import * as crypto from 'crypto';
import { Media, MediaDocument } from './media.schema';
import { CreateMediaDto } from './dto/create-media.dto';
import { UpdateMediaDto } from './dto/update-media.dto';
import { UploadSignatureDto } from './dto/upload-signature.dto';

@Injectable()
export class MediaService {
  constructor(
    @InjectModel(Media.name) private mediaModel: Model<MediaDocument>,
    private configService: ConfigService,
  ) {}

  async create(createMediaDto: CreateMediaDto): Promise<Media> {
    const media = new this.mediaModel({
      ...createMediaDto,
      upload_date: createMediaDto.upload_date || new Date(),
    });
    return media.save();
  }

  async findAll(): Promise<Media[]> {
    return this.mediaModel.find().sort({ upload_date: -1 }).exec();
  }

  async findById(id: string): Promise<Media | null> {
    return this.mediaModel.findById(id).exec();
  }

  async update(id: string, updateMediaDto: UpdateMediaDto): Promise<Media | null> {
    return this.mediaModel.findByIdAndUpdate(id, updateMediaDto, { new: true }).exec();
  }

  async delete(id: string): Promise<Media | null> {
    return this.mediaModel.findByIdAndDelete(id).exec();
  }

  generateUploadSignature(dto: UploadSignatureDto) {
    const cloudName = this.configService.get<string>('CLOUDINARY_CLOUD_NAME');
    const apiKey = this.configService.get<string>('CLOUDINARY_API_KEY');
    const apiSecret = this.configService.get<string>('CLOUDINARY_API_SECRET');
    if (!cloudName || !apiKey || !apiSecret) {
      throw new BadRequestException('Cloudinary configuration is missing');
    }

    const timestamp = Math.floor(Date.now() / 1000);
    const folder = dto.folder || 'media';
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