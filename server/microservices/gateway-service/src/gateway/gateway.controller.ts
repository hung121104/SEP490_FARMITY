import { Controller, Post, Body, Get, Query, Inject, Headers, UnauthorizedException, Res, Param, Delete, Req } from '@nestjs/common';
import { ClientProxy } from '@nestjs/microservices';
import { CreateAccountDto } from './dto/create-account.dto';
import { SavePositionDto } from './dto/save-position.dto';
import { GetPositionDto } from './dto/get-position.dto';
import { firstValueFrom } from 'rxjs';
import { Response, Request } from 'express';
import { CreateBlogDto } from './dto/create-blog.dto';
import { UpdateBlogDto } from './dto/update-blog.dto';
import { CreateNewsDto } from './dto/create-news.dto';
import { UpdateNewsDto } from './dto/update-news.dto';
import { CreateMediaDto } from './dto/create-media.dto';
import { UpdateMediaDto } from './dto/update-media.dto';
import { UploadSignatureDto } from './dto/upload-signature.dto';
import { RequestAdminResetDto } from './dto/request-admin-reset.dto';
import { ConfirmAdminResetDto } from './dto/confirm-admin-reset.dto';

@Controller()
export class GatewayController {
  constructor(
    @Inject('AUTH_SERVICE') private authClient: ClientProxy,
    @Inject('PLAYER_DATA_SERVICE') private playerDataClient: ClientProxy,
    @Inject('ADMIN_SERVICE') private adminClient: ClientProxy,
  ) {}

  @Post('player-data/world')
  async createWorld(@Body() body: any, @Req() req: Request) {
    const ownerId = req['user']?.sub;
    if (!ownerId) throw new UnauthorizedException('Missing owner');
    // forward optional _id for update, otherwise create
    return firstValueFrom(
      this.playerDataClient.send('create-world', { _id: body._id, worldName: body.worldName, ownerId }),
    );
  }

  @Get('player-data/world')
  async getWorld(@Query('_id') _id: string) {
    return firstValueFrom(this.playerDataClient.send('get-world', { _id }));
  }

  @Get('player-data/worlds')
  async getWorldsByOwner(@Req() req: Request, @Query('ownerId') ownerIdQuery?: string) {
    const user = req['user'];
    let ownerId = user?.sub;
    if (ownerIdQuery && user?.isAdmin) ownerId = ownerIdQuery;
    return firstValueFrom(this.playerDataClient.send('get-worlds-by-owner', { ownerId }));
  }

  @Post('auth/register')
  async register(@Body() createAccountDto: CreateAccountDto) {
    return this.authClient.send('register', createAccountDto);
  }

  @Post('auth/login-ingame')
  async login(@Body() loginDto: any) {
    return this.authClient.send('login-ingame', loginDto);
  }

  @Post('auth/register-admin')
  async registerAdmin(@Body() createAdminDto: any) {
    return this.authClient.send('register-admin', createAdminDto);
  }

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

