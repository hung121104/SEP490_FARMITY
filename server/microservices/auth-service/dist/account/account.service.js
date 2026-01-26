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
exports.AccountService = void 0;
const common_1 = require("@nestjs/common");
const mongoose_1 = require("@nestjs/mongoose");
const mongoose_2 = require("mongoose");
const bcrypt = require("bcrypt");
const jwt_1 = require("@nestjs/jwt");
const account_schema_1 = require("./account.schema");
let AccountService = class AccountService {
    constructor(accountModel, jwtService) {
        this.accountModel = accountModel;
        this.jwtService = jwtService;
    }
    async create(createAccountDto) {
        const { password, ...rest } = createAccountDto;
        const hashedPassword = await bcrypt.hash(password, 10);
        const account = new this.accountModel({
            ...rest,
            password: hashedPassword,
            gameSettings: {
                audio: createAccountDto.gameSettings?.audio ?? true,
                keyBinds: createAccountDto.gameSettings?.keyBinds ?? { moveup: 'w', attack: 'Left_Click' },
            },
        });
        try {
            return await account.save();
        }
        catch (error) {
            if (error.code === 11000) {
                throw new common_1.BadRequestException('Username or email already exists');
            }
            throw error;
        }
    }
    async findById(id) {
        return this.accountModel.findById(id).exec();
    }
    async login(loginDto) {
        const { username, password } = loginDto;
        const account = await this.accountModel.findOne({ username }).exec();
        if (!account) {
            throw new common_1.UnauthorizedException('Invalid credentials');
        }
        const isPasswordValid = await bcrypt.compare(password, account.password);
        if (!isPasswordValid) {
            throw new common_1.UnauthorizedException('Invalid credentials');
        }
        const payload = { username: account.username, sub: account._id };
        return {
            userId: account._id.toString(),
            username: account.username,
            access_token: this.jwtService.sign(payload),
        };
    }
};
exports.AccountService = AccountService;
exports.AccountService = AccountService = __decorate([
    (0, common_1.Injectable)(),
    __param(0, (0, mongoose_1.InjectModel)(account_schema_1.Account.name)),
    __metadata("design:paramtypes", [mongoose_2.Model,
        jwt_1.JwtService])
], AccountService);
//# sourceMappingURL=account.service.js.map