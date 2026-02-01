import { Controller, Post, Body, Get, Query, Inject, Headers, UnauthorizedException } from '@nestjs/common';
import { ClientProxy } from '@nestjs/microservices';
import { CreateAccountDto } from './dto/create-account.dto';
import { SavePositionDto } from './dto/save-position.dto';
import { GetPositionDto } from './dto/get-position.dto';
import { firstValueFrom } from 'rxjs';

@Controller()
export class GatewayController {
  constructor(
    @Inject('AUTH_SERVICE') private authClient: ClientProxy,
    @Inject('PLAYER_DATA_SERVICE') private playerDataClient: ClientProxy,
  ) {}

  @Post('auth/register')
  async register(@Body() createAccountDto: CreateAccountDto) {
    return this.authClient.send('register', createAccountDto);
  }

  @Post('auth/login-ingame')
  async login(@Body() loginDto: any) {
    return this.authClient.send('login-ingame', loginDto);
  }

  @Post('player-data/save-position')
  async savePosition(@Body() savePositionDto: SavePositionDto) {
    return this.playerDataClient.send('save-position', savePositionDto);
  }

  @Get('player-data/position')
  async getPosition(@Query() getPositionDto: GetPositionDto) {
    return this.playerDataClient.send('get-position', getPositionDto);
  }

  @Post('auth/register-admin')
  async registerAdmin(@Body() createAdminDto: any) {
    return this.authClient.send('register-admin', createAdminDto);
  }

  @Post('auth/login-admin')
  async loginAdmin(@Body() loginDto: any) {
    return this.authClient.send('login-admin', loginDto);
  }

  @Get('auth/admin-check')
  async adminCheck(@Headers('authorization') authHeader: string) {
    const token = authHeader?.split(' ')[1];
    if (!token) throw new UnauthorizedException('Missing token');

    const payload = await firstValueFrom(this.authClient.send('verify-token', token));
    if (!payload?.isAdmin) throw new UnauthorizedException('Not an admin');

    return { ok: true, user: payload };
  }
}