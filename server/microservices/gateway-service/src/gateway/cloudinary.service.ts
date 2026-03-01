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

  /** Upload a Multer file buffer to Cloudinary and return the secure_url */
  async uploadFile(
    file: Express.Multer.File,
    folder: string = 'item-icons',
  ): Promise<string> {
    return new Promise((resolve, reject) => {
      cloudinary.uploader
        .upload_stream({ folder }, (error, result) => {
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
