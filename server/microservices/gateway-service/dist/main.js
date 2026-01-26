"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const core_1 = require("@nestjs/core");
const common_1 = require("@nestjs/common");
const app_module_1 = require("./app.module");
const fs = require("fs");
const path = require("path");
const dotenv = require("dotenv");
dotenv.config();
async function bootstrap() {
    const httpsOptions = {
        key: fs.readFileSync(path.join(process.cwd(), 'certs', 'localhost-key.pem')),
        cert: fs.readFileSync(path.join(process.cwd(), 'certs', 'localhost.pem')),
    };
    const app = await core_1.NestFactory.create(app_module_1.AppModule, { httpsOptions });
    app.useGlobalPipes(new common_1.ValidationPipe());
    await app.listen(process.env.PORT || 3000, '0.0.0.0');
    console.log(`Gateway HTTPS API listening on https://0.0.0.0:${process.env.PORT || 3000}`);
}
bootstrap();
//# sourceMappingURL=main.js.map