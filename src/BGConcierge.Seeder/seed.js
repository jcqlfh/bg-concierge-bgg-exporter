import { initializeApp, cert } from 'firebase-admin/app';
import { getFirestore } from 'firebase-admin/firestore';
import { MerkleJson } from 'merkle-json';
import * as fs from 'fs';

// Carregar service account do arquivo JSON
const serviceAccount = JSON.parse(fs.readFileSync('./serviceAccount.json', 'utf8'));
console.log(serviceAccount);
// Initialize Firebase Admin
const app = initializeApp({
  credential: cert(serviceAccount),
  // Adicionar project ID explicitamente se necessário
  projectId: serviceAccount.project_id
});

// Initialize Firestore
const db = getFirestore(app);

async function uploadBoardgames() {
  try {
    // Teste de conectividade
    console.log('🔧 Testando conectividade com Firestore...');
    const testDoc = await db.collection('_test').add({
      timestamp: new Date(),
      message: 'Connection test'
    });
    console.log('✅ Conectividade OK, removendo documento de teste...');
    await testDoc.delete();
    
    // Pegar nome do arquivo da linha de comando
    const databaseFile = process.argv[2] || 'database.json';
    
    if (!fs.existsSync(databaseFile)) {
      console.error(`❌ Arquivo '${databaseFile}' não encontrado!`);
      console.log('💡 Use: node script.js <nome-do-arquivo.json>');
      process.exit(1);
    }
    
    console.log(`📂 Carregando arquivo: ${databaseFile}`);
    let rawdata = fs.readFileSync(databaseFile);
    let database = JSON.parse(rawdata);
    
    // Debug: verificar estrutura do arquivo
    console.log(`📊 Total de items: ${database.length}`);
    console.log(`🔍 Primeira entrada:`, JSON.stringify(database[0], null, 2).substring(0, 300) + '...');
    console.log(`🔍 Estrutura das chaves:`, Object.keys(database[0] || {}));
    
    const mj = new MerkleJson();
    const batchSize = 10;

    console.log(`Total items to process: ${database.length}`);

    while (database.length > 0) {
      const batch = database.splice(0, Math.min(batchSize, database.length));
      
      // Process batch sequentially to avoid overwhelming Firestore
      for (const obj of batch) {
        try {
          console.log(`Checking boardgame ID: ${obj.Id}`);
          
          // Debug: verificar estrutura do objeto
          if (!obj.Id || obj.Id === undefined || obj.Id === null) {
            console.warn(`⚠️  Objeto sem ID válido:`, JSON.stringify(obj, null, 2));
            continue;
          }
          
          // Query existing document
          const querySnapshot = await db.collection('boardgames')
            .where('Id', '==', obj.Id)
            .limit(1)
            .get();

          if (querySnapshot.empty) {
            console.log("Creating new boardgame...");
            
            // Debug: verificar se obj tem dados válidos
            const sanitizedObj = {
              ...obj,
              // Garantir que campos obrigatórios existam
              Id: obj.Id || -1,
              Name: obj.Name || '',
              // Remover campos undefined/null que podem causar problemas
              ...(Object.fromEntries(
                Object.entries(obj).filter(([_, v]) => v !== undefined && v !== null)
              ))
            };
            
            console.log(`📝 Dados a serem salvos:`, JSON.stringify(sanitizedObj, null, 2).substring(0, 200) + '...');
            
            await db.collection('boardgames').add({
              ...sanitizedObj,
              merkleHash: mj.hash(sanitizedObj),
              createdAt: new Date(),
              updatedAt: new Date()
            });
            console.log("✅ Boardgame created");
          } else {
            const existingDoc = querySnapshot.docs[0];
            const existingData = existingDoc.data();
            
            if (mj.hash(obj) !== existingData.merkleHash) {
              console.log("Updating existing boardgame...");
              await existingDoc.ref.update({
                ...obj,
                merkleHash: mj.hash(obj),
                updatedAt: new Date()
              });
              console.log("✅ Boardgame updated");
            } else {
              console.log("⏭️  No changes detected, skipping");
            }
          }
        } catch (error) {
          console.error(`❌ Error processing boardgame ${obj.Id}:`, error.message);
          console.error(`📋 Objeto completo:`, JSON.stringify(obj, null, 2));
          console.error(`🔍 Stack trace:`, error.stack);
        }
        
        // Small delay to avoid rate limiting
        await new Promise(resolve => setTimeout(resolve, 100));
      }
      
      console.log(`Remaining items: ${database.length}`);
    }
    
    console.log("🎉 Upload completed!");
  } catch (error) {
    console.error("💥 Upload failed:", error);
  }
}

// Execute upload
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
