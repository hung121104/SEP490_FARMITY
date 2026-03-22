import {
  Controller,
  Post,
  Body,
  Get,
  Query,
  Inject,
  Headers,
  UnauthorizedException,
  Res,
  Param,
  Delete,
  Req,
  HttpException,
  Put,
  UseInterceptors,
  UploadedFile,
  UploadedFiles,
  BadRequestException,
  ParseArrayPipe,
} from '@nestjs/common';
import { ClientProxy } from '@nestjs/microservices';
import { FileInterceptor, FileFieldsInterceptor, AnyFilesInterceptor } from '@nestjs/platform-express';
import { CreateAccountDto } from './dto/create-account.dto';
import { firstValueFrom } from 'rxjs';
import { Response, Request } from 'express';
import { CreateBlogDto } from './dto/create-blog.dto';
import { UpdateBlogDto } from './dto/update-blog.dto';
import { CreateNewsDto } from './dto/create-news.dto';
import { UpdateNewsDto } from './dto/update-news.dto';
import { CreateMediaDto } from './dto/create-media.dto';
import { UpdateMediaDto } from './dto/update-media.dto';
import { UploadSignatureDto } from './dto/upload-signature.dto';
import { RequestResetDto } from './dto/request-admin-reset.dto';
import { ConfirmResetDto } from './dto/confirm-admin-reset.dto';
import { UpdateWorldDto } from './dto/update-world.dto';
import { CreateItemDto } from './dto/create-item.dto';
import { CreatePlantDto } from './dto/create-plant.dto';
import { CreateCraftingRecipeDto } from './dto/create-crafting-recipe.dto';
import { UpdateCraftingRecipeDto } from './dto/update-crafting-recipe.dto';
import { VerifyRegistrationDto } from './dto/verify-registration.dto';
import { UpdateItemDto } from './dto/update-item.dto';
import { UpdatePlantDto } from './dto/update-plant.dto';
import { GatewayCloudinaryService } from './cloudinary.service';
import { HttpStatus } from '@nestjs/common';
import { CreateAchievementDto } from './dto/create-achievement.dto';
import { UpdateAchievementProgressDto } from './dto/update-achievement-progress.dto';
import { UpdateSkillLoadoutDto } from './dto/update-skill-loadout.dto';
import {
  UpdateWorldBlacklistDto,
  WorldBlacklistQueryDto,
} from './dto/world-blacklist.dto';
import { CreateQuestDto } from './dto/create-quest.dto';
import { UpdateQuestDto } from './dto/update-quest.dto';

const FERTILIZER_ITEM_TYPE = 14;

@Controller()
export class GatewayController {
  constructor(
    @Inject('AUTH_SERVICE') private authClient: ClientProxy,
    @Inject('PLAYER_DATA_SERVICE') private playerDataClient: ClientProxy,
    @Inject('ADMIN_SERVICE') private adminClient: ClientProxy,
    private readonly cloudinaryService: GatewayCloudinaryService,
  ) {}

  /** Extract a well-formed HttpException from an RPC error payload */
  private rpcError(err: any): HttpException {
    const payload = err?.message ?? err;
    let status = 500;
    let message = 'Internal server error';
    if (typeof payload === 'string') {
      try {
        const parsed = JSON.parse(payload);
        status = parsed.status || status;
        message = parsed.message || parsed.error || payload;
      } catch {
        message = payload;
      }
    } else if (payload && typeof payload === 'object') {
      status = payload.status || payload.statusCode || payload.code || status;
      message = payload.message || payload.error || JSON.stringify(payload);
    }
    return new HttpException(message, status);
  }

  private async enrichBlacklistResponse(payload: any) {
    const blacklistedPlayerIds: string[] = Array.isArray(payload?.blacklistedPlayerIds)
      ? payload.blacklistedPlayerIds.map((id: unknown) => String(id))
      : [];

    if (blacklistedPlayerIds.length === 0) {
      return {
        ...payload,
        blacklistedPlayers: [],
      };
    }

    const uniqueIds = Array.from(new Set(blacklistedPlayerIds));
    const lookup = await Promise.all(
      uniqueIds.map(async (accountId) => {
        try {
          const account: any = await firstValueFrom(this.authClient.send('find-account', accountId));
          return { accountId, username: account?.username ?? null };
        } catch {
          return { accountId, username: null };
        }
      }),
    );

    const usernameMap = new Map(lookup.map((item) => [item.accountId, item.username]));

    return {
      ...payload,
      blacklistedPlayers: blacklistedPlayerIds.map((accountId) => ({
        accountId,
        username: usernameMap.get(accountId) ?? null,
      })),
    };
  }

  private parseCrossResults(crossResults: any): any {
    if (crossResults === undefined) return undefined;

    try {
      return typeof crossResults === 'string'
        ? JSON.parse(crossResults)
        : crossResults;
    } catch {
      throw new BadRequestException(
        'crossResults must be a valid JSON array, e.g. [{"targetPlantId":"plant_corn","resultPlantId":"plant_hybrid_corn"}]',
      );
    }
  }

  private buildCreateItemDto(
    body: any,
    iconUrl: string,
    forcedItemType?: number,
    structureInteractionSpriteUrl?: string,
  ): CreateItemDto {
    const dto: CreateItemDto = {
      ...body,
      iconUrl,
      itemType: forcedItemType ?? Number(body.itemType),
      itemCategory: Number(body.itemCategory),
      maxStack: Number(body.maxStack),
      basePrice: Number(body.basePrice ?? 0),
      buyPrice: Number(body.buyPrice ?? 0),
      isStackable: body.isStackable === 'true' || body.isStackable === true,
      canBeSold: body.canBeSold !== 'false' && body.canBeSold !== false,
      canBeBought: body.canBeBought === 'true' || body.canBeBought === true,
      isQuestItem: body.isQuestItem === 'true' || body.isQuestItem === true,
      isArtifact: body.isArtifact === 'true' || body.isArtifact === true,
      isRareItem: body.isRareItem === 'true' || body.isRareItem === true,
    };

    if (structureInteractionSpriteUrl) dto.structureInteractionSpriteUrl = structureInteractionSpriteUrl;

    const crossResults = this.parseCrossResults(body.crossResults);
    if (crossResults !== undefined) dto.crossResults = crossResults;

    return dto;
  }

  private buildUpdateItemDto(
    body: any,
    iconUrl?: string,
    forcedItemType?: number,
    structureInteractionSpriteUrl?: string,
  ): UpdateItemDto {
    const dto: UpdateItemDto = { ...body };

    if (iconUrl) dto.iconUrl = iconUrl;
    if (structureInteractionSpriteUrl) dto.structureInteractionSpriteUrl = structureInteractionSpriteUrl;
    if (forcedItemType !== undefined) dto.itemType = forcedItemType;
    else if (body.itemType !== undefined) dto.itemType = Number(body.itemType);

    if (body.itemCategory !== undefined)
      dto.itemCategory = Number(body.itemCategory);
    if (body.maxStack !== undefined) dto.maxStack = Number(body.maxStack);
    if (body.basePrice !== undefined) dto.basePrice = Number(body.basePrice);
    if (body.buyPrice !== undefined) dto.buyPrice = Number(body.buyPrice);
    if (body.isStackable !== undefined)
      dto.isStackable =
        body.isStackable === 'true' || body.isStackable === true;
    if (body.canBeSold !== undefined)
      dto.canBeSold = body.canBeSold !== 'false' && body.canBeSold !== false;
    if (body.canBeBought !== undefined)
      dto.canBeBought =
        body.canBeBought === 'true' || body.canBeBought === true;
    if (body.isQuestItem !== undefined)
      dto.isQuestItem =
        body.isQuestItem === 'true' || body.isQuestItem === true;
    if (body.isArtifact !== undefined)
      dto.isArtifact = body.isArtifact === 'true' || body.isArtifact === true;
    if (body.isRareItem !== undefined)
      dto.isRareItem = body.isRareItem === 'true' || body.isRareItem === true;

    const crossResults = this.parseCrossResults(body.crossResults);
    if (crossResults !== undefined) dto.crossResults = crossResults;

    return dto;
  }

