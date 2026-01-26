"use strict";
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.CharacterModule = void 0;
const common_1 = require("@nestjs/common");
const mongoose_1 = require("@nestjs/mongoose");
const microservices_1 = require("@nestjs/microservices");
const character_schema_1 = require("./character.schema");
const character_service_1 = require("./character.service");
const character_controller_1 = require("./character.controller");
let CharacterModule = class CharacterModule {
};
exports.CharacterModule = CharacterModule;
exports.CharacterModule = CharacterModule = __decorate([
    (0, common_1.Module)({
        imports: [
            mongoose_1.MongooseModule.forFeature([{ name: character_schema_1.Character.name, schema: character_schema_1.CharacterSchema }]),
            microservices_1.ClientsModule.register([
                {
                    name: 'AUTH_SERVICE',
                    transport: microservices_1.Transport.TCP,
                    options: { host: 'localhost', port: 8877 },
                },
            ]),
        ],
        controllers: [character_controller_1.CharacterController],
        providers: [character_service_1.CharacterService],
    })
], CharacterModule);
//# sourceMappingURL=character.module.js.map