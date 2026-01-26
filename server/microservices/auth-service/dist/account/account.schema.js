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
Object.defineProperty(exports, "__esModule", { value: true });
exports.AccountSchema = exports.Account = exports.GameSettingsSchema = exports.GameSettings = void 0;
const mongoose_1 = require("@nestjs/mongoose");
let GameSettings = class GameSettings {
};
exports.GameSettings = GameSettings;
__decorate([
    (0, mongoose_1.Prop)({ default: true }),
    __metadata("design:type", Boolean)
], GameSettings.prototype, "audio", void 0);
__decorate([
    (0, mongoose_1.Prop)({ type: Object, default: { moveup: 'w', attack: 'Left_Click' } }),
    __metadata("design:type", Object)
], GameSettings.prototype, "keyBinds", void 0);
exports.GameSettings = GameSettings = __decorate([
    (0, mongoose_1.Schema)()
], GameSettings);
exports.GameSettingsSchema = mongoose_1.SchemaFactory.createForClass(GameSettings);
let Account = class Account {
};
exports.Account = Account;
__decorate([
    (0, mongoose_1.Prop)({ required: true, unique: true }),
    __metadata("design:type", String)
], Account.prototype, "username", void 0);
__decorate([
    (0, mongoose_1.Prop)({ required: true }),
    __metadata("design:type", String)
], Account.prototype, "password", void 0);
__decorate([
    (0, mongoose_1.Prop)({ required: true, unique: true }),
    __metadata("design:type", String)
], Account.prototype, "email", void 0);
__decorate([
    (0, mongoose_1.Prop)({ type: exports.GameSettingsSchema, default: () => ({}) }),
    __metadata("design:type", GameSettings)
], Account.prototype, "gameSettings", void 0);
exports.Account = Account = __decorate([
    (0, mongoose_1.Schema)()
], Account);
exports.AccountSchema = mongoose_1.SchemaFactory.createForClass(Account);
//# sourceMappingURL=account.schema.js.map