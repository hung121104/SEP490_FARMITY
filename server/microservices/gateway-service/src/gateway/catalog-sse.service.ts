import { Injectable } from '@nestjs/common';
import { Subject, Observable, interval, merge } from 'rxjs';
import { map, filter } from 'rxjs/operators';

/** Shape of a catalog change event from admin-service CUD controllers. */
export interface CatalogChange {
  data: any;
  catalogVersion: number;
  changeType: 'create' | 'update' | 'delete';
  entityType: string;
}

/** SSE event payload sent to connected Unity hosts. */
export interface CatalogSseEvent {
  type: string;
  entity: string;
  data: any;
  version: number;
}

@Injectable()
export class CatalogSseService {
  private readonly subject = new Subject<CatalogSseEvent>();

  /** Push a catalog change into the SSE stream.
   *  Called by GatewayController after each successful game-data CUD. */
  emit(change: CatalogChange): void {
    this.subject.next({
      type: change.changeType,
      entity: change.entityType,
      data: change.data,
      version: change.catalogVersion,
    });
  }

  /** Returns an Observable suitable for NestJS @Sse() endpoint.
   *  Merges catalog change events with a 30s keepalive ping to prevent idle disconnects. */
  getStream(): Observable<MessageEvent> {
    const changes$ = this.subject.asObservable().pipe(
      map((event) => ({ data: JSON.stringify(event) }) as MessageEvent),
    );
    // SSE comment ping — keeps connection alive through proxies/gateways
    const ping$ = interval(30_000).pipe(
      map(() => ({ data: ':ping' }) as unknown as MessageEvent),
    );
    return merge(changes$, ping$);
  }
}
