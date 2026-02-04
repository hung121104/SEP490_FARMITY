import { Injectable, BadRequestException } from '@nestjs/common';
import { InjectModel } from '@nestjs/mongoose';
import { Model, Types } from 'mongoose';
import { Session, SessionDocument } from './session.schema';

@Injectable()
export class SessionService {
  constructor(
    @InjectModel(Session.name) private sessionModel: Model<SessionDocument>,
  ) {}

  async createSession(
    token: string,
    userId: string,
    inactivityTimeoutMinutes: number = 1,
  ): Promise<SessionDocument> {
    const session = new this.sessionModel({
      token,
      userId,
      inactivityTimeoutMinutes,
      lastActivityAt: new Date(),
    });
    return session.save();
  }

  async updateActivity(token: string): Promise<SessionDocument | null> {
    return this.sessionModel
      .findOneAndUpdate(
        { token, isRevoked: false },
        { lastActivityAt: new Date() },
        { new: true },
      )
      .exec();
  }

  async getSession(token: string): Promise<SessionDocument | null> {
    return this.sessionModel.findOne({ token, isRevoked: false }).exec();
  }

  async revokeSession(token: string): Promise<boolean> {
    const result = await this.sessionModel
      .findOneAndUpdate({ token }, { isRevoked: true }, { new: true })
      .exec();
    return !!result;
  }

  async revokeUserSessions(userId: string): Promise<number> {
    const result = await this.sessionModel
      .updateMany(
        { userId, isRevoked: false } as any,
        { isRevoked: true },
      )
      .exec();

    return result.modifiedCount;
  }

  // Check if admin session is still active based on inactivity timeout
  // Auto-revokes session if inactivity period has exceeded (60 minutes for admin)
  // This provides automatic logout for idle admin sessions
  async isSessionActive(token: string): Promise<boolean> {
    const session = await this.getSession(token);
    if (!session) {
      console.log(`[session-service] Session not found`);
      return false;
    }

    const now = new Date();
    const lastActivity = new Date(session.lastActivityAt);
    const inactivityMs = session.inactivityTimeoutMinutes * 60 * 1000;
    const timeSinceLastActivityMs = now.getTime() - lastActivity.getTime();

    console.log(
      `[session-service] Timeout: ${session.inactivityTimeoutMinutes}min (${inactivityMs}ms), Time since activity: ${Math.round(
        timeSinceLastActivityMs / 1000
      )}s`,
    );

    if (timeSinceLastActivityMs > inactivityMs) {
      console.log(`[session-service] AUTO-LOGOUT: Session exceeded timeout!`);
      await this.revokeSession(token);
      return false;
    }

    console.log(`[session-service] Session still active`);
    return true;
  }

  // Cleanup revoked sessions older than 24 hours
  // Run periodically to maintain database performance
  // Call this as a scheduled task or cron job
  async cleanupExpiredSessions(): Promise<number> {
    const now = new Date();
    const result = await this.sessionModel
      .deleteMany({
        createdAt: {
          $lt: new Date(now.getTime() - 24 * 60 * 60 * 1000),
        },
        isRevoked: true,
      })
      .exec();
    return result.deletedCount;
  }
}