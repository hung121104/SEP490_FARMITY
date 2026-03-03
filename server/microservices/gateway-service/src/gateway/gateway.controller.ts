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
} from '@nestjs/common';
import { ClientProxy } from '@nestjs/microservices';
import { FileInterceptor, AnyFilesInterceptor } from '@nestjs/platform-express';
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
import { RequestAdminResetDto } from './dto/request-admin-reset.dto';
import { ConfirmAdminResetDto } from './dto/confirm-admin-reset.dto';
import { UpdateWorldDto } from './dto/update-world.dto';
import { CreateItemDto } from './dto/create-item.dto';
import { CreatePlantDto } from './dto/create-plant.dto';
import { CreateCraftingRecipeDto } from './dto/create-crafting-recipe.dto';
import { UpdateCraftingRecipeDto } from './dto/update-crafting-recipe.dto';
import { UpdateItemDto } from './dto/update-item.dto';
import { GatewayCloudinaryService } from './cloudinary.service';
import { HttpStatus } from '@nestjs/common';

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
      return await firstValueFrom(
        this.playerDataClient.send('update-world', { ...body, ownerId }),
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

  @Post('auth/admin-reset/request')
  async adminResetRequest(@Body() dto: RequestAdminResetDto) {
    try {
      return await firstValueFrom(
        this.authClient.send('admin-reset-request', dto),
      );
    } catch (err) {
      throw this.rpcError(err);
    }
  }

  @Post('auth/admin-reset/confirm')
  async adminResetConfirm(@Body() dto: ConfirmAdminResetDto) {
    try {
      return await firstValueFrom(
        this.authClient.send('admin-reset-confirm', dto),
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
    FileInterceptor('icon', { limits: { fileSize: 5 * 1024 * 1024 } }),
  )
  async createItem(
    @UploadedFile() file: Express.Multer.File,
    @Body() body: any,
  ) {
    if (!file)
      throw new BadRequestException(
        'An icon file is required (field name: "icon")',
      );
    try {
      // Upload icon to Cloudinary internally — no separate endpoint needed
      const iconUrl = await this.cloudinaryService.uploadFile(
        file,
        body.folder || 'item-icons',
      );

      // Parse numeric/boolean fields that arrive as strings from form-data
      const dto: CreateItemDto = {
        ...body,
        iconUrl,
        itemType: Number(body.itemType),
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
    FileInterceptor('icon', { limits: { fileSize: 5 * 1024 * 1024 } }),
  )
  async updateItem(
    @Param('itemID') itemID: string,
    @UploadedFile() file: Express.Multer.File,
    @Body() body: any,
  ) {
    try {
      const dto: UpdateItemDto = { ...body };

      // If a new icon was uploaded, replace the iconUrl
      if (file) {
        dto.iconUrl = await this.cloudinaryService.uploadFile(
          file,
          body.folder || 'item-icons',
        );
      }

      // Parse numeric / boolean fields that arrive as strings from form-data
      if (body.itemType !== undefined) dto.itemType = Number(body.itemType);
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
   *    growthStages  — JSON string: [{"stageNum":0,"age":0},{"stageNum":1,"age":3},…]
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
      let stages: { stageNum: number; age: number; stageIconUrl?: string }[] =
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

      // Sort stage files by the trailing number in the original filename
      // e.g. "cabbage_0.png" → 0, "cabbage_2.png" → 2
      const parseStageIndex = (filename: string): number => {
        const match = filename.replace(/\.[^.]+$/, '').match(/(\d+)$/);
        if (!match)
          throw new BadRequestException(
            `Stage sprite filename "${filename}" must end with a stage index, e.g. "cabbage_0.png"`,
          );
        return parseInt(match[1], 10);
      };

      const sortedStageFiles = [...stageFiles].sort(
        (a, b) =>
          parseStageIndex(a.originalname) - parseStageIndex(b.originalname),
      );

      // Validate that the parsed indices are 0..N-1 with no gaps
      sortedStageFiles.forEach((f, i) => {
        const idx = parseStageIndex(f.originalname);
        if (idx !== i)
          throw new BadRequestException(
            `Stage sprite indices must be contiguous starting from 0. Got index ${idx} at position ${i}.`,
          );
      });

      // Upload stage sprites sorted by index and inject stageIconUrl
      for (let i = 0; i < stages.length; i++) {
        const publicId = sortedStageFiles[i].originalname.replace(
          /\.[^.]+$/,
          '',
        );
        stages[i].stageIconUrl = await this.cloudinaryService.uploadFile(
          sortedStageFiles[i],
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

  /** DELETE /game-data/plants/:id — delete by MongoDB _id (admin) */
  @Delete('game-data/plants/:id')
  async deletePlant(@Param('id') id: string) {
    try {
      return await firstValueFrom(this.adminClient.send('delete-plant', id));
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
}
