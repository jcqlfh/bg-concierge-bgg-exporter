import { initializeApp, cert } from 'firebase-admin/app';
import { getFirestore } from 'firebase-admin/firestore';
import { MerkleJson } from 'merkle-json';
import * as fs from 'fs';

const serviceAccount = JSON.parse(fs.readFileSync('./serviceAccount.json', 'utf8'));
const app = initializeApp({
  credential: cert(serviceAccount),
  projectId: serviceAccount.project_id
});

const db = getFirestore(app);
async function uploadBoardgames() {
  try {
    console.log('🔧 Testando conectividade com Firestore...');
    const testDoc = await db.collection('_test').add({
      timestamp: new Date(),
      message: 'Connection test'
    });
    console.log('✅ Conectividade OK, removendo documento de teste...');
    await testDoc.delete();
    
    const databaseFile = process.argv[2] || 'database.json';
    
    if (!fs.existsSync(databaseFile)) {
      console.error(`❌ Arquivo '${databaseFile}' não encontrado!`);
      console.log('💡 Use: node script.js <nome-do-arquivo.json>');
      process.exit(1);
    }
    
    console.log(`📂 Carregando arquivo: ${databaseFile}`);
    let rawdata = fs.readFileSync(databaseFile);
    let database = JSON.parse(rawdata);
    
    const mj = new MerkleJson();
    const BATCH_SIZE = 500;
    const MAX_CONCURRENT_BATCHES = 5;

    console.log(`Total items to process: ${database.length}`);

    const chunks = [];
    for (let i = 0; i < database.length; i += BATCH_SIZE) {
      chunks.push(database.slice(i, i + BATCH_SIZE));
    }

    async function processBatch(items) {
      const batch = db.batch();
      const operations = {
        boardgames: [],
        rankings: new Map()
      };

      for (const obj of items) {
        if (!obj || !obj.Id || obj.Id == "-1") continue;

        try {
          const sanitizedObj = {
            ...obj,
            ...(Object.fromEntries(
              Object.entries(obj).filter(([_, v]) => v !== undefined && v !== null)
            ))
          };

          const merkleHash = mj.hash(sanitizedObj);
          const docRef = db.collection('boardgames').doc(obj.Id.toString());

          batch.set(docRef, {
            ...sanitizedObj,
            merkleHash,
            updatedAt: new Date(),
            createdAt: new Date()
          }, { merge: true });

          operations.boardgames.push(obj.Id);

          if (obj.Statistics?.Ranks) {
            for (const rank of obj.Statistics.Ranks) {
              if (typeof rank.Value !== 'number' || !rank.Value || !rank.Name || rank.Value == "-1") {
                continue;
              }

              const collectionName = rank.Name + "list" ;

              const rankDoc = {
                gameId: obj.Id,
                position: rank.Value,
                gameRef: docRef,
                rankName: rank.Name,
                bayesAverage: rank.BayesAverage || null,
                updatedAt: new Date()
              };

              const rankRef = db.collection(collectionName).doc(obj.Id.toString());
              batch.set(rankRef, rankDoc, { merge: true });

              if (!operations.rankings.has(collectionName)) {
                operations.rankings.set(collectionName, 0);
              }
              operations.rankings.set(
                collectionName, 
                operations.rankings.get(collectionName) + 1
              );
            }
          }
        } catch (error) {
          console.error(`Error processing ${obj.Id}:`, error.message);
        }
      }

      try {
        await batch.commit();
        console.log('✅ Batch committed:', {
          boardgames: operations.boardgames.length,
          rankings: Object.fromEntries(operations.rankings)
        });
      } catch (error) {
        console.error('Batch commit failed:', error);
        throw error;
      }
    }

    for (let i = 0; i < chunks.length; i += MAX_CONCURRENT_BATCHES) {
      const batchPromises = chunks
        .slice(i, i + MAX_CONCURRENT_BATCHES)
        .map(chunk => processBatch(chunk));

      await Promise.all(batchPromises);
      
      console.log(`Progress: ${Math.min((i + MAX_CONCURRENT_BATCHES) * BATCH_SIZE, database.length)}/${database.length}`);
    }

    console.log("🎉 Upload completed!");
  } catch (error) {
    console.error("💥 Upload failed:", error);
  }
}

if (process.argv.length < 3) {
  console.log('📋 Como usar:');
  console.log('  node upload-firebase.js <arquivo-database.json>');
  console.log('');
  console.log('📝 Exemplos:');
  console.log('  node upload-firebase.js database.json');
  console.log('  node upload-firebase.js database-20000.json');
  console.log('  node upload-firebase.js ./data/boardgames.json');
  process.exit(0);
}

uploadBoardgames();
