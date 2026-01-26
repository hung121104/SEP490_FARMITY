import { Document, Types } from 'mongoose';
export type CharacterDocument = Character & Document;
export declare class Character {
    worldId: string;
    accountId: Types.ObjectId;
    positionX: number;
    positionY: number;
    chunkIndex: number;
}
export declare const CharacterSchema: import("mongoose").Schema<Character, import("mongoose").Model<Character, any, any, any, (Document<unknown, any, Character, any, import("mongoose").DefaultSchemaOptions> & Character & {
    _id: Types.ObjectId;
} & {
    __v: number;
} & {
    id: string;
}) | (Document<unknown, any, Character, any, import("mongoose").DefaultSchemaOptions> & Character & {
    _id: Types.ObjectId;
} & {
    __v: number;
}), any, Character>, {}, {}, {}, {}, import("mongoose").DefaultSchemaOptions, Character, Document<unknown, {}, Character, {
    id: string;
}, import("mongoose").ResolveSchemaOptions<import("mongoose").DefaultSchemaOptions>> & Omit<Character & {
    _id: Types.ObjectId;
} & {
    __v: number;
}, "id"> & {
    id: string;
}, {
    worldId?: import("mongoose").SchemaDefinitionProperty<string, Character, Document<unknown, {}, Character, {
        id: string;
    }, import("mongoose").ResolveSchemaOptions<import("mongoose").DefaultSchemaOptions>> & Omit<Character & {
        _id: Types.ObjectId;
    } & {
        __v: number;
    }, "id"> & {
        id: string;
    }>;
    accountId?: import("mongoose").SchemaDefinitionProperty<Types.ObjectId, Character, Document<unknown, {}, Character, {
        id: string;
    }, import("mongoose").ResolveSchemaOptions<import("mongoose").DefaultSchemaOptions>> & Omit<Character & {
        _id: Types.ObjectId;
    } & {
        __v: number;
    }, "id"> & {
        id: string;
    }>;
    positionX?: import("mongoose").SchemaDefinitionProperty<number, Character, Document<unknown, {}, Character, {
        id: string;
    }, import("mongoose").ResolveSchemaOptions<import("mongoose").DefaultSchemaOptions>> & Omit<Character & {
        _id: Types.ObjectId;
    } & {
        __v: number;
    }, "id"> & {
        id: string;
    }>;
    positionY?: import("mongoose").SchemaDefinitionProperty<number, Character, Document<unknown, {}, Character, {
        id: string;
    }, import("mongoose").ResolveSchemaOptions<import("mongoose").DefaultSchemaOptions>> & Omit<Character & {
        _id: Types.ObjectId;
    } & {
        __v: number;
    }, "id"> & {
        id: string;
    }>;
    chunkIndex?: import("mongoose").SchemaDefinitionProperty<number, Character, Document<unknown, {}, Character, {
        id: string;
    }, import("mongoose").ResolveSchemaOptions<import("mongoose").DefaultSchemaOptions>> & Omit<Character & {
        _id: Types.ObjectId;
    } & {
        __v: number;
    }, "id"> & {
        id: string;
    }>;
}, Character>;
