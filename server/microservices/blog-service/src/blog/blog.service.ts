import { Injectable } from '@nestjs/common';
import { InjectModel } from '@nestjs/mongoose';
import { Model } from 'mongoose';
import { Blog, BlogDocument } from './blog.schema';
import { CreateBlogDto } from './dto/create-blog.dto';
import { UpdateBlogDto } from './dto/update-blog.dto';

@Injectable()
export class BlogService {
  constructor(
    @InjectModel(Blog.name) private blogModel: Model<BlogDocument>,
  ) {}

  async create(createBlogDto: CreateBlogDto): Promise<Blog> {
    const blog = new this.blogModel({
      ...createBlogDto,
      publishDate: createBlogDto.publishDate || new Date(),
    });
    return blog.save();
  }

  async findAll(): Promise<Blog[]> {
    return this.blogModel.find().sort({ publishDate: -1 }).exec();
  }

  async findById(id: string): Promise<Blog | null> {
    return this.blogModel.findById(id).exec();
  }

  async update(id: string, updateBlogDto: UpdateBlogDto): Promise<Blog | null> {
    return this.blogModel.findByIdAndUpdate(id, updateBlogDto, { new: true }).exec();
  }

  async delete(id: string): Promise<Blog | null> {
    return this.blogModel.findByIdAndDelete(id).exec();
  }
}