import { Controller, Sse } from '@nestjs/common';
import { Observable } from 'rxjs';
import { CatalogSseService } from './catalog-sse.service';

@Controller('game-data')
export class CatalogSseController {
  constructor(private readonly catalogSseService: CatalogSseService) {}

  /** SSE stream — Unity Host connects here for real-time catalog changes. */
  @Sse('catalog-stream')
  stream(): Observable<MessageEvent> {
    return this.catalogSseService.getStream();
  }
}
