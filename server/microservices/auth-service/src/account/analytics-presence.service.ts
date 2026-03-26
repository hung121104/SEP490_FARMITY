import { Injectable, OnModuleDestroy } from '@nestjs/common';
import { ConfigService } from '@nestjs/config';
import Redis from 'ioredis';

@Injectable()
export class AnalyticsPresenceService implements OnModuleDestroy {
  private readonly redisEnabled: boolean;
  private readonly keyPrefix: string;
  private readonly sessionExpiryZsetKey: string;
  private readonly sidUserHashKey: string;
  private readonly offlineTimeoutSeconds: number;
  private client: Redis | null = null;

  constructor(private readonly configService: ConfigService) {
    const host = this.configService.get<string>('REDIS_HOST');
    this.redisEnabled = !!host;

    const prefix = this.configService.get<string>('REDIS_KEY_PREFIX') || 'farmity:analytics';
    this.keyPrefix = prefix;
    this.sessionExpiryZsetKey = `${this.keyPrefix}:presence:session-expiry`;
    this.sidUserHashKey = `${this.keyPrefix}:presence:sid-user`;
    this.offlineTimeoutSeconds = Number(
      this.configService.get<string>('HEARTBEAT_OFFLINE_TIMEOUT_SECONDS') || 300,
    );

    if (this.redisEnabled) {
      const port = Number(this.configService.get<string>('REDIS_PORT') || 6379);
      const password = this.configService.get<string>('REDIS_PASSWORD');
      const db = Number(this.configService.get<string>('REDIS_DB') || 0);
      const useTls = this.configService.get<string>('REDIS_TLS') === 'true';

      this.client = new Redis({
        host,
        port,
        password: password || undefined,
        db,
        tls: useTls ? {} : undefined,
        lazyConnect: true,
        maxRetriesPerRequest: 2,
      });
    }
  }

  async onModuleDestroy() {
    if (!this.client) return;
    try {
      await this.client.quit();
    } catch {
      await this.client.disconnect();
    }
  }

  isEnabled(): boolean {
    return this.redisEnabled;
  }

  private async ensureConnected(): Promise<boolean> {
    if (!this.client) return false;
    if (this.client.status === 'ready') return true;

    try {
      await this.client.connect();
      return true;
    } catch {
      return false;
    }
  }

  async touchHeartbeat(sessionId: string, userId: string): Promise<void> {
    if (!sessionId || !userId) return;
    const connected = await this.ensureConnected();
    if (!connected || !this.client) return;

    const nowMs = Date.now();
    const expiryMs = nowMs + this.offlineTimeoutSeconds * 1000;
    try {
      await this.client
        .multi()
        .zadd(this.sessionExpiryZsetKey, String(expiryMs), sessionId)
        .hset(this.sidUserHashKey, sessionId, userId)
        .exec();
    } catch {
      // Presence tracking is best-effort and must not break auth flows.
    }
  }

  async touchSession(sessionId: string, userId: string): Promise<void> {
    await this.touchHeartbeat(sessionId, userId);
  }

  async removeSession(sessionId: string): Promise<void> {
    if (!sessionId) return;
    const connected = await this.ensureConnected();
    if (!connected || !this.client) return;

    try {
      await this.client
        .multi()
        .zrem(this.sessionExpiryZsetKey, sessionId)
        .hdel(this.sidUserHashKey, sessionId)
        .exec();
    } catch {
      // Presence tracking is best-effort and must not break auth flows.
    }
  }

  async getRealtimeActiveUserIds(): Promise<string[] | null> {
    const connected = await this.ensureConnected();
    if (!connected || !this.client) return null;

    const nowMs = Date.now();

    try {
      await this.client.zremrangebyscore(this.sessionExpiryZsetKey, '-inf', String(nowMs - 1));

      const sessionIds = await this.client.zrangebyscore(
        this.sessionExpiryZsetKey,
        String(nowMs),
        '+inf',
      );

      if (!sessionIds.length) return [];

      const userIds = await this.client.hmget(this.sidUserHashKey, ...sessionIds);
      const uniqueUserIds = Array.from(
        new Set(
          userIds
            .filter((value): value is string => typeof value === 'string')
            .map((value) => value.trim())
            .filter((value) => value.length > 0),
        ),
      );

      return uniqueUserIds;
    } catch {
      return null;
    }
  }

  async getActiveUserIds(windowMs: number): Promise<string[] | null> {
    // Legacy compatibility path for old callers.
    return this.getRealtimeActiveUserIds();
  }
}
