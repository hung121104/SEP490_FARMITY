import { Injectable, NestMiddleware, UnauthorizedException, Inject } from '@nestjs/common';
import { Request, Response, NextFunction } from 'express';
import { ClientProxy } from '@nestjs/microservices';
import { firstValueFrom } from 'rxjs';

@Injectable()
export class AuthMiddleware implements NestMiddleware {
  constructor(@Inject('AUTH_SERVICE') private readonly authClient: ClientProxy) {}

  async use(req: Request, _res: Response, next: NextFunction) {
    // Extract token from Authorization header
    const authHeader = (req.headers['authorization'] || '') as string;
    const tokenFromHeader = authHeader.startsWith('Bearer ') ? authHeader.slice(7) : undefined;

    // Extract token from cookies
    const cookieHeader = (req.headers['cookie'] || '') as string;
    const cookies = cookieHeader.split(';').reduce<Record<string, string>>((acc, c) => {
      const [k, v] = c.split('=').map(s => s?.trim());
      if (k && v) acc[k] = decodeURIComponent(v);
      return acc;
    }, {});
    const tokenFromCookie = cookies['access_token'];

    // Use token from either source
    const token = tokenFromHeader ?? tokenFromCookie;
    if (!token) throw new UnauthorizedException('Missing token');

    try {
      // Verify token with passive verification (doesn't reset inactivity timer)
      const payload = await firstValueFrom(this.authClient.send('verify-token-passive', token));
      
      // Check if user is admin
      if (!payload?.isAdmin) {
        throw new UnauthorizedException('Not an admin');
      }

      // Attach user payload to request
      req['user'] = payload;
      next();
    } catch (err) {
      throw new UnauthorizedException('Invalid token or not an admin');
    }
  }
}
