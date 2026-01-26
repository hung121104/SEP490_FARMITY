"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const core_1 = require("@nestjs/core");
const microservices_1 = require("@nestjs/microservices");
const common_1 = require("@nestjs/common");
const dotenv = require("dotenv");
const app_module_1 = require("./app.module");
dotenv.config();
async function bootstrap() {
    const app = await core_1.NestFactory.createMicroservice(app_module_1.AppModule, {
        transport: microservices_1.Transport.TCP,
        options: { host: 'localhost', port: parseInt(process.env.PORT || '8878') },
    });
    app.useGlobalPipes(new common_1.ValidationPipe());
    await app.listen();
    console.log(`Player Data TCP Microservice listening on port ${process.env.PORT || '8878'}`);
}
bootstrap();
//# sourceMappingURL=main.js.map