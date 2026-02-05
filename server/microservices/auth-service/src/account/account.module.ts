import { Module } from '@nestjs/common';
import { MongooseModule } from '@nestjs/mongoose';
import { JwtModule } from '@nestjs/jwt';
import { Account, AccountSchema } from './account.schema';
import { Session, SessionSchema } from './session.schema';
import { AccountService } from './account.service';
import { AccountController } from './account.controller';
import { SessionService } from './session.service';

@Module({
  imports: [
    MongooseModule.forFeature([
      { name: Account.name, schema: AccountSchema },
      { name: Session.name, schema: SessionSchema },
    ]),
    JwtModule.register({
      secret: process.env.JWT_SECRET || 'your-secret-key',
      signOptions: { expiresIn: '24h' },
    }),
  ],
  controllers: [AccountController],
  providers: [AccountService, SessionService],
  exports: [AccountService, SessionService],
})
export class AccountModule {}