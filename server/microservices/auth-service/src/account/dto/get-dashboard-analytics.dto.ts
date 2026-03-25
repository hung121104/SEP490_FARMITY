import { IsISO8601, IsOptional } from 'class-validator';

export class GetDashboardAnalyticsDto {
  @IsOptional()
  @IsISO8601()
  startDate?: string;

  @IsOptional()
  @IsISO8601()
  endDate?: string;
}

export type DashboardAnalyticsResponse = {
  rangeStartUtc: string;
  rangeEndUtc: string;
  generatedAtUtc: string;
  totalUsers: number;
  dailyActiveUsers: number;
  concurrentPlayers: number;
  newUsers: number;
  returningUsers: number;
  legitActiveUsers: number;
  concurrentSource: 'redis-realtime' | 'mongo-fallback';
};
