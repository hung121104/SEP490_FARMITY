import { Injectable, NestMiddleware, UnauthorizedException, Inject } from '@nestjs/common';
import { Request, Response, NextFunction } from 'express';
import { ClientProxy } from '@nestjs/microservices';
import { firstValueFrom } from 'rxjs';

@Injectable()
export class AuthMiddleware implements NestMiddleware {
  constructor(@Inject('AUTH_SERVICE') private readonly authClient: ClientProxy) {}

  async use(req: Request, _res: Response, next: NextFunction) {
    const auth = (req.headers['authorization'] || '') as string;
    const token = auth.startsWith('Bearer ') ? auth.slice(7) : undefined;
    if (!token) throw new UnauthorizedException('Missing token');
    try {
      const payload = await firstValueFrom(this.authClient.send('verify-token', token));
      req['user'] = payload;
      next();
    } catch (err) {
      throw new UnauthorizedException('Invalid token');
    }
  }
}
