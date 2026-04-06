import { Controller, Body } from '@nestjs/common';
import { MessagePattern } from '@nestjs/microservices';
import { BlogService } from './blog.service';
import { CreateBlogDto } from './dto/create-blog.dto';
import { UpdateBlogDto } from './dto/update-blog.dto';

@Controller()
export class BlogController {
  constructor(private readonly blogService: BlogService) {}

  @MessagePattern('create-blog')
  async createBlog(@Body() createBlogDto: CreateBlogDto) {
    return this.blogService.create(createBlogDto);
  }

  @MessagePattern('get-all-blogs')
  async getAllBlogs() {
    return this.blogService.findAll();
  }

  @MessagePattern('get-blog-by-id')
  async getBlogById(@Body() id: string) {
    return this.blogService.findById(id);
  }

  @MessagePattern('update-blog')
  async updateBlog(@Body() payload: { id: string; updateBlogDto: UpdateBlogDto }) {
    return this.blogService.update(payload.id, payload.updateBlogDto);
  }

  @MessagePattern('delete-blog')
  async deleteBlog(@Body() id: string) {
    return this.blogService.delete(id);
  }
}