  private parseNumericField(value: any): number | undefined {
    if (value === undefined || value === null || value === '') return undefined;
    const parsed = Number(value);
    if (Number.isNaN(parsed)) {
      throw new BadRequestException(`Invalid numeric value: ${value}`);
    }
    return parsed;
  }

  private buildCreateCombatSkillDto(body: any, iconUrl: string): any {
    const dto: any = {
      ...body,
      iconUrl,
    };

    const numericFields = [
      'requiredWeaponType',
      'cooldown',
      'skillMultiplier',
      'projectileSpeed',
      'projectileRange',
      'projectileKnockback',
      'slashVfxDuration',
      'slashVfxSpawnOffset',
      'slashVfxPositionOffsetX',
      'slashVfxPositionOffsetY',
      'slashKnockbackForce',
    ];

    for (const field of numericFields) {
      if (body[field] !== undefined) {
        dto[field] = this.parseNumericField(body[field]);
      }
    }

    return dto;
  }

  private buildUpdateCombatSkillDto(body: any, iconUrl?: string): any {
    const dto: any = {
      ...body,
    };

    if (iconUrl) dto.iconUrl = iconUrl;

    const numericFields = [
      'requiredWeaponType',
      'cooldown',
      'skillMultiplier',
      'projectileSpeed',
      'projectileRange',
      'projectileKnockback',
      'slashVfxDuration',
      'slashVfxSpawnOffset',
      'slashVfxPositionOffsetX',
      'slashVfxPositionOffsetY',
      'slashKnockbackForce',
    ];

    for (const field of numericFields) {
      if (body[field] !== undefined) {
        dto[field] = this.parseNumericField(body[field]);
      }
    }

    return dto;
  }

  @Post('player-data/world')
  async createWorld(@Body() body: any, @Req() req: Request) {
    const ownerIdRaw = req['user']?.sub;
    const ownerId = ownerIdRaw ? String(ownerIdRaw) : undefined;
    if (!ownerId) throw new UnauthorizedException('Missing owner');
    // forward optional _id for update, otherwise create
    try {
      return await firstValueFrom(
        this.playerDataClient.send('create-world', {
          _id: body._id,
          worldName: body.worldName,
          ownerId,
        }),
      );
    } catch (err) {
      const payload = err?.message ?? err;
      let status = 500;
      let message = 'Internal server error';
      if (typeof payload === 'string') {
        try {
          const parsed = JSON.parse(payload);
          status = parsed.status || status;
          message = parsed.message || parsed.error || payload;
        } catch {
          message = payload;
        }
      } else if (payload && typeof payload === 'object') {
        status = payload.status || payload.code || status;
        message = payload.message || payload.error || JSON.stringify(payload);
      }
      throw new HttpException(message, status);
    }
  }

  @Get('player-data/world')
  async getWorld(@Query('_id') _id: string, @Req() req: Request) {
    try {
      const ownerIdRaw = req['user']?.sub;
      const ownerId = ownerIdRaw ? String(ownerIdRaw) : undefined;
      return await firstValueFrom(
        this.playerDataClient.send('get-world', { _id, ownerId }),
      );
    } catch (err) {
      const payload = err?.message ?? err;
      let status = 500;
      let message = 'Internal server error';
      if (typeof payload === 'string') {
        try {
          const parsed = JSON.parse(payload);
          status = parsed.status || status;
          message = parsed.message || parsed.error || payload;
        } catch {
          message = payload;
        }
      } else if (payload && typeof payload === 'object') {
        status = payload.status || payload.code || status;
        message = payload.message || payload.error || JSON.stringify(payload);
      }
      throw new HttpException(message, status);
    }
  }

  @Put('player-data/world')
  async updateWorld(@Body() body: UpdateWorldDto, @Req() req: Request) {
    const ownerIdRaw = req['user']?.sub;
    const ownerId = ownerIdRaw ? String(ownerIdRaw) : undefined;
    if (!ownerId) throw new UnauthorizedException('Missing owner');
    try {
      // Forward to save-world which handles time + characters + tile deltas
      // inside a MongoDB transaction (falls back gracefully on standalone).
      return await firstValueFrom(
        this.playerDataClient.send('save-world', { ...body, ownerId }),
      );
    } catch (err) {
      throw this.rpcError(err);
    }
  }


  @Delete('player-data/world')
  async deleteWorld(@Query('_id') _id: string, @Req() req: Request) {
    const ownerIdRaw = req['user']?.sub;
    const ownerId = ownerIdRaw ? String(ownerIdRaw) : undefined;
    if (!ownerId) throw new UnauthorizedException('Missing owner');
    try {
      return await firstValueFrom(
        this.playerDataClient.send('delete-world', { _id, ownerId }),
      );
    } catch (err) {
      const payload = err?.message ?? err;
      let status = 500;
      let message = 'Internal server error';
      if (typeof payload === 'string') {
        try {
          const parsed = JSON.parse(payload);
          status = parsed.status || status;
          message = parsed.message || parsed.error || payload;
        } catch {
          message = payload;
        }
      } else if (payload && typeof payload === 'object') {
        status = payload.status || payload.code || status;
        message = payload.message || payload.error || JSON.stringify(payload);
      }
      throw new HttpException(message, status);
    }
  }

