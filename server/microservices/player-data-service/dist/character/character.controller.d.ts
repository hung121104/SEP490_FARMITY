import { CharacterService } from './character.service';
import { SavePositionDto } from './dto/save-position.dto';
import { GetPositionDto } from './dto/get-position.dto';
export declare class CharacterController {
    private readonly characterService;
    constructor(characterService: CharacterService);
    savePosition(savePositionDto: SavePositionDto): Promise<import("./character.schema").Character>;
    getPosition(getPositionDto: GetPositionDto): Promise<{
        positionX: number;
        positionY: number;
        chunkIndex: number;
    }>;
}
