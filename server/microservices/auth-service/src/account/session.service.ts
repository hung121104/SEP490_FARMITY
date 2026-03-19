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
}
