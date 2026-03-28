import { Injectable } from '@nestjs/common';
import { Subject, Observable, interval, merge } from 'rxjs';
import { map } from 'rxjs/operators';

/** Shape of a catalog change event from admin-service CUD controllers. */
export interface CatalogChange {
  data: any;
  changeType: 'create' | 'update' | 'delete';
  entityType: string;
}

/** SSE event payload sent to connected Unity hosts. */
export interface CatalogSseEvent {
  type: string;
  entity: string;
  data: any;
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
    });
  }

  /** Returns an Observable suitable for NestJS @Sse() endpoint.
   *  Merges catalog change events with a 30s keepalive ping to prevent idle disconnects. */
  getStream(): Observable<MessageEvent> {
    const changes$ = this.subject.asObservable().pipe(
      map((event) => ({ data: JSON.stringify(event) }) as MessageEvent),
    );
    const ping$ = interval(30_000).pipe(
      map(() => ({ data: ':ping' }) as unknown as MessageEvent),
    );
    return merge(changes$, ping$);
  }
}
