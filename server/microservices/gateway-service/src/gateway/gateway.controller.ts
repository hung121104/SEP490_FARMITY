import { Controller, Post, Body, Get, Query, Inject, Headers, UnauthorizedException, Res, Param, Delete } from '@nestjs/common';
import { ClientProxy } from '@nestjs/microservices';
import { CreateAccountDto } from './dto/create-account.dto';
import { SavePositionDto } from './dto/save-position.dto';
import { GetPositionDto } from './dto/get-position.dto';
import { firstValueFrom } from 'rxjs';
import { Response } from 'express';
import { CreateBlogDto } from './dto/create-blog.dto';
import { UpdateBlogDto } from './dto/update-blog.dto';
import { CreateNewsDto } from './dto/create-news.dto';
import { UpdateNewsDto } from './dto/update-news.dto';
import { UploadSignatureDto } from './dto/upload-signature.dto';

@Controller()
export class GatewayController {
  constructor(
    @Inject('AUTH_SERVICE') private authClient: ClientProxy,
    @Inject('PLAYER_DATA_SERVICE') private playerDataClient: ClientProxy,
    @Inject('BLOG_SERVICE') private blogClient: ClientProxy,
    @Inject('NEWS_SERVICE') private newsClient: ClientProxy,
  ) {}

  @Post('auth/register')
  async register(@Body() createAccountDto: CreateAccountDto) {
    return this.authClient.send('register', createAccountDto);
  }

  @Post('auth/login-ingame')
  async login(@Body() loginDto: any) {
    return this.authClient.send('login-ingame', loginDto);
  }

  // Admin registration endpoint for web management
  @Post('auth/register-admin')
  async registerAdmin(@Body() createAdminDto: any) {
    return this.authClient.send('register-admin', createAdminDto);
  }

  // Admin login endpoint for web management
  @Post('auth/login-admin')
  async loginAdmin(@Body() loginDto: any, @Res({ passthrough: true }) res: Response) {
    const result = await firstValueFrom(this.authClient.send('login-admin', loginDto));
    const token = result?.access_token;
    if (!token) throw new UnauthorizedException('Login failed');
    res.cookie('access_token', token, {
      httpOnly: true,
      secure: true,
      sameSite: 'lax',
      maxAge: 60 * 60 * 1000,
    });
    return { userId: result.userId, username: result.username, access_token: token };
  }

  @Post('player-data/save-position')
  async savePosition(@Body() savePositionDto: SavePositionDto) {
    return this.playerDataClient.send('save-position', savePositionDto);
  }

  @Get('player-data/position')
  async getPosition(@Query() getPositionDto: GetPositionDto) {
    return this.playerDataClient.send('get-position', getPositionDto);
  }

  // Admin session check (web management) without affecting game endpoints
  @Get('auth/admin-check')
  async adminCheck(@Headers('authorization') authHeader: string, @Headers('cookie') cookieHeader: string) {
    return this.verifyAdminToken(authHeader, cookieHeader);
  }

  // Admin logout endpoint for web management
  @Post('auth/logout')
  async logout(
    @Headers('authorization') authHeader: string,
    @Headers('cookie') cookieHeader: string,
    @Res({ passthrough: true }) res: Response,
  ) {
    const tokenFromHeader = authHeader?.split(' ')[1];
    const cookies = (cookieHeader || '').split(';').reduce<Record<string, string>>((acc, c) => {
      const [k, v] = c.split('=').map(s => s?.trim());
      if (k && v) acc[k] = decodeURIComponent(v);
      return acc;
    }, {});
    const token = tokenFromHeader ?? cookies['access_token'];
    if (token) {
      await firstValueFrom(this.authClient.send('logout', token));
    }
    res.clearCookie('access_token', { httpOnly: true, secure: true, sameSite: 'lax' });
    return { ok: true };
  }

  @Post('blog/create')
  async createBlog(
    @Body() createBlogDto: CreateBlogDto,
    @Headers('authorization') authHeader: string,
    @Headers('cookie') cookieHeader: string,
  ) {
    await this.verifyAdminToken(authHeader, cookieHeader);
    return this.blogClient.send('create-blog', createBlogDto);
  }

  @Get('blog/all')
  async getAllBlogs() {
    return this.blogClient.send('get-all-blogs', {});
  }

  @Get('blog/:id')
  async getBlogById(@Param('id') id: string) {
    return this.blogClient.send('get-blog-by-id', id);
  }

  @Post('blog/update/:id')
  async updateBlog(
    @Param('id') id: string,
    @Body() updateBlogDto: UpdateBlogDto,
    @Headers('authorization') authHeader: string,
    @Headers('cookie') cookieHeader: string,
  ) {
    await this.verifyAdminToken(authHeader, cookieHeader);
    return this.blogClient.send('update-blog', { id, updateBlogDto });
  }

  @Delete('blog/delete/:id')
  async deleteBlog(
    @Param('id') id: string,
    @Headers('authorization') authHeader: string,
    @Headers('cookie') cookieHeader: string,
  ) {
    await this.verifyAdminToken(authHeader, cookieHeader);
    return this.blogClient.send('delete-blog', id);
  }

  @Post('news/upload-signature')
  async getNewsUploadSignature(
    @Body() dto: UploadSignatureDto,
    @Headers('authorization') authHeader: string,
    @Headers('cookie') cookieHeader: string,
  ) {
    await this.verifyAdminToken(authHeader, cookieHeader);
    return this.newsClient.send('news-upload-signature', dto);
  }

  @Post('news/create')
  async createNews(
    @Body() createNewsDto: CreateNewsDto,
    @Headers('authorization') authHeader: string,
    @Headers('cookie') cookieHeader: string,
  ) {
    await this.verifyAdminToken(authHeader, cookieHeader);
    return this.newsClient.send('create-news', createNewsDto);
  }

  @Get('news/all')
  async getAllNews() {
    return this.newsClient.send('get-all-news', {});
  }

  @Get('news/:id')
  async getNewsById(@Param('id') id: string) {
    return this.newsClient.send('get-news-by-id', id);
  }

  @Post('news/update/:id')
  async updateNews(
    @Param('id') id: string,
    @Body() updateNewsDto: UpdateNewsDto,
    @Headers('authorization') authHeader: string,
    @Headers('cookie') cookieHeader: string,
  ) {
    await this.verifyAdminToken(authHeader, cookieHeader);
    return this.newsClient.send('update-news', { id, updateNewsDto });
  }

  @Delete('news/delete/:id')
  async deleteNews(
    @Param('id') id: string,
    @Headers('authorization') authHeader: string,
    @Headers('cookie') cookieHeader: string,
  ) {
    await this.verifyAdminToken(authHeader, cookieHeader);
    return this.newsClient.send('delete-news', id);
  }

  private async verifyAdminToken(authHeader: string, cookieHeader: string): Promise<any> {
    const tokenFromHeader = authHeader?.split(' ')[1];
    const cookies = (cookieHeader || '').split(';').reduce<Record<string, string>>((acc, c) => {
      const [k, v] = c.split('=').map(s => s?.trim());
      if (k && v) acc[k] = decodeURIComponent(v);
      return acc;
    }, {});
    const token = tokenFromHeader ?? cookies['access_token'];
    if (!token) throw new UnauthorizedException('Missing token');

    const payload = await firstValueFrom(this.authClient.send('verify-token-passive', token));
    if (!payload?.isAdmin) throw new UnauthorizedException('Not an admin');
    
    return payload;
  }
}