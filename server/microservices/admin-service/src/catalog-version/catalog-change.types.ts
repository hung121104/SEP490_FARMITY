/** Wrapper returned by CUD (Create/Update/Delete) controller methods.
 *  The gateway unpacks `data` for the HTTP response and forwards
 *  the full object to the SSE stream for real-time sync. */
export interface CatalogChange<T = any> {
  data: T;
  catalogVersion: number;
  changeType: 'create' | 'update' | 'delete';
  entityType: string;
}
