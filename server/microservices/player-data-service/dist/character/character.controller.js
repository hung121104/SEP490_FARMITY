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
exports.CharacterController = void 0;
const common_1 = require("@nestjs/common");
const microservices_1 = require("@nestjs/microservices");
const character_service_1 = require("./character.service");
const save_position_dto_1 = require("./dto/save-position.dto");
const get_position_dto_1 = require("./dto/get-position.dto");
let CharacterController = class CharacterController {
    constructor(characterService) {
        this.characterService = characterService;
    }
    async savePosition(savePositionDto) {
        return this.characterService.savePosition(savePositionDto);
    }
    async getPosition(getPositionDto) {
        return this.characterService.getPosition(getPositionDto);
    }
};
exports.CharacterController = CharacterController;
__decorate([
    (0, microservices_1.MessagePattern)('save-position'),
    __param(0, (0, common_1.Body)()),
    __metadata("design:type", Function),
    __metadata("design:paramtypes", [save_position_dto_1.SavePositionDto]),
    __metadata("design:returntype", Promise)
], CharacterController.prototype, "savePosition", null);
__decorate([
    (0, microservices_1.MessagePattern)('get-position'),
    __param(0, (0, common_1.Body)()),
    __metadata("design:type", Function),
    __metadata("design:paramtypes", [get_position_dto_1.GetPositionDto]),
    __metadata("design:returntype", Promise)
], CharacterController.prototype, "getPosition", null);
exports.CharacterController = CharacterController = __decorate([
    (0, common_1.Controller)(),
    __metadata("design:paramtypes", [character_service_1.CharacterService])
], CharacterController);
//# sourceMappingURL=character.controller.js.map