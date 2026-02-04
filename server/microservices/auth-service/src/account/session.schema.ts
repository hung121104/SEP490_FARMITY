import { Prop, Schema, SchemaFactory } from '@nestjs/mongoose';
import { Document, Schema as MongooseSchema } from 'mongoose';

export type SessionDocument = Session & Document;

@Schema({ timestamps: true })
export class Session {
	@Prop({ required: true })
	token: string;

	@Prop({ required: true })
	userId: MongooseSchema.Types.ObjectId;

	@Prop({ default: Date.now })
	createdAt: Date;

	@Prop({ default: Date.now })
	lastActivityAt: Date;

	@Prop({ default: 30 })
	inactivityTimeoutMinutes: number;

	@Prop({ default: false })
	isRevoked: boolean;
}

export const SessionSchema = SchemaFactory.createForClass(Session);
