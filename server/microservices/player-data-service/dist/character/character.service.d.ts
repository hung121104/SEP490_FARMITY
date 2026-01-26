import { Model } from 'mongoose';
import { ClientProxy } from '@nestjs/microservices';
import { Character, CharacterDocument } from './character.schema';
import { SavePositionDto } from './dto/save-position.dto';
import { GetPositionDto } from './dto/get-position.dto';
export declare class CharacterService {
    private characterModel;
    private authClient;
    private savePositionCounter;
    private getPositionCounter;
    constructor(characterModel: Model<CharacterDocument>, authClient: ClientProxy);
    savePosition(savePositionDto: SavePositionDto): Promise<Character>;
    getPosition(getPositionDto: GetPositionDto): Promise<{
        positionX: number;
        positionY: number;
        chunkIndex: number;
    } | null>;
}
