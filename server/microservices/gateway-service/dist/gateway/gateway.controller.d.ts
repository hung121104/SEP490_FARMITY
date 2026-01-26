import { ClientProxy } from '@nestjs/microservices';
import { CreateAccountDto } from './dto/create-account.dto';
import { SavePositionDto } from './dto/save-position.dto';
import { GetPositionDto } from './dto/get-position.dto';
export declare class GatewayController {
    private authClient;
    private playerDataClient;
    constructor(authClient: ClientProxy, playerDataClient: ClientProxy);
    register(createAccountDto: CreateAccountDto): Promise<import("rxjs").Observable<any>>;
    login(loginDto: any): Promise<import("rxjs").Observable<any>>;
    savePosition(savePositionDto: SavePositionDto): Promise<import("rxjs").Observable<any>>;
    getPosition(getPositionDto: GetPositionDto): Promise<import("rxjs").Observable<any>>;
}
