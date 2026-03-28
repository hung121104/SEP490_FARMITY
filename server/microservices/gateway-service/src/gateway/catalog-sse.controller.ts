import { Controller, Get, Inject, Sse } from '@nestjs/common';
import { ClientProxy } from '@nestjs/microservices';
import { firstValueFrom, Observable } from 'rxjs';
import { CatalogSseService } from './catalog-sse.service';

@Controller('game-data')
export class CatalogSseController {
  constructor(
    @Inject('ADMIN_SERVICE') private adminClient: ClientProxy,
    private readonly catalogSseService: CatalogSseService,
  ) {}

  /** SSE stream — Unity Host connects here for real-time catalog changes. */
  @Sse('catalog-stream')
  stream(): Observable<MessageEvent> {
    return this.catalogSseService.getStream();
  }

  /** REST — returns { version: number } for version checks. */
  @Get('catalog-version')
  async getVersion(): Promise<{ version: number }> {
    return await firstValueFrom(
      this.adminClient.send('get-catalog-version', {}),
    );
  }
}
