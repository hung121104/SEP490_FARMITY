import { Module } from '@nestjs/common';
import { MongooseModule } from '@nestjs/mongoose';
import { ClientsModule, Transport } from '@nestjs/microservices';
import { JwtModule } from '@nestjs/jwt';
import { ConfigModule } from '@nestjs/config';
import { Account, AccountSchema } from './account.schema';
import { Session, SessionSchema } from './session.schema';
import { UnverifiedAccount, UnverifiedAccountSchema } from './unverified-account.schema';
import { AccountService } from './account.service';
import { AccountController } from './account.controller';
import { SessionService } from './session.service';

@Module({
  imports: [
    ConfigModule,
    MongooseModule.forFeature([
      { name: Account.name, schema: AccountSchema },
      { name: Session.name, schema: SessionSchema },
      { name: UnverifiedAccount.name, schema: UnverifiedAccountSchema },
    ]),
    ClientsModule.register([
      {
        name: 'ADMIN_SERVICE',
        transport: Transport.TCP,
        options: { host: 'localhost', port: 3006 },
      },
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