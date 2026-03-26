import { Prop, Schema, SchemaFactory } from '@nestjs/mongoose';
import { Document, Schema as MongooseSchema } from 'mongoose';

export type SessionDocument = Session & Document;

@Schema({ timestamps: true })
export class Session {
	@Prop({ required: true, unique: true, index: true })
	sessionId: string;

	@Prop({ required: true })
	userId: MongooseSchema.Types.ObjectId;

	@Prop({ default: Date.now })
	createdAt: Date;

	@Prop({ default: Date.now })
	lastActivityAt: Date;

	@Prop({ default: null })
	lastHeartbeatAt: Date | null;

	@Prop({ default: 0 })
	cumulativeHeartbeatMs: number;

	@Prop({ default: false })
	isLegit: boolean;

	@Prop({ default: 30 })
	inactivityTimeoutMinutes: number;

	@Prop({ default: false })
	isRevoked: boolean;
}

export const SessionSchema = SchemaFactory.createForClass(Session);
SessionSchema.index({ createdAt: 1, userId: 1 });
SessionSchema.index({ lastActivityAt: 1, isRevoked: 1, userId: 1 });
SessionSchema.index({ createdAt: 1, isLegit: 1, userId: 1 });
