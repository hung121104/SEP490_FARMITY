import { Injectable, NestMiddleware, UnauthorizedException } from '@nestjs/common';
import { Request, Response, NextFunction } from 'express';

@Injectable()
export class AuthenticationMiddleware implements NestMiddleware {
  async use(req: Request, _res: Response, next: NextFunction) {
    const user = req['user'];
    if (!user || !user.isAdmin) {
      throw new UnauthorizedException('Admin privileges required');
    }
    next();
  }
}
