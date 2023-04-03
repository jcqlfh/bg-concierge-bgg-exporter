import { initializeApp } from "@firebase/app";
import { getAuth, signInWithEmailAndPassword } from "@firebase/auth";
import { getFirestore, collection, where, updateDoc, getDocs, addDoc, query} from "@firebase/firestore";
import { MerkleJson } from 'merkle-json'
import * as fs from 'fs';
import dotenv from 'dotenv';

dotenv.config();

// Initialize Cloud Firestore through Firebase
const app = initializeApp(JSON.parse(process.env.FIREBASE_CONFIG));

// Initialize Cloud Firestore and get a reference to the service
const db = getFirestore(app);

let rawdata = fs.readFileSync('database.json');
let database = JSON.parse(rawdata);

const auth = getAuth();
signInWithEmailAndPassword(auth, process.env.FIREBASE_USER, process.env.FIREBASE_PASS)
  .then(async (userCredential) => {
    // Signed in
    const user = userCredential.user;
    const mj = new MerkleJson();

    const boardgamesRef = collection(db, "boardgames");


    while(database.length) {
    await new Promise((resolve) => database.splice(0, database.length < 10 ? database.length:10).forEach(async function(obj) {
      console.log("Checking if boardgame exists with Id:", obj.Id);
      const result = await getDocs(query(collection(db, "boardgames"), where('Id', '==', obj.Id)))
      if (result?.empty) {
        console.log("Boardgame does not exist. Creating...");
        const doc = await addDoc(collection(db, "boardgames"),{
          ...obj,
          merkleHash: mj.hash(obj)
        }).catch(function(error) {
          console.error("Error creating boardgame: ", error);
        })
      } else if(mj.hash(obj) !== mj.hash(result.docs[0].data(), true)) {
        console.log("Boardgame exists. Updating boardgame");
        const doc = await updateDoc(result.docs[0].ref,{
          ...obj,
          merkleHash: mj.hash(obj)
        }, {merge: false}).catch(function(error) {
          console.error("Error updating boardgame: ", error);
        });
        console.log("Boardgame updated");         
      } else {
        console.log("Boardgame exists but nothing has changed. Skipping...")
      }
      resolve();
    }));
  }
  })
  .catch((error) => {
    console.log(error);
  });