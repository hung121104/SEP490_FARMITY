import { Injectable } from '@nestjs/common';
import { ConfigService } from '@nestjs/config';
import { InjectModel } from '@nestjs/mongoose';
import { RpcException } from '@nestjs/microservices';
import { Model } from 'mongoose';
import { Account, AccountDocument } from './account.schema';
import { Session, SessionDocument } from './session.schema';
import {
  DashboardAnalyticsResponse,
  GetDashboardAnalyticsDto,
} from './dto/get-dashboard-analytics.dto';
import { AnalyticsPresenceService } from './analytics-presence.service';

@Injectable()
export class AnalyticsService {
  private readonly heartbeatOfflineTimeoutMs: number;

  constructor(
    @InjectModel(Account.name)
    private readonly accountModel: Model<AccountDocument>,
    @InjectModel(Session.name)
    private readonly sessionModel: Model<SessionDocument>,
    private readonly presenceService: AnalyticsPresenceService,
    private readonly configService: ConfigService,
  ) {
    const offlineTimeoutSeconds = Number(
      this.configService.get<string>('HEARTBEAT_OFFLINE_TIMEOUT_SECONDS') || 300,
    );
    this.heartbeatOfflineTimeoutMs = offlineTimeoutSeconds * 1000;
  }

  async getDashboardAnalytics(
    dto: GetDashboardAnalyticsDto,
  ): Promise<DashboardAnalyticsResponse> {
    const { start, end } = this.resolveRange(dto);

    const [
      totalUsers,
      newUsers,
      dailyActiveUsers,
      returningUsers,
      legitActiveUsers,
    ] =
      await Promise.all([
        this.accountModel.countDocuments({ isAdmin: false }).exec(),
        this.accountModel
          .countDocuments({
            isAdmin: false,
            createdAt: { $gte: start, $lt: end },
          })
          .exec(),
        this.getDailyActiveUsers(start, end),
        this.getReturningUsers(start, end),
        this.getLegitActiveUsers(start, end),
      ]);

    const concurrent = await this.getConcurrentPlayers();

    return {
      rangeStartUtc: start.toISOString(),
      rangeEndUtc: end.toISOString(),
      generatedAtUtc: new Date().toISOString(),
      totalUsers,
      dailyActiveUsers,
      concurrentPlayers: concurrent.value,
      newUsers,
      returningUsers,
      legitActiveUsers,
      concurrentSource: concurrent.source,
    };
  }

  private resolveRange(dto: GetDashboardAnalyticsDto): { start: Date; end: Date } {
    const hasStart = !!dto?.startDate;
    const hasEnd = !!dto?.endDate;

    if (!hasStart && !hasEnd) {
      const now = new Date();
      const start = new Date(
        Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate()),
      );
      const end = new Date(start.getTime() + 24 * 60 * 60 * 1000);
      return { start, end };
    }

    const parsedStart = hasStart ? new Date(dto.startDate as string) : null;
    const parsedEnd = hasEnd ? new Date(dto.endDate as string) : null;

    if ((parsedStart && Number.isNaN(parsedStart.getTime())) || (parsedEnd && Number.isNaN(parsedEnd.getTime()))) {
      throw new RpcException({ status: 400, message: 'Invalid startDate or endDate' });
    }

    let start: Date;
    let end: Date;

    if (parsedStart && parsedEnd) {
      start = parsedStart;
      end = parsedEnd;
    } else if (parsedStart) {
      start = parsedStart;
      end = new Date(parsedStart.getTime() + 24 * 60 * 60 * 1000);
    } else {
      const onlyEnd = parsedEnd as Date;
      start = new Date(
        Date.UTC(onlyEnd.getUTCFullYear(), onlyEnd.getUTCMonth(), onlyEnd.getUTCDate()),
      );
      end = onlyEnd;
    }

    if (start >= end) {
      throw new RpcException({
        status: 400,
        message: 'startDate must be earlier than endDate',
      });
    }

    return { start, end };
  }

  private async getDailyActiveUsers(start: Date, end: Date): Promise<number> {
    const inRangeUserIdsRaw = await this.sessionModel
      .distinct('userId', { createdAt: { $gte: start, $lt: end } })
      .exec();
    const inRangeUserIds = inRangeUserIdsRaw.map((id) => String(id));

    if (!inRangeUserIds.length) return 0;

    return this.accountModel
      .countDocuments({
        _id: { $in: inRangeUserIds },
        isAdmin: false,
      } as any)
      .exec();
  }

  private async getReturningUsers(start: Date, end: Date): Promise<number> {
    const inRangeUserIdsRaw = await this.sessionModel
      .distinct('userId', { createdAt: { $gte: start, $lt: end } })
      .exec();
    const inRangeUserIds = inRangeUserIdsRaw.map((id) => String(id));

    if (!inRangeUserIds.length) return 0;

    const nonAdminUsersInRangeRaw = await this.accountModel
      .distinct('_id', {
        _id: { $in: inRangeUserIds },
        isAdmin: false,
      } as any)
      .exec();
    const nonAdminUsersInRange = nonAdminUsersInRangeRaw.map((id) => String(id));

    if (!nonAdminUsersInRange.length) return 0;

    const returningIds = await this.sessionModel
      .distinct('userId', {
        userId: { $in: nonAdminUsersInRange },
        createdAt: { $lt: start },
      } as any)
      .exec();

    return returningIds.length;
  }

  private async getLegitActiveUsers(start: Date, end: Date): Promise<number> {
    const legitUserIdsRaw = await this.sessionModel
      .distinct('userId', {
        createdAt: { $gte: start, $lt: end },
        isLegit: true,
      })
      .exec();

    const legitUserIds = legitUserIdsRaw.map((id) => String(id));
    if (!legitUserIds.length) return 0;

    return this.accountModel
      .countDocuments({ _id: { $in: legitUserIds }, isAdmin: false } as any)
      .exec();
  }

  private async getConcurrentPlayers(): Promise<{
    value: number;
    source: 'redis-realtime' | 'mongo-fallback';
  }> {
    if (this.presenceService.isEnabled()) {
      const activeRedisUserIds =
        await this.presenceService.getRealtimeActiveUserIds();

      if (activeRedisUserIds !== null) {
        if (!activeRedisUserIds.length) {
          return { value: 0, source: 'redis-realtime' };
        }

        const value = await this.accountModel
          .countDocuments({ _id: { $in: activeRedisUserIds }, isAdmin: false } as any)
          .exec();

        return { value, source: 'redis-realtime' };
      }
    }

    const cutoff = new Date(Date.now() - this.heartbeatOfflineTimeoutMs);
    const activeSessionUserIdsRaw = await this.sessionModel
      .distinct('userId', {
        isRevoked: false,
        lastHeartbeatAt: { $gte: cutoff },
      })
      .exec();
    const activeSessionUserIds = activeSessionUserIdsRaw.map((id) => String(id));

    if (!activeSessionUserIds.length) {
      return { value: 0, source: 'mongo-fallback' };
    }

    const value = await this.accountModel
      .countDocuments({ _id: { $in: activeSessionUserIds }, isAdmin: false } as any)
      .exec();

    return { value, source: 'mongo-fallback' };
  }
}
