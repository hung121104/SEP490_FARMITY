import { Controller, Post, Body, Get, Query, Inject } from '@nestjs/common';
import { ClientProxy } from '@nestjs/microservices';
import { CreateAccountDto } from './dto/create-account.dto';
import { SavePositionDto } from './dto/save-position.dto';
import { GetPositionDto } from './dto/get-position.dto';

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
}