import { connect, connection } from 'mongoose';
import { Character, CharacterSchema } from './src/character/character.schema';

async function seedDatabase() {
  try {
    await connect('mongodb://localhost:27017/game');
    console.log('Connected to MongoDB');

    const CharacterModel = connection.model('Character', CharacterSchema);

    const documents = [];
    const worldIds = ['world1', 'world2', 'world3', 'world4', 'world5'];

    for (let i = 0; i < 1000; i++) {
      const worldId = worldIds[Math.floor(Math.random() * worldIds.length)];
      const playerID = `player${i}`;
      const positionX = Math.floor(Math.random() * 1000);
      const positionY = Math.floor(Math.random() * 1000);
      const chunkIndex = Math.floor(Math.random() * 100);

      documents.push({
        worldId,
        playerID,
        positionX,
        positionY,
        chunkIndex,
      });
    }

    await CharacterModel.insertMany(documents);
    console.log('Seeded 1000 documents into characters collection');

    await connection.close();
    console.log('Database connection closed');
  } catch (error) {
    console.error('Error seeding database:', error);
  }
}

seedDatabase();
