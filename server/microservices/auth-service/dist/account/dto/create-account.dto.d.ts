export declare class CreateAccountDto {
    username: string;
    password: string;
    email: string;
    gameSettings?: {
        audio?: boolean;
        keyBinds?: Record<string, string>;
    };
}