  @Get('player-data/world/blacklist')
  async getWorldBlacklist(@Query() query: WorldBlacklistQueryDto, @Req() req: Request) {
    const requesterIdRaw = req['user']?.sub;
    const requesterId = requesterIdRaw ? String(requesterIdRaw) : undefined;
    const requesterIsAdmin = !!req['user']?.isAdmin;
    if (!requesterId) throw new UnauthorizedException('Missing requester');
    try {
      const response = await firstValueFrom(
        this.playerDataClient.send('get-world-blacklist', {
          worldId: query._id,
          requesterId,
          requesterIsAdmin,
        }),
      );
      return await this.enrichBlacklistResponse(response);
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  @Post('player-data/world/blacklist')
  async addWorldBlacklistPlayer(
    @Body() dto: UpdateWorldBlacklistDto,
    @Req() req: Request,
  ) {
    const requesterIdRaw = req['user']?.sub;
    const requesterId = requesterIdRaw ? String(requesterIdRaw) : undefined;
    const requesterIsAdmin = !!req['user']?.isAdmin;
    if (!requesterId) throw new UnauthorizedException('Missing requester');
    try {
      const response = await firstValueFrom(
        this.playerDataClient.send('add-world-blacklist-player', {
          worldId: dto._id,
          requesterId,
          requesterIsAdmin,
          playerId: dto.playerId,
        }),
      );
      return await this.enrichBlacklistResponse(response);
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  @Delete('player-data/world/blacklist')
  async removeWorldBlacklistPlayer(
    @Body() dto: UpdateWorldBlacklistDto,
    @Req() req: Request,
  ) {
    const requesterIdRaw = req['user']?.sub;
    const requesterId = requesterIdRaw ? String(requesterIdRaw) : undefined;
    const requesterIsAdmin = !!req['user']?.isAdmin;
    if (!requesterId) throw new UnauthorizedException('Missing requester');
    try {
      const response = await firstValueFrom(
        this.playerDataClient.send('remove-world-blacklist-player', {
          worldId: dto._id,
          requesterId,
          requesterIsAdmin,
          playerId: dto.playerId,
        }),
      );
      return await this.enrichBlacklistResponse(response);
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  @Get('player-data/worlds')
  async getWorldsByOwner(
    @Req() req: Request,
    @Query('ownerId') ownerIdQuery?: string,
  ) {
    const user = req['user'];
    let ownerId = user?.sub ? String(user.sub) : undefined;
    if (ownerIdQuery && user?.isAdmin) ownerId = String(ownerIdQuery);
    if (!ownerId) throw new UnauthorizedException('Missing owner');
    try {
      return await firstValueFrom(
        this.playerDataClient.send('get-worlds-by-owner', { ownerId }),
      );
    } catch (err) {
      const payload = err?.message ?? err;
      let status = 500;
      let message = 'Internal server error';
      if (typeof payload === 'string') {
        try {
          const parsed = JSON.parse(payload);
          status = parsed.status || status;
          message = parsed.message || parsed.error || payload;
        } catch {
          message = payload;
        }
      } else if (payload && typeof payload === 'object') {
        status = payload.status || payload.code || status;
        message = payload.message || payload.error || JSON.stringify(payload);
      }
      throw new HttpException(message, status);
    }
  }

  @Post('auth/register')
  async register(@Body() createAccountDto: CreateAccountDto) {
    try {
      return await firstValueFrom(
        this.authClient.send('register', createAccountDto),
      );
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  @Post('auth/verify-registration')
  async verifyRegistration(@Body() dto: VerifyRegistrationDto) {
    try {
      return await firstValueFrom(
        this.authClient.send('verify-registration', dto),
      );
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  @Post('auth/login-ingame')
  async login(@Body() loginDto: any) {
    try {
      return await firstValueFrom(
        this.authClient.send('login-ingame', loginDto),
      );
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  @Post('auth/register-admin')
  async registerAdmin(@Body() createAdminDto: any) {
    try {
      return await firstValueFrom(
        this.authClient.send('register-admin', createAdminDto),
      );
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  @Post('auth/login-admin')
  async loginAdmin(
    @Body() loginDto: any,
    @Res({ passthrough: true }) res: Response,
  ) {
    try {
      const result = await firstValueFrom(
        this.authClient.send('login-admin', loginDto),
      );
      const token = result?.access_token;
      if (!token) throw new HttpException('Login failed', 401);
      res.cookie('access_token', token, {
        httpOnly: true,
        secure: true,
        sameSite: 'lax',
        maxAge: 60 * 60 * 1000,
      });
      return {
        userId: result.userId,
        username: result.username,
        access_token: token,
      };
    } catch (err) {
      if (err instanceof HttpException) throw err;
      throw this.rpcError(err);
    }
  }

  @Get('auth/admin-check')
  async adminCheck(@Req() req: Request) {
    return req['user'];
  }

  @Post('auth/logout')
  async logout(
    @Headers('authorization') authHeader: string,
    @Headers('cookie') cookieHeader: string,
    @Res({ passthrough: true }) res: Response,
  ) {
    const tokenFromHeader = authHeader?.split(' ')[1];
    const cookies = (cookieHeader || '')
      .split(';')
      .reduce<Record<string, string>>((acc, c) => {
        const [k, v] = c.split('=').map((s) => s?.trim());
        if (k && v) acc[k] = decodeURIComponent(v);
        return acc;
      }, {});
    const token = tokenFromHeader ?? cookies['access_token'];
    if (token) {
      await firstValueFrom(this.authClient.send('logout', token));
    }
    res.clearCookie('access_token', {
      httpOnly: true,
      secure: true,
      sameSite: 'lax',
    });
    return { ok: true };
  }

  @Post('blog/create')
  async createBlog(@Body() createBlogDto: CreateBlogDto) {
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
  ) {
    return this.adminClient.send('update-blog', { id, updateBlogDto });
  }

  @Delete('blog/delete/:id')
  async deleteBlog(@Param('id') id: string) {
    return this.adminClient.send('delete-blog', id);
  }

  @Post('news/upload-signature')
  async getNewsUploadSignature(@Body() dto: UploadSignatureDto) {
    return this.adminClient.send('news-upload-signature', dto);
  }

  @Post('news/create')
  async createNews(@Body() createNewsDto: CreateNewsDto) {
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
  ) {
    return this.adminClient.send('update-news', { id, updateNewsDto });
  }

  @Delete('news/delete/:id')
  async deleteNews(@Param('id') id: string) {
    return this.adminClient.send('delete-news', id);
  }

  @Post('media/upload-signature')
  async getMediaUploadSignature(@Body() dto: UploadSignatureDto) {
    return this.adminClient.send('media-upload-signature', dto);
  }

  @Post('media/create')
  async createMedia(@Body() createMediaDto: CreateMediaDto) {
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
  ) {
    return this.adminClient.send('update-media', { id, updateMediaDto });
  }

  @Delete('media/delete/:id')
  async deleteMedia(@Param('id') id: string) {
    return this.adminClient.send('delete-media', id);
  }

  @Post('auth/reset/request')
  async resetRequest(@Body() dto: RequestResetDto) {
    try {
      return await firstValueFrom(
        this.authClient.send('reset-request', dto),
      );
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  @Post('auth/reset/confirm')
  async resetConfirm(@Body() dto: ConfirmResetDto) {
    try {
      return await firstValueFrom(
        this.authClient.send('reset-confirm', dto),
      );
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  // ── Game Data: Resource Configs ─────────────────────────────────────────────

  @Get('game-data/resource-configs/catalog')
  async getResourceConfigCatalog() {
    try {
      return await firstValueFrom(
        this.adminClient.send('get-resource-config-catalog', {}),
      );
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  @Post('game-data/resource-configs')
  @UseInterceptors(
    FileInterceptor('sprite', { limits: { fileSize: 5 * 1024 * 1024 } }),
  )
  async createResourceConfig(
    @UploadedFile() file: Express.Multer.File,
    @Body() body: any,
  ) {
    try {
      const dto: any = {
        resourceId: body.resourceId,
        name: body.name,
        maxHp: Number(body.maxHp),
      };

      if (body.requiredToolId) dto.requiredToolId = body.requiredToolId;
      if (body.resourceType) dto.resourceType = body.resourceType;
      if (body.spawnWeight !== undefined)
        dto.spawnWeight = Number(body.spawnWeight);

      if (file) {
        dto.spriteUrl = await this.cloudinaryService.uploadFile(
          file,
          body.folder || 'resource-sprites',
        );
      }

      if (body.dropTable) {
        try {
          dto.dropTable =
            typeof body.dropTable === 'string'
              ? JSON.parse(body.dropTable)
              : body.dropTable;
        } catch {
          throw new BadRequestException('dropTable must be a valid JSON array');
        }
      } else {
        dto.dropTable = [];
      }

      return await firstValueFrom(
        this.adminClient.send('create-resource-config', dto),
      );
    } catch (err) {
      if (err instanceof HttpException) throw err;
      throw this.rpcError(err);
    }
  }

  @Put('game-data/resource-configs/:resourceId')
  @UseInterceptors(
    FileInterceptor('sprite', { limits: { fileSize: 5 * 1024 * 1024 } }),
  )
  async updateResourceConfig(
    @Param('resourceId') resourceId: string,
    @UploadedFile() file: Express.Multer.File,
    @Body() body: any,
  ) {
    try {
      const dto: any = {};
      if (body.name) dto.name = body.name;
      if (body.maxHp !== undefined) dto.maxHp = Number(body.maxHp);
      if (body.requiredToolId !== undefined)
        dto.requiredToolId = body.requiredToolId;
      if (body.resourceType !== undefined)
        dto.resourceType = body.resourceType;
      if (body.spawnWeight !== undefined)
        dto.spawnWeight = Number(body.spawnWeight);

      if (file) {
        dto.spriteUrl = await this.cloudinaryService.uploadFile(
          file,
          body.folder || 'resource-sprites',
        );
      }

      if (body.dropTable !== undefined) {
        try {
          dto.dropTable =
            typeof body.dropTable === 'string'
              ? JSON.parse(body.dropTable)
              : body.dropTable;
        } catch {
          throw new BadRequestException('dropTable must be a valid JSON array');
        }
      }

      return await firstValueFrom(
        this.adminClient.send('update-resource-config', { resourceId, dto }),
      );
    } catch (err) {
      if (err instanceof HttpException) throw err;
      throw this.rpcError(err);
    }
  }

  @Delete('game-data/resource-configs/:resourceId')
  async deleteResourceConfig(@Param('resourceId') resourceId: string) {
    try {
      return await firstValueFrom(
        this.adminClient.send('delete-resource-config', { resourceId }),
      );
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  // ── Game Data: Items ────────────────────────────────────────────────────────

  /** POST /game-data/items/create — accepts multipart/form-data with an icon file
   *  + item JSON fields. Uploads the icon to Cloudinary internally, then
   *  creates the item in admin-service (admin only). */
  @Post('game-data/items/create')
  @UseInterceptors(
    FileFieldsInterceptor(
      [
        { name: 'icon', maxCount: 1 },
        { name: 'structureInteractionSprite', maxCount: 1 },
      ],
      { limits: { fileSize: 5 * 1024 * 1024 } },
    ),
  )
  async createItem(
    @UploadedFiles()
    files: {
      icon?: Express.Multer.File[];
      structureInteractionSprite?: Express.Multer.File[];
    },
    @Body() body: any,
  ) {
    const iconFile = files?.icon?.[0];
    if (!iconFile)
      throw new BadRequestException(
        'An icon file is required (field name: "icon")',
      );
    try {
      const iconUrl = await this.cloudinaryService.uploadFile(
        iconFile,
        body.folder || 'item-icons',
      );

      let structureInteractionSpriteUrl: string | undefined;
      const interactionFile = files?.structureInteractionSprite?.[0];
      if (interactionFile) {
        structureInteractionSpriteUrl = await this.cloudinaryService.uploadFile(
          interactionFile,
          body.folder || 'item-icons',
        );
      }

      const dto = this.buildCreateItemDto(body, iconUrl, undefined, structureInteractionSpriteUrl);

      return await firstValueFrom(this.adminClient.send('create-item', dto));
    } catch (err) {
      if (err instanceof HttpException) throw err;
      throw this.rpcError(err);
    }
  }

  /** GET /game-data/items/catalog — full catalog { items: [...] } for Unity client */
  @Get('game-data/items/catalog')
  async getItemCatalog() {
    try {
      return await firstValueFrom(
        this.adminClient.send('get-item-catalog', {}),
      );
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  /** GET /game-data/items/all — flat array of all items */
  @Get('game-data/items/all')
  async getAllItems() {
    try {
      return await firstValueFrom(this.adminClient.send('get-all-items', {}));
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  /** GET /game-data/items/by-item-id/:itemID — find by game-side itemID string */
  @Get('game-data/items/by-item-id/:itemID')
  async getItemByItemId(@Param('itemID') itemID: string) {
    try {
      return await firstValueFrom(
        this.adminClient.send('get-item-by-item-id', itemID),
      );
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  /** GET /game-data/items/:id — find by MongoDB _id */
  @Get('game-data/items/:id')
  async getItemById(@Param('id') id: string) {
    try {
      return await firstValueFrom(this.adminClient.send('get-item-by-id', id));
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  /** PUT /game-data/items/:itemID — update item by game-side itemID.
   *  Accepts multipart/form-data; include an "icon" file to replace the icon. */
  @Put('game-data/items/:itemID')
  @UseInterceptors(
    FileFieldsInterceptor(
      [
        { name: 'icon', maxCount: 1 },
        { name: 'structureInteractionSprite', maxCount: 1 },
      ],
      { limits: { fileSize: 5 * 1024 * 1024 } },
    ),
  )
  async updateItem(
    @Param('itemID') itemID: string,
    @UploadedFiles()
    files: {
      icon?: Express.Multer.File[];
      structureInteractionSprite?: Express.Multer.File[];
    },
    @Body() body: any,
  ) {
    try {
      let iconUrl: string | undefined;
      const iconFile = files?.icon?.[0];
      if (iconFile) {
        iconUrl = await this.cloudinaryService.uploadFile(
          iconFile,
          body.folder || 'item-icons',
        );
      }

      let structureInteractionSpriteUrl: string | undefined;
      const interactionFile = files?.structureInteractionSprite?.[0];
      if (interactionFile) {
        structureInteractionSpriteUrl = await this.cloudinaryService.uploadFile(
          interactionFile,
          body.folder || 'item-icons',
        );
      }

      const dto = this.buildUpdateItemDto(body, iconUrl, undefined, structureInteractionSpriteUrl);

      return await firstValueFrom(
        this.adminClient.send('update-item', { itemID, dto }),
      );
    } catch (err) {
      if (err instanceof HttpException) throw err;
      throw this.rpcError(err);
    }
  }

  /** DELETE /game-data/items/:itemID — delete by game-side itemID (admin) */
  @Delete('game-data/items/:itemID')
  async deleteItem(@Param('itemID') itemID: string) {
    try {
      return await firstValueFrom(this.adminClient.send('delete-item', itemID));
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  // ── Game Data: Combat Skills ───────────────────────────────────────────────

  /** GET /game-data/combat-skills/catalog — full catalog { skills: [...] } for Unity client */
  @Get('game-data/combat-skills/catalog')
  async getCombatSkillCatalog() {
    try {
      return await firstValueFrom(
        this.adminClient.send('get-combat-skill-catalog', {}),
      );
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  /** GET /game-data/combat-skills/all — flat array of all combat skill documents */
  @Get('game-data/combat-skills/all')
  async getAllCombatSkills() {
    try {
      return await firstValueFrom(
        this.adminClient.send('get-all-combat-skills', {}),
      );
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  /** GET /game-data/combat-skills/by-skill-id/:skillId — find by game-side skillId string */
  @Get('game-data/combat-skills/by-skill-id/:skillId')
  async getCombatSkillById(@Param('skillId') skillId: string) {
    try {
      return await firstValueFrom(
        this.adminClient.send('get-combat-skill-by-id', { skillId }),
      );
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  /** POST /game-data/combat-skills/create — accepts multipart/form-data with required icon file (admin only) */
  @Post('game-data/combat-skills/create')
  @UseInterceptors(
    FileInterceptor('icon', { limits: { fileSize: 5 * 1024 * 1024 } }),
  )
  async createCombatSkill(
    @UploadedFile() file: Express.Multer.File,
    @Body() body: any,
  ) {
    if (!file)
      throw new BadRequestException(
        'An icon file is required (field name: "icon")',
      );

    try {
      const iconUrl = await this.cloudinaryService.uploadFile(
        file,
        body.folder || 'skill-icons',
      );
      const dto = this.buildCreateCombatSkillDto(body, iconUrl);

      return await firstValueFrom(
        this.adminClient.send('create-combat-skill', dto),
      );
    } catch (err) {
      if (err instanceof HttpException) throw err;
      throw this.rpcError(err);
    }
  }

  /** PUT /game-data/combat-skills/:skillId — accepts multipart/form-data; include icon file to replace icon (admin only) */
  @Put('game-data/combat-skills/:skillId')
  @UseInterceptors(
    FileInterceptor('icon', { limits: { fileSize: 5 * 1024 * 1024 } }),
  )
  async updateCombatSkill(
    @Param('skillId') skillId: string,
    @UploadedFile() file: Express.Multer.File,
    @Body() patch: any,
  ) {
    try {
      let iconUrl: string | undefined;
      if (file) {
        iconUrl = await this.cloudinaryService.uploadFile(
          file,
          patch.folder || 'skill-icons',
        );
      }

      const dto = this.buildUpdateCombatSkillDto(patch, iconUrl);

      return await firstValueFrom(
        this.adminClient.send('update-combat-skill', { skillId, ...dto }),
      );
    } catch (err) {
      if (err instanceof HttpException) throw err;
      throw this.rpcError(err);
    }
  }

  /** DELETE /game-data/combat-skills/:skillId — delete by game-side skillId (admin only) */
  @Delete('game-data/combat-skills/:skillId')
  async deleteCombatSkill(@Param('skillId') skillId: string) {
    try {
      return await firstValueFrom(
        this.adminClient.send('delete-combat-skill', { skillId }),
      );
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  @Post('game-data/fertilizers/create')
  @UseInterceptors(
    FileInterceptor('icon', { limits: { fileSize: 5 * 1024 * 1024 } }),
  )
  async createFertilizer(
    @UploadedFile() file: Express.Multer.File,
    @Body() body: any,
  ) {
    if (!file)
      throw new BadRequestException(
        'An icon file is required (field name: "icon")',
      );

    try {
      const iconUrl = await this.cloudinaryService.uploadFile(
        file,
        body.folder || 'item-icons',
      );
      const dto = this.buildCreateItemDto(body, iconUrl, FERTILIZER_ITEM_TYPE);

      return await firstValueFrom(
        this.adminClient.send('create-fertilizer', dto),
      );
    } catch (err) {
      if (err instanceof HttpException) throw err;
      throw this.rpcError(err);
    }
  }

  @Get('game-data/fertilizers/catalog')
  async getFertilizerCatalog() {
    try {
      return await firstValueFrom(
        this.adminClient.send('get-fertilizer-catalog', {}),
      );
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  @Get('game-data/fertilizers/all')
  async getAllFertilizers() {
    try {
      return await firstValueFrom(
        this.adminClient.send('get-all-fertilizers', {}),
      );
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  @Get('game-data/fertilizers/by-item-id/:itemID')
  async getFertilizerByItemId(@Param('itemID') itemID: string) {
    try {
      return await firstValueFrom(
        this.adminClient.send('get-fertilizer-by-item-id', itemID),
      );
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  @Get('game-data/fertilizers/:id')
  async getFertilizerById(@Param('id') id: string) {
    try {
      return await firstValueFrom(
        this.adminClient.send('get-fertilizer-by-id', id),
      );
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  @Put('game-data/fertilizers/:itemID')
  @UseInterceptors(
    FileInterceptor('icon', { limits: { fileSize: 5 * 1024 * 1024 } }),
  )
  async updateFertilizer(
    @Param('itemID') itemID: string,
    @UploadedFile() file: Express.Multer.File,
    @Body() body: any,
  ) {
    try {
      let iconUrl: string | undefined;
      if (file) {
        iconUrl = await this.cloudinaryService.uploadFile(
          file,
          body.folder || 'item-icons',
        );
      }

      const dto = this.buildUpdateItemDto(
        body,
        iconUrl,
        FERTILIZER_ITEM_TYPE,
      );

      return await firstValueFrom(
        this.adminClient.send('update-fertilizer', { itemID, dto }),
      );
    } catch (err) {
      if (err instanceof HttpException) throw err;
      throw this.rpcError(err);
    }
  }

  @Delete('game-data/fertilizers/:itemID')
  async deleteFertilizer(@Param('itemID') itemID: string) {
    try {
      return await firstValueFrom(
        this.adminClient.send('delete-fertilizer', itemID),
      );
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  // ── Game Data: Plants ───────────────────────────────────────────────────────

  /** POST /game-data/plants/create — multipart/form-data.
   *
   *  File fields:
   *    stageSprites         — repeated file field; filenames must end with _<stageIndex>
   *                           e.g. cabbage_0.png, cabbage_1.png, cabbage_2.png
   *    hybridFlowerSprite   — optional; sprite at pollenStage (hybrid plants only)
   *    hybridMatureSprite   — optional; sprite at pollenStage+1 (hybrid plants only)
   *
   *  Text fields:
   *    plantId, plantName, harvestedItemId  — required
   *    growthStages  — JSON string: [{"stageNum":0,"growthDurationMinutes":0},{"stageNum":1,"growthDurationMinutes":30},…]
   *                    stageIconUrl is injected automatically from the uploaded sprites.
   *    All other CreatePlantDto optional fields as plain strings.
   *
   *  All sprites are uploaded to Cloudinary folder "plant-sprites" internally.
   */
  @Post('game-data/plants/create')
  @UseInterceptors(
    AnyFilesInterceptor({ limits: { fileSize: 5 * 1024 * 1024 } }),
  )
  async createPlant(
    @UploadedFiles() files: Express.Multer.File[],
    @Body() body: any,
  ) {
    try {
      // Parse growthStages JSON string sent as a form field
      let stages: { stageNum: number; growthDurationMinutes: number; stageIconUrl?: string }[] =
        [];
      if (body.growthStages) {
        try {
          stages = JSON.parse(body.growthStages);
        } catch {
          throw new BadRequestException(
            'growthStages must be a valid JSON string array',
          );
        }
      }
      if (!stages.length) {
        throw new BadRequestException(
          'growthStages must contain at least one entry',
        );
      }

      // Separate files by fieldname
      const stageFiles = (files ?? []).filter(
        (f) => f.fieldname === 'stageSprites',
      );
      const hybridFlowerFile = (files ?? []).find(
        (f) => f.fieldname === 'hybridFlowerSprite',
      );
      const hybridMatureFile = (files ?? []).find(
        (f) => f.fieldname === 'hybridMatureSprite',
      );

      if (stageFiles.length !== stages.length) {
        throw new BadRequestException(
          `Expected ${stages.length} stageSprites file(s), received ${stageFiles.length}`,
        );
      }

      // Use form order for stage sprites (first file → stage 0, second → stage 1, etc.)
      for (let i = 0; i < stages.length; i++) {
        const publicId = stageFiles[i].originalname.replace(
          /\.[^.]+$/,
          '',
        );
        stages[i].stageIconUrl = await this.cloudinaryService.uploadFile(
          stageFiles[i],
          'plant-sprites',
          publicId,
        );
      }

      // Upload optional hybrid sprites
      let hybridFlowerIconUrl: string | undefined;
      let hybridMatureIconUrl: string | undefined;
      if (hybridFlowerFile)
        hybridFlowerIconUrl = await this.cloudinaryService.uploadFile(
          hybridFlowerFile,
          'plant-sprites',
          hybridFlowerFile.originalname.replace(/\.[^.]+$/, ''),
        );
      if (hybridMatureFile)
        hybridMatureIconUrl = await this.cloudinaryService.uploadFile(
          hybridMatureFile,
          'plant-sprites',
          hybridMatureFile.originalname.replace(/\.[^.]+$/, ''),
        );

      const dto: CreatePlantDto = {
        plantId: body.plantId,
        plantName: body.plantName,
        harvestedItemId: body.harvestedItemId,
        growthStages: stages as any,
        ...(body.canProducePollen !== undefined && {
          canProducePollen:
            body.canProducePollen === 'true' || body.canProducePollen === true,
        }),
        ...(body.pollenStage !== undefined && {
          pollenStage: Number(body.pollenStage),
        }),
        ...(body.pollenItemId && { pollenItemId: body.pollenItemId }),
        ...(body.maxPollenHarvestsPerStage !== undefined && {
          maxPollenHarvestsPerStage: Number(body.maxPollenHarvestsPerStage),
        }),
        ...(body.growingSeason !== undefined && {
          growingSeason: Number(body.growingSeason),
        }),
        ...(body.isHybrid !== undefined && {
          isHybrid: body.isHybrid === 'true' || body.isHybrid === true,
        }),
        ...(body.receiverPlantId && { receiverPlantId: body.receiverPlantId }),
        ...(body.pollenPlantId && { pollenPlantId: body.pollenPlantId }),
        ...(hybridFlowerIconUrl && { hybridFlowerIconUrl }),
        ...(hybridMatureIconUrl && { hybridMatureIconUrl }),
        ...(body.dropSeeds !== undefined && {
          dropSeeds: body.dropSeeds === 'true' || body.dropSeeds === true,
        }),
      };

      return await firstValueFrom(this.adminClient.send('create-plant', dto));
    } catch (err) {
      if (err instanceof HttpException) throw err;
      throw this.rpcError(err);
    }
  }

  /** GET /game-data/plants/catalog — full catalog { plants: [...] } for Unity PlantCatalogService */
  @Get('game-data/plants/catalog')
  async getPlantCatalog() {
    try {
      return await firstValueFrom(
        this.adminClient.send('get-plant-catalog', {}),
      );
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  /** GET /game-data/plants/all — flat array of all plants */
  @Get('game-data/plants/all')
  async getAllPlants() {
    try {
      return await firstValueFrom(this.adminClient.send('get-all-plants', {}));
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  /** GET /game-data/plants/by-plant-id/:plantId — find by game-side plantId string */
  @Get('game-data/plants/by-plant-id/:plantId')
  async getPlantByPlantId(@Param('plantId') plantId: string) {
    try {
      return await firstValueFrom(
        this.adminClient.send('get-plant-by-plant-id', plantId),
      );
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  /** GET /game-data/plants/:id — find by MongoDB _id */
  @Get('game-data/plants/:id')
  async getPlantById(@Param('id') id: string) {
    try {
      return await firstValueFrom(this.adminClient.send('get-plant-by-id', id));
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  /** PUT /game-data/plants/:plantId — update an existing plant by game-side plantId.
   *  Accepts multipart/form-data; include sprite files to replace sprites. */
  @Put('game-data/plants/:plantId')
  @UseInterceptors(
    AnyFilesInterceptor({ limits: { fileSize: 5 * 1024 * 1024 } }),
  )
  async updatePlant(
    @Param('plantId') plantId: string,
    @UploadedFiles() files: Express.Multer.File[],
    @Body() body: any,
  ) {
    try {
      const dto: UpdatePlantDto = {};

      if (body.plantName !== undefined) dto.plantName = body.plantName;
      if (body.harvestedItemId !== undefined)
        dto.harvestedItemId = body.harvestedItemId;
      if (body.pollenItemId !== undefined) dto.pollenItemId = body.pollenItemId;
      if (body.receiverPlantId !== undefined)
        dto.receiverPlantId = body.receiverPlantId;
      if (body.pollenPlantId !== undefined)
        dto.pollenPlantId = body.pollenPlantId;
      if (body.pollenStage !== undefined)
        dto.pollenStage = Number(body.pollenStage);
      if (body.maxPollenHarvestsPerStage !== undefined)
        dto.maxPollenHarvestsPerStage = Number(body.maxPollenHarvestsPerStage);
      if (body.growingSeason !== undefined)
        dto.growingSeason = Number(body.growingSeason);
      if (body.canProducePollen !== undefined)
        dto.canProducePollen =
          body.canProducePollen === 'true' || body.canProducePollen === true;
      if (body.isHybrid !== undefined)
        dto.isHybrid = body.isHybrid === 'true' || body.isHybrid === true;
      if (body.dropSeeds !== undefined)
        dto.dropSeeds = body.dropSeeds === 'true' || body.dropSeeds === true;

      // Re-parse growthStages if provided
      if (body.growthStages) {
        let stages: { stageNum: number; growthDurationMinutes: number; stageIconUrl?: string }[];
        try {
          stages = JSON.parse(body.growthStages);
        } catch {
          throw new BadRequestException(
            'growthStages must be a valid JSON string array',
          );
        }

        const stageFiles = (files ?? []).filter(
          (f) => f.fieldname === 'stageSprites',
        );
        if (stageFiles.length > 0) {
          if (stageFiles.length !== stages.length) {
            throw new BadRequestException(
              `Expected ${stages.length} stageSprites file(s), received ${stageFiles.length}`,
            );
          }
          // Use form order for stage sprites (first file → stage 0, second → stage 1, etc.)
          for (let i = 0; i < stages.length; i++) {
            const publicId = stageFiles[i].originalname.replace(
              /\.[^.]+$/,
              '',
            );
            stages[i].stageIconUrl = await this.cloudinaryService.uploadFile(
              stageFiles[i],
              'plant-sprites',
              publicId,
            );
          }
        }
        dto.growthStages = stages as any;
      }

      // Optional hybrid sprite replacements
      const hybridFlowerFile = (files ?? []).find(
        (f) => f.fieldname === 'hybridFlowerSprite',
      );
      const hybridMatureFile = (files ?? []).find(
        (f) => f.fieldname === 'hybridMatureSprite',
      );
      if (hybridFlowerFile)
        dto.hybridFlowerIconUrl = await this.cloudinaryService.uploadFile(
          hybridFlowerFile,
          'plant-sprites',
          hybridFlowerFile.originalname.replace(/\.[^.]+$/, ''),
        );
      if (hybridMatureFile)
        dto.hybridMatureIconUrl = await this.cloudinaryService.uploadFile(
          hybridMatureFile,
          'plant-sprites',
          hybridMatureFile.originalname.replace(/\.[^.]+$/, ''),
        );

      return await firstValueFrom(
        this.adminClient.send('update-plant', { plantId, dto }),
      );
    } catch (err) {
      if (err instanceof HttpException) throw err;
      throw this.rpcError(err);
    }
  }

  /** DELETE /game-data/plants/:plantId — delete by game-side plantId (admin) */
  @Delete('game-data/plants/:plantId')
  async deletePlant(@Param('plantId') plantId: string) {
    try {
      return await firstValueFrom(
        this.adminClient.send('delete-plant', plantId),
      );
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  // ── Game Data: Crafting Recipes ──────────────────────────────────────────────

  /** POST /game-data/crafting-recipes/create — create a new crafting recipe */
  @Post('game-data/crafting-recipes/create')
  async createCraftingRecipe(@Body() body: CreateCraftingRecipeDto) {
    try {
      return await firstValueFrom(
        this.adminClient.send('create-crafting-recipe', body),
      );
    } catch (err) {
      if (err instanceof HttpException) throw err;
      throw this.rpcError(err);
    }
  }

  /** GET /game-data/crafting-recipes/catalog — full catalog { recipes: [...] } for Unity client */
  @Get('game-data/crafting-recipes/catalog')
  async getCraftingRecipeCatalog() {
    try {
      return await firstValueFrom(
        this.adminClient.send('get-crafting-recipe-catalog', {}),
      );
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  /** GET /game-data/crafting-recipes/all — flat array of all crafting recipes */
  @Get('game-data/crafting-recipes/all')
  async getAllCraftingRecipes() {
    try {
      return await firstValueFrom(
        this.adminClient.send('get-all-crafting-recipes', {}),
      );
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  /** GET /game-data/crafting-recipes/by-recipe-id/:recipeID — find by game-side recipeID string */
  @Get('game-data/crafting-recipes/by-recipe-id/:recipeID')
  async getCraftingRecipeByRecipeId(@Param('recipeID') recipeID: string) {
    try {
      return await firstValueFrom(
        this.adminClient.send('get-crafting-recipe-by-recipe-id', recipeID),
      );
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  /** GET /game-data/crafting-recipes/:id — find by MongoDB _id */
  @Get('game-data/crafting-recipes/:id')
  async getCraftingRecipeById(@Param('id') id: string) {
    try {
      return await firstValueFrom(
        this.adminClient.send('get-crafting-recipe-by-id', id),
      );
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  /** PUT /game-data/crafting-recipes/:recipeID — update existing recipe by recipeID */
  @Put('game-data/crafting-recipes/:recipeID')
  async updateCraftingRecipe(
    @Param('recipeID') recipeID: string,
    @Body() body: UpdateCraftingRecipeDto,
  ) {
    try {
      return await firstValueFrom(
        this.adminClient.send('update-crafting-recipe', {
          recipeID,
          dto: body,
        }),
      );
    } catch (err) {
      if (err instanceof HttpException) throw err;
      throw this.rpcError(err);
    }
  }

  /** DELETE /game-data/crafting-recipes/:recipeID — delete by game-side recipeID (admin) */
  @Delete('game-data/crafting-recipes/:recipeID')
  async deleteCraftingRecipe(@Param('recipeID') recipeID: string) {
    try {
      return await firstValueFrom(
        this.adminClient.send('delete-crafting-recipe', recipeID),
      );
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  // ── Dropped Items ───────────────────────────────────────────────────────────

  /** POST /player-data/dropped-items — persist a new dropped item (auth required) */
  @Post('player-data/dropped-items')
  async createDroppedItem(@Body() body: any, @Req() req: Request) {
    const ownerId = req['user']?.sub ? String(req['user'].sub) : undefined;
    if (!ownerId) throw new UnauthorizedException('Missing owner');
    try {
      return await firstValueFrom(
        this.playerDataClient.send('create-dropped-item', body),
      );
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  /** DELETE /player-data/dropped-items/:dropId — remove a picked-up/despawned item (auth required) */
  @Delete('player-data/dropped-items/:dropId')
  async deleteDroppedItem(
    @Param('dropId') dropId: string,
    @Req() req: Request,
  ) {
    const ownerId = req['user']?.sub ? String(req['user'].sub) : undefined;
    if (!ownerId) throw new UnauthorizedException('Missing owner');
    try {
      return await firstValueFrom(
        this.playerDataClient.send('delete-dropped-item', { dropId }),
      );
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  /** GET /player-data/dropped-items?roomName=X&chunkX=Y&chunkY=Z — query items (auth required) */
  @Get('player-data/dropped-items')
  async getDroppedItems(
    @Query('roomName') roomName: string,
    @Query('chunkX') chunkX: string,
    @Query('chunkY') chunkY: string,
    @Req() req: Request,
  ) {
    const ownerId = req['user']?.sub ? String(req['user'].sub) : undefined;
    if (!ownerId) throw new UnauthorizedException('Missing owner');
    try {
      const payload: Record<string, any> = { roomName };
      if (chunkX !== undefined) payload.chunkX = Number(chunkX);
      if (chunkY !== undefined) payload.chunkY = Number(chunkY);
      return await firstValueFrom(
        this.playerDataClient.send('get-dropped-items', payload),
      );
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  // ── Game Config ────────────────────────────────────────────────────────────

  /** GET /game-config/main-menu — public (no auth).
   *  Returns { currentBackgroundUrl, version } or null if not set. */
  @Get('game-config/main-menu')
  async getMainMenuConfig() {
    try {
      return await firstValueFrom(
        this.adminClient.send('get-main-menu-config', {}),
      );
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  /** PUT /game-config/main-menu — admin-only, multipart/form-data.
   *  Accepts a "background" image file. Uploads to Cloudinary, then
   *  persists the URL in the GameConfig singleton document. */
  @Put('game-config/main-menu')
  @UseInterceptors(
    FileInterceptor('background', { limits: { fileSize: 10 * 1024 * 1024 } }),
  )
  async updateMainMenuBackground(
    @UploadedFile() file: Express.Multer.File,
  ) {
    if (!file)
      throw new BadRequestException(
        'A background image file is required (field name: "background")',
      );
    try {
      const url = await this.cloudinaryService.uploadFile(
        file,
        'game-config',
      );
      return await firstValueFrom(
        this.adminClient.send('update-main-menu-background', { url }),
      );
    } catch (err) {
      if (err instanceof HttpException) throw err;
      throw this.rpcError(err);
    }
  }

  // ── Game Data: Achievements (Admin) ────────────────────────────────────────

  /** POST /game-data/achievements/create — create a new achievement definition (admin only) */
  @Post('game-data/achievements/create')
  async createAchievement(@Body() dto: CreateAchievementDto) {
    try {
      return await firstValueFrom(this.adminClient.send('create-achievement', dto));
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  /** GET /game-data/achievements/all — list all achievement definitions */
  @Get('game-data/achievements/all')
  async getAllAchievements() {
    try {
      return await firstValueFrom(this.adminClient.send('get-all-achievements', {}));
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  /** GET /game-data/achievements/:achievementId — get one definition */
  @Get('game-data/achievements/:achievementId')
  async getAchievementById(@Param('achievementId') achievementId: string) {
    try {
      return await firstValueFrom(this.adminClient.send('get-achievement-by-id', achievementId));
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  /** PUT /game-data/achievements/:achievementId — update a definition (admin only) */
  @Put('game-data/achievements/:achievementId')
  async updateAchievement(
    @Param('achievementId') achievementId: string,
    @Body() dto: any,
  ) {
    try {
      return await firstValueFrom(
        this.adminClient.send('update-achievement', { achievementId, dto }),
      );
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  /** DELETE /game-data/achievements/:achievementId — delete a definition (admin only) */
  @Delete('game-data/achievements/:achievementId')
  async deleteAchievement(@Param('achievementId') achievementId: string) {
    try {
      return await firstValueFrom(
        this.adminClient.send('delete-achievement', achievementId),
      );
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  // ── Player Achievements ─────────────────────────────────────────────────────

  /** GET /player-data/achievement — get all achievements with this player's progress */
  @Get('player-data/achievement')
  async getPlayerAchievements(@Req() req: Request) {
    const accountId = req['user']?.sub;
    if (!accountId) throw new UnauthorizedException('Missing account');
    try {
      return await firstValueFrom(
        this.authClient.send('get-player-achievements', String(accountId)),
      );
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  /** PUT /player-data/achievement/progress — update progress on one requirement */
  @Put('player-data/achievement/progress')
  async updateAchievementProgress(
    @Body() dto: UpdateAchievementProgressDto,
    @Req() req: Request,
  ) {
    const accountId = req['user']?.sub;
    if (!accountId) throw new UnauthorizedException('Missing account');
    try {
      return await firstValueFrom(
        this.authClient.send('update-achievement-progress', {
          ...dto,
          accountId: String(accountId),
        }),
      );
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  /** PUT /player-data/achievement/progress/batch — update progress for multiple requirements in one call */
  @Put('player-data/achievement/progress/batch')
  async updateAchievementProgressBatch(
    @Body(new ParseArrayPipe({ items: UpdateAchievementProgressDto }))
    updates: UpdateAchievementProgressDto[],
    @Req() req: Request,
  ) {
    const accountId = req['user']?.sub;
    if (!accountId) throw new UnauthorizedException('Missing account');
    try {
      return await firstValueFrom(
        this.authClient.send('update-achievement-progress-batch', {
          accountId: String(accountId),
          updates,
        }),
      );
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  /** GET /player-data/combat/skill-loadout?worldId=... — get this player's persisted skill slots for a world */
  @Get('player-data/combat/skill-loadout')
  async getSkillLoadout(@Query('worldId') worldId: string, @Req() req: Request) {
    const accountId = req['user']?.sub;
    if (!accountId) throw new UnauthorizedException('Missing account');
    if (!worldId) throw new BadRequestException('worldId is required');

    try {
      return await firstValueFrom(
        this.playerDataClient.send('get-character-skill-loadout', {
          worldId,
          accountId: String(accountId),
        }),
      );
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  /** PUT /player-data/combat/skill-loadout — update this player's persisted skill slots */
  @Put('player-data/combat/skill-loadout')
  async updateSkillLoadout(
    @Body() dto: UpdateSkillLoadoutDto,
    @Req() req: Request,
  ) {
    const accountId = req['user']?.sub;
    if (!accountId) throw new UnauthorizedException('Missing account');
    if (!dto?.worldId) throw new BadRequestException('worldId is required');

    try {
      return await firstValueFrom(
        this.playerDataClient.send('update-character-skill-loadout', {
          worldId: dto.worldId,
          accountId: String(accountId),
          playerSkillSlotIds: Array.isArray(dto.playerSkillSlotIds)
            ? dto.playerSkillSlotIds
            : [],
        }),
      );
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  // ── Skin Catalog (Paper Doll) ──────────────────────────────────────────────

  /**
   * GET /game-data/skin-configs — public (no auth required).
   * Query param `layer` is optional (e.g. ?layer=tool).
   * Unity SkinCatalogManager calls this on startup.
   */
  @Get('game-data/skin-configs')
  async getSkinCatalog(@Query('layer') layer?: string) {
    try {
      return await firstValueFrom(
        this.adminClient.send('get-skin-catalog', { layer }),
      );
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  /**
   * POST /game-data/skin-configs — admin-only.
   * Accepts multipart/form-data.
   * File field : spritesheet  (PNG, max 10 MB) — required.
   * Text fields: configId, displayName, cellSize?, layer?
   */
  @Post('game-data/skin-configs')
  @UseInterceptors(
    FileInterceptor('spritesheet', { limits: { fileSize: 10 * 1024 * 1024 } }),
  )
  async createSkinConfig(
    @UploadedFile() file: Express.Multer.File,
    @Body() body: any,
  ) {
    if (!file)
      throw new BadRequestException(
        'A spritesheet file is required (field name: "spritesheet")',
      );
    try {
      const spritesheetUrl = await this.cloudinaryService.uploadFile(
        file,
        'skin-spritesheets',
        body.configId || undefined,
      );
      const dto = {
        configId: body.configId,
        displayName: body.displayName,
        spritesheetUrl,
        cellSize: body.cellSize !== undefined ? Number(body.cellSize) : undefined,
        layer: body.layer,
      };
      return await firstValueFrom(
        this.adminClient.send('create-skin-config', dto),
      );
    } catch (err) {
      if (err instanceof HttpException) throw err;
      throw this.rpcError(err);
    }
  }

  /**
   * PUT /game-data/skin-configs/:configId — admin-only.
   * Accepts multipart/form-data.
   * File field : spritesheet  (PNG, max 10 MB) — optional; omit to keep existing URL.
   * Text fields: displayName?, cellSize?, layer?
   */
  @Put('game-data/skin-configs/:configId')
  @UseInterceptors(
    FileInterceptor('spritesheet', { limits: { fileSize: 10 * 1024 * 1024 } }),
  )
  async updateSkinConfig(
    @Param('configId') configId: string,
    @UploadedFile() file: Express.Multer.File,
    @Body() body: any,
  ) {
    try {
      const patch: Record<string, any> = {};
      if (body.displayName !== undefined) patch.displayName = body.displayName;
      if (body.cellSize !== undefined) patch.cellSize = Number(body.cellSize);
      if (body.layer !== undefined) patch.layer = body.layer;

      if (file) {
        patch.spritesheetUrl = await this.cloudinaryService.uploadFile(
          file,
          'skin-spritesheets',
          configId,
        );
      }

      return await firstValueFrom(
        this.adminClient.send('update-skin-config', { configId, ...patch }),
      );
    } catch (err) {
      if (err instanceof HttpException) throw err;
      throw this.rpcError(err);
    }
  }

  /**
   * DELETE /game-data/skin-configs/:configId — admin-only.
   */
  @Delete('game-data/skin-configs/:configId')
  async deleteSkinConfig(@Param('configId') configId: string) {
    try {
      return await firstValueFrom(
        this.adminClient.send('delete-skin-config', { configId }),
      );
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  // ── Material Catalog ──────────────────────────────────────────────────────────

  /**
   * GET /game-data/materials/catalog — public.
   * Unity MaterialCatalogService calls this on startup.
   * Returns { materials: MaterialEntry[] } sorted by materialTier.
   */
  @Get('game-data/materials/catalog')
  async getMaterialCatalog() {
    try {
      return await firstValueFrom(
        this.adminClient.send('get-material-catalog', {}),
      );
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  /**
   * GET /game-data/materials — public, flat array.
   */
  @Get('game-data/materials')
  async getAllMaterials() {
    try {
      return await firstValueFrom(
        this.adminClient.send('get-all-materials', {}),
      );
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  /**
   * GET /game-data/materials/:materialId — public.
   */
  @Get('game-data/materials/:materialId')
  async getMaterialById(@Param('materialId') materialId: string) {
    try {
      return await firstValueFrom(
        this.adminClient.send('get-material-by-id', { materialId }),
      );
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  /**
   * POST /game-data/materials — admin-only.
   * Accepts multipart/form-data.
   * File field : spritesheet  (PNG, max 10 MB) — required.
   * Text fields: materialId, materialName, materialTier?, cellSize?, description?
   * The spritesheet is uploaded to Cloudinary folder 'material-spritesheets'.
   */
  @Post('game-data/materials')
  @UseInterceptors(
    FileInterceptor('spritesheet', { limits: { fileSize: 10 * 1024 * 1024 } }),
  )
  async createMaterial(
    @UploadedFile() file: Express.Multer.File,
    @Body() body: any,
  ) {
    if (!file)
      throw new BadRequestException(
        'A spritesheet file is required (field name: "spritesheet")',
      );
    try {
      const spritesheetUrl = await this.cloudinaryService.uploadFile(
        file,
        'material-spritesheets',
        body.materialId || undefined,
      );
      const dto = {
        materialId:    body.materialId,
        materialName:  body.materialName,
        spritesheetUrl,
        materialTier:  body.materialTier  !== undefined ? Number(body.materialTier)  : undefined,
        cellSize:      body.cellSize      !== undefined ? Number(body.cellSize)      : undefined,
        description:   body.description,
      };
      return await firstValueFrom(
        this.adminClient.send('create-material', dto),
      );
    } catch (err) {
      if (err instanceof HttpException) throw err;
      throw this.rpcError(err);
    }
  }

  /**
   * PUT /game-data/materials/:materialId — admin-only.
   * Accepts multipart/form-data.
   * File field : spritesheet  (PNG, max 10 MB) — optional.
   * Text fields: materialName?, materialTier?, cellSize?, description?
   */
  @Put('game-data/materials/:materialId')
  @UseInterceptors(
    FileInterceptor('spritesheet', { limits: { fileSize: 10 * 1024 * 1024 } }),
  )
  async updateMaterial(
    @Param('materialId') materialId: string,
    @UploadedFile() file: Express.Multer.File,
    @Body() body: any,
  ) {
    try {
      const patch: Record<string, any> = { materialId };
      if (body.materialName  !== undefined) patch.materialName  = body.materialName;
      if (body.materialTier  !== undefined) patch.materialTier  = Number(body.materialTier);
      if (body.cellSize      !== undefined) patch.cellSize      = Number(body.cellSize);
      if (body.description   !== undefined) patch.description   = body.description;

      if (file) {
        patch.spritesheetUrl = await this.cloudinaryService.uploadFile(
          file,
          'material-spritesheets',
          materialId,
        );
      }

      return await firstValueFrom(
        this.adminClient.send('update-material', patch),
      );
    } catch (err) {
      if (err instanceof HttpException) throw err;
      throw this.rpcError(err);
    }
  }

  /**
   * DELETE /game-data/materials/:materialId — admin-only.
   */
  @Delete('game-data/materials/:materialId')
  async deleteMaterial(@Param('materialId') materialId: string) {
    try {
      return await firstValueFrom(
        this.adminClient.send('delete-material', { materialId }),
      );
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  // ── Quest CRUD ─────────────────────────────────────────────────────────────

  /** POST /game-data/quests — create a new quest definition (admin only) */
  @Post('game-data/quests')
  async createQuest(@Body() body: CreateQuestDto) {
    try {
      return await firstValueFrom(this.playerDataClient.send('create-quest', body));
    } catch (err) {
      if (err instanceof HttpException) throw err;
      throw this.rpcError(err);
    }
  }

  /** GET /game-data/quests/catalog — full catalog { quests: [...] } for Unity client */
  @Get('game-data/quests/catalog')
  async getQuestCatalog() {
    try {
      return await firstValueFrom(this.playerDataClient.send('get-quest-catalog', {}));
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  /** GET /game-data/quests/all — flat array of all quests */
  @Get('game-data/quests/all')
  async getAllQuests() {
    try {
      return await firstValueFrom(this.playerDataClient.send('get-all-quests', {}));
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  /** GET /game-data/quests/by-quest-id/:questId — find by game-side questId string */
  @Get('game-data/quests/by-quest-id/:questId')
  async getQuestByQuestId(@Param('questId') questId: string) {
    try {
      return await firstValueFrom(this.playerDataClient.send('get-quest-by-quest-id', questId));
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  /** GET /game-data/quests/:id — find by MongoDB _id */
  @Get('game-data/quests/:id')
  async getQuestById(@Param('id') id: string) {
    try {
      return await firstValueFrom(this.playerDataClient.send('get-quest-by-id', id));
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  /** PUT /game-data/quests/:questId — update quest by game-side questId (admin only) */
  @Put('game-data/quests/:questId')
  async updateQuest(@Param('questId') questId: string, @Body() body: UpdateQuestDto) {
    try {
      return await firstValueFrom(
        this.playerDataClient.send('update-quest', { questId, dto: body }),
      );
    } catch (err) {
      if (err instanceof HttpException) throw err;
      throw this.rpcError(err);
    }
  }

  /** DELETE /game-data/quests/:questId — delete by game-side questId (admin only) */
  @Delete('game-data/quests/:questId')
  async deleteQuest(@Param('questId') questId: string) {
    try {
      return await firstValueFrom(this.playerDataClient.send('delete-quest', questId));
    } catch (err) {
      throw this.rpcError(err);
    }
  }
}
