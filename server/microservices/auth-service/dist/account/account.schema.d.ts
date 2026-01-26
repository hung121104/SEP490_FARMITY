import { Document } from 'mongoose';
export type AccountDocument = Account & Document;
export declare class GameSettings {
    audio: boolean;
    keyBinds: Record<string, string>;
}
export declare const GameSettingsSchema: import("mongoose").Schema<GameSettings, import("mongoose").Model<GameSettings, any, any, any, (Document<unknown, any, GameSettings, any, import("mongoose").DefaultSchemaOptions> & GameSettings & {
    _id: import("mongoose").Types.ObjectId;
} & {
    __v: number;
} & {
    id: string;
}) | (Document<unknown, any, GameSettings, any, import("mongoose").DefaultSchemaOptions> & GameSettings & {
    _id: import("mongoose").Types.ObjectId;
} & {
    __v: number;
}), any, GameSettings>, {}, {}, {}, {}, import("mongoose").DefaultSchemaOptions, GameSettings, Document<unknown, {}, GameSettings, {
    id: string;
}, import("mongoose").ResolveSchemaOptions<import("mongoose").DefaultSchemaOptions>> & Omit<GameSettings & {
    _id: import("mongoose").Types.ObjectId;
} & {
    __v: number;
}, "id"> & {
    id: string;
}, {
    audio?: import("mongoose").SchemaDefinitionProperty<boolean, GameSettings, Document<unknown, {}, GameSettings, {
        id: string;
    }, import("mongoose").ResolveSchemaOptions<import("mongoose").DefaultSchemaOptions>> & Omit<GameSettings & {
        _id: import("mongoose").Types.ObjectId;
    } & {
        __v: number;
    }, "id"> & {
        id: string;
    }>;
    keyBinds?: import("mongoose").SchemaDefinitionProperty<Record<string, string>, GameSettings, Document<unknown, {}, GameSettings, {
        id: string;
    }, import("mongoose").ResolveSchemaOptions<import("mongoose").DefaultSchemaOptions>> & Omit<GameSettings & {
        _id: import("mongoose").Types.ObjectId;
    } & {
        __v: number;
    }, "id"> & {
        id: string;
    }>;
}, GameSettings>;
export declare class Account {
    username: string;
    password: string;
    email: string;
    gameSettings: GameSettings;
}
export declare const AccountSchema: import("mongoose").Schema<Account, import("mongoose").Model<Account, any, any, any, (Document<unknown, any, Account, any, import("mongoose").DefaultSchemaOptions> & Account & {
    _id: import("mongoose").Types.ObjectId;
} & {
    __v: number;
} & {
    id: string;
}) | (Document<unknown, any, Account, any, import("mongoose").DefaultSchemaOptions> & Account & {
    _id: import("mongoose").Types.ObjectId;
} & {
    __v: number;
}), any, Account>, {}, {}, {}, {}, import("mongoose").DefaultSchemaOptions, Account, Document<unknown, {}, Account, {
    id: string;
}, import("mongoose").ResolveSchemaOptions<import("mongoose").DefaultSchemaOptions>> & Omit<Account & {
    _id: import("mongoose").Types.ObjectId;
} & {
    __v: number;
}, "id"> & {
    id: string;
}, {
    username?: import("mongoose").SchemaDefinitionProperty<string, Account, Document<unknown, {}, Account, {
        id: string;
    }, import("mongoose").ResolveSchemaOptions<import("mongoose").DefaultSchemaOptions>> & Omit<Account & {
        _id: import("mongoose").Types.ObjectId;
    } & {
        __v: number;
    }, "id"> & {
        id: string;
    }>;
    password?: import("mongoose").SchemaDefinitionProperty<string, Account, Document<unknown, {}, Account, {
        id: string;
    }, import("mongoose").ResolveSchemaOptions<import("mongoose").DefaultSchemaOptions>> & Omit<Account & {
        _id: import("mongoose").Types.ObjectId;
    } & {
        __v: number;
    }, "id"> & {
        id: string;
    }>;
    email?: import("mongoose").SchemaDefinitionProperty<string, Account, Document<unknown, {}, Account, {
        id: string;
    }, import("mongoose").ResolveSchemaOptions<import("mongoose").DefaultSchemaOptions>> & Omit<Account & {
        _id: import("mongoose").Types.ObjectId;
    } & {
        __v: number;
    }, "id"> & {
        id: string;
    }>;
    gameSettings?: import("mongoose").SchemaDefinitionProperty<GameSettings, Account, Document<unknown, {}, Account, {
        id: string;
    }, import("mongoose").ResolveSchemaOptions<import("mongoose").DefaultSchemaOptions>> & Omit<Account & {
        _id: import("mongoose").Types.ObjectId;
    } & {
        __v: number;
    }, "id"> & {
        id: string;
    }>;
}, Account>;
