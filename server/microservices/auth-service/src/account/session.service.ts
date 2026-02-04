import { Injectable } from '@nestjs/common';
import { InjectModel } from '@nestjs/mongoose';
import { Model } from 'mongoose';
import { Session, SessionDocument } from './session.schema';

@Injectable()
export class SessionService {
	constructor(
		@InjectModel(Session.name) private sessionModel: Model<SessionDocument>,
	) {}

	// Creates a session record used to enforce admin inactivity auto-logout
	async createSession(
		token: string,
		userId: string,
		inactivityTimeoutMinutes: number = 30,
	): Promise<SessionDocument> {
		const session = new this.sessionModel({
			token,
			userId,
			inactivityTimeoutMinutes,
			lastActivityAt: new Date(),
		});
		const saved = await session.save();
		console.log(`[session-service] Session created: userId=${userId}, timeout=${inactivityTimeoutMinutes}min`);
		return saved;
	}

	// Refreshes last activity time for active admin sessions
	async updateActivity(token: string): Promise<SessionDocument | null> {
		const updated = await this.sessionModel
			.findOneAndUpdate(
				{ token, isRevoked: false },
				{ lastActivityAt: new Date() },
				{ new: true },
			)
			.exec();
		if (updated) {
			console.log('[session-service] Activity updated');
		}
		return updated;
	}

	// Retrieves an active admin session by token
	async getSession(token: string): Promise<SessionDocument | null> {
		return this.sessionModel.findOne({ token, isRevoked: false }).exec();
	}

	// Marks admin session revoked to prevent further use
	async revokeSession(token: string): Promise<boolean> {
		const result = await this.sessionModel
			.findOneAndUpdate({ token }, { isRevoked: true }, { new: true })
			.exec();
		return !!result;
	}

	// Enforces inactivity timeout for admin sessions (auto-logout)
	async isSessionActive(token: string): Promise<boolean> {
		const session = await this.getSession(token);
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
			await this.revokeSession(token);
			console.log('[session-service] Session auto-revoked due to inactivity');
			return false;
		}

		return true;
	}
}
