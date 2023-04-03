import { initializeApp } from "@firebase/app";
import { getAuth, signInWithEmailAndPassword } from "@firebase/auth";
import { getFirestore, collection, addDoc, where, getDocs } from "@firebase/firestore";
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
  .then((userCredential) => {
    // Signed in
    const user = userCredential.user;

    database.forEach(function(obj) {
      addDoc(collection(db, "boardgames", obj.Id), {
        ...obj
      }).then(function(docRef) {
        console.log("Boardgame written with ID: ", docRef.id);

        const ranks = obj.Statistics?.Ranks ?? [];
        
        ranks.forEach(rank =>
          addDoc(collection(db, rank.Name, obj.Id), {
            ...rank
          }).then(function(docRef) {
            console.log("Rank written with ID: ", docRef.id);
          })
          .catch(function(error) {
            console.error("Error adding rank: ", error);
          })
        )
      })
      .catch(function(error) {
        console.error("Error adding boardgame: ", error);
      });
    });
    return;
  })
  .catch((error) => {
    const errorCode = error.code;
    const errorMessage = error.message;
    console.log(errorMessage);
  });