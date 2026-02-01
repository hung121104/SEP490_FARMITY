import { connect, connection, Types } from 'mongoose';
import { CharacterSchema } from './microservices/player-data-service/src/character/character.schema';

async function seedDatabase() {
  try {
    await connect('mongodb://localhost:27017/game');
    console.log('Connected to MongoDB');

    const CharacterModel = connection.model('Character', CharacterSchema);

    const documents: any[] = [];
    const worldIds = ['world1', 'world2', 'world3', 'world4', 'world5'];

    for (let i = 0; i < 1000; i++) {
      const worldId = worldIds[Math.floor(Math.random() * worldIds.length)];
      const accountId = new Types.ObjectId(); // required by schema
      const positionX = Math.floor(Math.random() * 1000);
      const positionY = Math.floor(Math.random() * 1000);
      const chunkIndex = Math.floor(Math.random() * 100);

      documents.push({
        worldId,
        accountId,
        positionX,
        positionY,
        chunkIndex,
      });
    }

    // ordered: false lets insertMany continue past duplicate-key errors
    await CharacterModel.insertMany(documents, { ordered: false });
    //console.log(`Seeded ${documents.length} documents into characters collection`);

    await connection.close();
    console.log('Database connection closed');
  } catch (error) {
    console.error('Error seeding database:', error);
  }
}

seedDatabase();
