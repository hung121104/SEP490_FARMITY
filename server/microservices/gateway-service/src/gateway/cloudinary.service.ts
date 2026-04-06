import { Injectable, InternalServerErrorException } from '@nestjs/common';
import { v2 as cloudinary } from 'cloudinary';

@Injectable()
export class GatewayCloudinaryService {
  constructor() {
    // Configure here (inside constructor) so it runs after dotenv has loaded
    // in main.ts — not at module-import time which is too early.
    cloudinary.config({
      cloud_name: process.env.CLOUDINARY_CLOUD_NAME,
      api_key: process.env.CLOUDINARY_API_KEY,
      api_secret: process.env.CLOUDINARY_API_SECRET,
    });
  }

  /** Upload a Multer file buffer to Cloudinary and return the secure_url.
   *  @param publicId  Optional. When provided, Cloudinary preserves this as the asset name
   *                   (without extension), e.g. "cabbage_0". If omitted, Cloudinary auto-generates an ID.
   */
  async uploadFile(
    file: Express.Multer.File,
    folder: string = 'item-icons',
    publicId?: string,
  ): Promise<string> {
    return new Promise((resolve, reject) => {
      cloudinary.uploader
        .upload_stream({ folder, ...(publicId ? { public_id: publicId } : {}) }, (error, result) => {
          if (error || !result) {
            return reject(
              new InternalServerErrorException(
                `Cloudinary upload failed: ${error?.message ?? 'unknown'}`,
              ),
            );
          }
          resolve(result.secure_url);
        })
        .end(file.buffer);
    });
  }
}