  @Get('auth/admin-check')
  async adminCheck(@Headers('authorization') authHeader: string, @Headers('cookie') cookieHeader: string) {
    return this.verifyAdminToken(authHeader, cookieHeader);
  }

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
    return this.adminClient.send('create-blog', createBlogDto);
  }

  @Get('blog/all')
  async getAllBlogs() {
    return this.adminClient.send('get-all-blogs', {});
  }

  @Get('blog/:id')
  async getBlogById(@Param('id') id: string) {
    return this.adminClient.send('get-blog-by-id', id);
  }

  @Post('blog/update/:id')
  async updateBlog(
    @Param('id') id: string,
    @Body() updateBlogDto: UpdateBlogDto,
    @Headers('authorization') authHeader: string,
    @Headers('cookie') cookieHeader: string,
  ) {
    await this.verifyAdminToken(authHeader, cookieHeader);
    return this.adminClient.send('update-blog', { id, updateBlogDto });
  }

  @Delete('blog/delete/:id')
  async deleteBlog(
    @Param('id') id: string,
    @Headers('authorization') authHeader: string,
    @Headers('cookie') cookieHeader: string,
  ) {
    await this.verifyAdminToken(authHeader, cookieHeader);
    return this.adminClient.send('delete-blog', id);
  }

  @Post('news/upload-signature')
  async getNewsUploadSignature(
    @Body() dto: UploadSignatureDto,
    @Headers('authorization') authHeader: string,
    @Headers('cookie') cookieHeader: string,
  ) {
    await this.verifyAdminToken(authHeader, cookieHeader);
    return this.adminClient.send('news-upload-signature', dto);
  }

  @Post('news/create')
  async createNews(
    @Body() createNewsDto: CreateNewsDto,
    @Headers('authorization') authHeader: string,
    @Headers('cookie') cookieHeader: string,
  ) {
    await this.verifyAdminToken(authHeader, cookieHeader);
    return this.adminClient.send('create-news', createNewsDto);
  }

  @Get('news/all')
  async getAllNews() {
    return this.adminClient.send('get-all-news', {});
  }

  @Get('news/:id')
  async getNewsById(@Param('id') id: string) {
    return this.adminClient.send('get-news-by-id', id);
  }

  @Post('news/update/:id')
  async updateNews(
    @Param('id') id: string,
    @Body() updateNewsDto: UpdateNewsDto,
    @Headers('authorization') authHeader: string,
    @Headers('cookie') cookieHeader: string,
  ) {
    await this.verifyAdminToken(authHeader, cookieHeader);
    return this.adminClient.send('update-news', { id, updateNewsDto });
  }

  @Delete('news/delete/:id')
  async deleteNews(
    @Param('id') id: string,
    @Headers('authorization') authHeader: string,
    @Headers('cookie') cookieHeader: string,
  ) {
    await this.verifyAdminToken(authHeader, cookieHeader);
    return this.adminClient.send('delete-news', id);
  }

  @Post('media/upload-signature')
  async getMediaUploadSignature(
    @Body() dto: UploadSignatureDto,
    @Headers('authorization') authHeader: string,
    @Headers('cookie') cookieHeader: string,
  ) {
    await this.verifyAdminToken(authHeader, cookieHeader);
    return this.adminClient.send('media-upload-signature', dto);
  }

  @Post('media/create')
  async createMedia(
    @Body() createMediaDto: CreateMediaDto,
    @Headers('authorization') authHeader: string,
    @Headers('cookie') cookieHeader: string,
  ) {
    await this.verifyAdminToken(authHeader, cookieHeader);
    return this.adminClient.send('create-media', createMediaDto);
  }

  @Get('media/all')
  async getAllMedia() {
    return this.adminClient.send('get-all-media', {});
  }

  @Get('media/:id')
  async getMediaById(@Param('id') id: string) {
    return this.adminClient.send('get-media-by-id', id);
  }

  @Post('media/update/:id')
  async updateMedia(
    @Param('id') id: string,
    @Body() updateMediaDto: UpdateMediaDto,
    @Headers('authorization') authHeader: string,
    @Headers('cookie') cookieHeader: string,
  ) {
    await this.verifyAdminToken(authHeader, cookieHeader);
    return this.adminClient.send('update-media', { id, updateMediaDto });
  }

  @Delete('media/delete/:id')
  async deleteMedia(
    @Param('id') id: string,
    @Headers('authorization') authHeader: string,
    @Headers('cookie') cookieHeader: string,
  ) {
    await this.verifyAdminToken(authHeader, cookieHeader);
    return this.adminClient.send('delete-media', id);
  }

  @Post('auth/admin-reset/request')
  async adminResetRequest(@Body() dto: RequestAdminResetDto) {
    return this.authClient.send('admin-reset-request', dto);
  }

  @Post('auth/admin-reset/confirm')
  async adminResetConfirm(@Body() dto: ConfirmAdminResetDto) {
    return this.authClient.send('admin-reset-confirm', dto);
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