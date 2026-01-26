"use strict";
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};
var __param = (this && this.__param) || function (paramIndex, decorator) {
    return function (target, key) { decorator(target, key, paramIndex); }
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.CharacterService = void 0;
const common_1 = require("@nestjs/common");
const mongoose_1 = require("@nestjs/mongoose");
const mongoose_2 = require("mongoose");
const microservices_1 = require("@nestjs/microservices");
const character_schema_1 = require("./character.schema");
let CharacterService = class CharacterService {
    constructor(characterModel, authClient) {
        this.characterModel = characterModel;
        this.authClient = authClient;
        this.savePositionCounter = 0;
        this.getPositionCounter = 0;
    }
    async savePosition(savePositionDto) {
        const account = await this.authClient.send('find-account', savePositionDto.accountId).toPromise();
        if (!account) {
            throw new common_1.BadRequestException('Invalid account');
        }
        const start = Date.now();
        this.savePositionCounter++;
        const result = await this.characterModel.findOneAndUpdate({ worldId: savePositionDto.worldId, accountId: savePositionDto.accountId }, {
            positionX: savePositionDto.positionX,
            positionY: savePositionDto.positionY,
            chunkIndex: savePositionDto.chunkIndex,
        }, { upsert: true, new: true });
        const end = Date.now();
        console.log(`Endpoint: save-position, Call count: ${this.savePositionCounter}, Time: ${end - start}ms`);
        return result;
    }
    async getPosition(getPositionDto) {
        const account = await this.authClient.send('find-account', getPositionDto.accountId).toPromise();
        if (!account) {
            throw new common_1.BadRequestException('Invalid account');
        }
        const start = Date.now();
        this.getPositionCounter++;
        const character = await this.characterModel.findOne({ worldId: getPositionDto.worldId, accountId: getPositionDto.accountId }, { positionX: 1, positionY: 1, chunkIndex: 1, _id: 0 });
        const end = Date.now();
        console.log(`Endpoint: get-position, Call count: ${this.getPositionCounter}, Time: ${end - start}ms`);
        return character ? { positionX: character.positionX, positionY: character.positionY, chunkIndex: character.chunkIndex } : null;
    }
};
exports.CharacterService = CharacterService;
exports.CharacterService = CharacterService = __decorate([
    (0, common_1.Injectable)(),
    __param(0, (0, mongoose_1.InjectModel)(character_schema_1.Character.name)),
    __param(1, (0, common_1.Inject)('AUTH_SERVICE')),
    __metadata("design:paramtypes", [mongoose_2.Model,
        microservices_1.ClientProxy])
], CharacterService);
//# sourceMappingURL=character.service.js.map