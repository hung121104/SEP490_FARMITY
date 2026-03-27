import { Injectable } from '@nestjs/common';
import { InjectModel } from '@nestjs/mongoose';
import { Model } from 'mongoose';
import * as crypto from 'crypto';
import { Session, SessionDocument } from './session.schema';

@Injectable()
export class SessionService {
	constructor(
		@InjectModel(Session.name) private sessionModel: Model<SessionDocument>,
	) {}

	
	async createSession(
		userId: string,
		inactivityTimeoutMinutes: number = 60,
	): Promise<SessionDocument> {
		const sessionId = crypto.randomUUID();
		const session = new this.sessionModel({
			sessionId,
			userId,
			inactivityTimeoutMinutes,
			lastActivityAt: new Date(),
		});
		const saved = await session.save();
		console.log(`[session-service] Session created: sid=${sessionId}, userId=${userId}, timeout=${inactivityTimeoutMinutes}min`);
		return saved;
	}

	
	async updateActivity(sessionId: string): Promise<SessionDocument | null> {
		const updated = await this.sessionModel
			.findOneAndUpdate(
				{ sessionId, isRevoked: false },
				{ lastActivityAt: new Date() },
				{ new: true },
			)
			.exec();
		if (updated) {
			console.log('[session-service] Activity updated');
		}
		return updated;
	}

	
	async getSession(sessionId: string): Promise<SessionDocument | null> {
		return this.sessionModel.findOne({ sessionId, isRevoked: false }).exec();
	}

	
	async revokeSession(sessionId: string): Promise<boolean> {
		const result = await this.sessionModel
			.findOneAndUpdate({ sessionId }, { isRevoked: true }, { new: true })
			.exec();
		return !!result;
	}

	
	async isSessionActive(sessionId: string): Promise<boolean> {
		const session = await this.getSession(sessionId);
		if (!session) {
			console.log('[session-service] Session not found');
			return false;
		}

		const now = new Date();
		const lastActivity = new Date(session.lastActivityAt);
		const inactivityMs = session.inactivityTimeoutMinutes * 60 * 1000;
		const timeSinceLastActivityMs = now.getTime() - lastActivity.getTime();

		console.log(`[session-service] Inactivity ${Math.round(timeSinceLastActivityMs / 1000)}s / ${session.inactivityTimeoutMinutes}min`);

		if (timeSinceLastActivityMs > inactivityMs) {
			await this.revokeSession(sessionId);
			console.log('[session-service] Session auto-revoked due to inactivity');
			return false;
		}

		return true;
	}

	async recordHeartbeat(
		sessionId: string,
		offlineTimeoutSeconds: number,
		legitThresholdMs: number,
	): Promise<{ isLegit: boolean; cumulativeHeartbeatMs: number } | null> {
		const session = await this.sessionModel
			.findOne({ sessionId, isRevoked: false })
			.exec();

		if (!session) return null;

		const nowMs = Date.now();
		let deltaMs = 0;
		if (session.lastHeartbeatAt) {
			const previousMs = new Date(session.lastHeartbeatAt).getTime();
			const rawDelta = nowMs - previousMs;
			if (rawDelta > 0 && rawDelta <= offlineTimeoutSeconds * 1000) {
				deltaMs = rawDelta;
			}
		}

		session.cumulativeHeartbeatMs =
			(session.cumulativeHeartbeatMs || 0) + deltaMs;
		session.lastHeartbeatAt = new Date(nowMs);
		session.lastActivityAt = new Date(nowMs);

		if (!session.isLegit && session.cumulativeHeartbeatMs >= legitThresholdMs) {
			session.isLegit = true;
		}

		await session.save();

		return {
			isLegit: !!session.isLegit,
			cumulativeHeartbeatMs: session.cumulativeHeartbeatMs || 0,
		};
	}

	async hasActiveSessionForUser(userId: string): Promise<boolean> {
		return this.hasActiveSessionForUserWithOptions(userId, {
			useHeartbeatFreshness: false,
			offlineTimeoutSeconds: 0,
		});
	}

	async hasActiveSessionForUserWithOptions(
		userId: string,
		options: {
			useHeartbeatFreshness?: boolean;
			offlineTimeoutSeconds?: number;
		} = {},
	): Promise<boolean> {
		const useHeartbeatFreshness = !!options.useHeartbeatFreshness;
		const offlineTimeoutSeconds = Number(options.offlineTimeoutSeconds || 0);
		const offlineTimeoutMs = offlineTimeoutSeconds > 0 ? offlineTimeoutSeconds * 1000 : 0;

		const sessions = await this.sessionModel
			.find({ userId, isRevoked: false } as any)
			.exec();

		if (!sessions.length) return false;

		const nowMs = Date.now();
		let hasActive = false;

		for (const session of sessions) {
			const lastActivityMs = new Date(session.lastActivityAt).getTime();
			const inactivityMs = (session.inactivityTimeoutMinutes || 60) * 60 * 1000;
			const expired = nowMs - lastActivityMs > inactivityMs;
			const hasHeartbeat = !!session.lastHeartbeatAt;
			const heartbeatStale =
				useHeartbeatFreshness &&
				hasHeartbeat &&
				offlineTimeoutMs > 0 &&
				nowMs - new Date(session.lastHeartbeatAt as Date).getTime() > offlineTimeoutMs;
			const noHeartbeatAndStaleActivity =
				useHeartbeatFreshness &&
				!hasHeartbeat &&
				offlineTimeoutMs > 0 &&
				nowMs - lastActivityMs > offlineTimeoutMs;

			if (expired || heartbeatStale || noHeartbeatAndStaleActivity) {
				await this.sessionModel
					.findOneAndUpdate(
						{ sessionId: session.sessionId },
						{ isRevoked: true },
					)
					.exec();
				continue;
			}

			hasActive = true;
			break;
		}

		return hasActive;
	}
}
