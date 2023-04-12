using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.IO;
using UnityEngine;
using Hexas;
using Characters;
using AI_Util;
using System.Data;
using Mono.Data.Sqlite;
using System;
using System.Text;
using AI_Class;

namespace Classifiers {

abstract public class Classifier {
	public float fitness;
	public int useCount;
	public int lastUse;
	
	abstract public string getStringInfo();
	abstract public int distance(Classifier c);
	abstract public bool isSimilar(Classifier c);
	abstract public bool equals(Classifier c);
	abstract public bool equals2(Classifier c);
	abstract public bool isSimilar2(Classifier c);
	abstract public bool sameAction(Classifier c);
	abstract public Classifier copy();
	abstract public Classifier merge(Classifier c2);
	abstract public Classifier mutate(float likelihood,int maxAttempts);
	abstract public void saveInBinary(BinaryWriter writer);
	abstract public void loadInBinary(BinaryReader reader);
	abstract public void loadDB(IDataReader reader, IDataReader readerChar);
	abstract public void saveDB(IDbCommand dbcmd);
	
	/** -1 <= n <= 1 */
	public void addToFitness(float n){
		if (n > 0.0f && n <= 1.0f){
			fitness += (1.0f-fitness) * n;
		}else if (n < 0.0f && n >= -1.0f){
			fitness += fitness * n;
		}
	}
	
	/** converts the number into one between -1 and 1 */
	public void addToFitness2(float n){
		addToFitness((n >= 0) ? (1.0f-(1.0f/(1.0f+n*0.01f))) : -(1.0f-(1.0f/(1.0f-n*0.01f))));
	}
}

public class ClassifierList<C> where C : Classifier {
	/// <summary>
    /// get Classifier from list of Classifier with RouletteWheel Selection
	/// <param name="Classifiers">: the list of matching Classifiers
	/// <returns>  selected Classifier  </returns>  
    /// </summary>
	public static C getClassifierRouletteWheel(List<C> Classifiers){
		float total = 0.0f;
		foreach (C c in Classifiers) total += c.fitness;
		float r = UnityEngine.Random.Range(0.0f,total);
		float n = 0.0f;
		foreach (C c in Classifiers){
			if (r >= n && r <= c.fitness + n) return c;
			else n += c.fitness;
		}
		return null; 
	}
	
	/// <summary>
    /// get Classifier from list of Classifier with ReverseRouletteWheel Selection
	/// <param name="Classifiers">: the list of matching Classifiers
	/// <returns>  selected Classifier  </returns>  
    /// </summary>
	public static C getClassifierReverseRouletteWheel(List<C> Classifiers){
		float total = 0.0f;
		foreach (C c in Classifiers) total += (1.0f-c.fitness);
		float r = UnityEngine.Random.Range(0.0f,total);
		float n = 0.0f;
		foreach (C c in Classifiers){
			if (r >= n && r <= (1.0f-c.fitness) + n) return c;
			else n += (1.0f-c.fitness);
		}
		return null;
	}

		/// <summary>
		/// get Classifier from list of Classifier with Elitist Selection
		/// <param name="Classifiers">: the list of matching Classifiers
		/// <returns>  selected Classifier  </returns>  
		/// </summary>
		public static C getClassifierElististSelection(List<C> Classifiers){
			if (Classifiers.Count > 0){
				float maxFitness = Classifiers[0].fitness;
				List<C> maxClassifiers = new List<C>();
				// Find max value
				for (int i=1;i<Classifiers.Count;i++) if (Classifiers[i].fitness > maxFitness) maxFitness = Classifiers[i].fitness;
				// Get all Classifiers with max value
				for (int i=0;i<Classifiers.Count;i++) if (Classifiers[i].fitness == maxFitness) maxClassifiers.Add(Classifiers[i]);
				return getRandomClassifier(maxClassifiers);
			}else{
				return null; //need to understand that method too
			}

			//new code
			
			/*float moy = 0;
		
			for(int i = 0; i < Classifiers.Count; i++)
			{
				moy += Classifiers[i].fitness;
			}

			moy /= Classifiers.Count;

			List<C> elit = new List<C>();

			for(int i = 0; i < Classifiers.Count; i++)
			{
				if (Classifiers[i].fitness >= (moy + (moy / 25)))
				{
					elit.Add(Classifiers[i]);
				}
			}
			return getRandomClassifier(elit);*/
		}
		
		/// <summary>
		/// get Classifier from list of Classifier randomly
		/// <param name="Classifiers">: the list of matching Classifiers
		/// <returns>  selected Classifier  </returns>  
		/// </summary>
		public static C getRandomClassifier(List<C> Classifiers){
		return (Classifiers.Count > 0) ? Classifiers[UnityEngine.Random.Range(0,Classifiers.Count)] : null;
	}
	
	/// <summary>
    /// get Classifier from list of Classifier with the tournament selection
	/// <param name="Classifiers">: the list of matching Classifiers
	/// <returns>  selected Classifier  </returns>  
    /// </summary>
	public static C getClassifierTournamentSelection(List<C> Classifiers){
		/*if (Classifiers.Count > 0){
			if (Classifiers.Count==1 || Classifiers.Count==2){
				return getClassifierElististSelection(Classifiers);
			}else{
				C cl1=getRandomClassifier(Classifiers);
				C cl2=getRandomClassifier(Classifiers);
				int nb=0;
				while(cl2.action==cl1.action && nb <10){
					cl2=getRandomClassifier(Classifiers);
					nb++;
				}
				if (cl1.fitness>cl2.fitness) return cl1;
				else return cl2;
			}
		}else{
			return null;
		}*/
		return null;
	}
	
	private static void sort2(List<C> t,int debutT1,int debutT2,int endT2){
		for (int i=debutT1;i<debutT2 && debutT2 <= endT2;i++){
			if (t[i].fitness > t[debutT2].fitness){
				C temp = t[debutT2];
				for (int j=debutT2;j>i;j--){
					t[j] = t[j-1];
				}
				t[i] = temp;
				debutT2++;
			}
		}
	}

	public static void sortByFitness(List<C> t){
		int nb = t.Count;
		if (nb > 1){
			for (int i=0;i<nb;i+=2){
				if (i+1 < nb){
					if (t[i].fitness > t[i+1].fitness){
						C temp = t[i];
						t[i] = t[i+1];
						t[i+1] = temp;
					}
				}

			}

			for (int j=2;j<nb;j<<=1){
				for (int i=0;i<nb;i+=j<<1){
					if (i+j < nb){
						sort2(t,0+i,j+i,(((j<<1)-1)+i >= nb) ? nb-1 : ((j<<1)-1)+i); //(4-1) .. (7-1) .. (16-1)
					}
				}
			}
		}
	}
	
	private static void sort3(List<C> t,int debutT1,int debutT2,int endT2){
		for (int i=debutT1;i<debutT2 && debutT2 <= endT2;i++){
			if (t[i].fitness < t[debutT2].fitness){
				C temp = t[debutT2];
				for (int j=debutT2;j>i;j--){
					t[j] = t[j-1];
				}
				t[i] = temp;
				debutT2++;
			}
		}
	}

	public static void sortByFitness2(List<C> t){
		int nb = t.Count;
		if (nb > 1){
			for (int i=0;i<nb;i+=2){
				if (i+1 < nb){
					if (t[i].fitness < t[i+1].fitness){
						C temp = t[i];
						t[i] = t[i+1];
						t[i+1] = temp;
					}
				}

			}

			for (int j=2;j<nb;j<<=1){
				for (int i=0;i<nb;i+=j<<1){
					if (i+j < nb){
						sort3(t,0+i,j+i,(((j<<1)-1)+i >= nb) ? nb-1 : ((j<<1)-1)+i); //(4-1) .. (7-1) .. (16-1)
					}
				}
			}
		}
	}
}

public class ClassifierSystem<C> where C : Classifier {
	public List<C> classifiers;
	
	public ClassifierSystem(){
		classifiers = new List<C>();
	}
	
	public void Add(C classifier){
		classifiers.Add(classifier);
	}
	
	public bool exists(C c){
		foreach (C c2 in classifiers) if (c.equals(c2)) return true;
		return false;
	}
	
	public List<C> findMatchingClassifiers(C classifier){
		List<C> r = new List<C>();
		foreach (C c in classifiers){
			//put isSimilar2 back if problem
			//seems to work better
			if (c.equals2(classifier)){
				r.Add(c);
			}
		}
		return r;
	}
	
	/** 0 < likelihood < 1 (represents the chance of a mutation for one attribute)
		Returns the mutated classifier (or null). */
	public C mutateRandomClassifiers(float likelihood,int maxAttempts){
		C cRandom = ClassifierList<C>.getRandomClassifier(this.classifiers);
		if (cRandom != null){
			C cNew = (C) cRandom.mutate(likelihood,maxAttempts);
			if (cNew != null && !(this.exists(cNew))){
				this.Add(cNew);
				return cNew;
			}
		}
		return null;
	}
	
	/** Only classifiers with more than useCountThreshold will be removed.
		Only classifiers with less than fitnessThreshold will be removed.
		Returns the removed classifier (or null). */
	public C removeBadClassifier(int useCountThreshold,float fitnessThreshold){
		C cRandom = ClassifierList<C>.getClassifierReverseRouletteWheel(this.classifiers);
		if (cRandom != null){
			if (cRandom.useCount >= useCountThreshold && cRandom.fitness <= fitnessThreshold){
				this.classifiers.Remove(cRandom);
				return cRandom;
			}
		}
		return null;
	}
	
	/** Only classifiers with more than useCountThreshold will be merged.
		Only classifiers with more than fitnessThreshold will be merged.
		Returns the new classifier (or null). */
	public C mergeSimilarClassifiers(int useCountThreshold,float fitnessThreshold){
		C cRandom = ClassifierList<C>.getRandomClassifier(this.classifiers);
		if (cRandom != null){
			if (cRandom.useCount >= useCountThreshold && cRandom.fitness >= fitnessThreshold){
				for (int i=0;i<classifiers.Count;i++){
					if (cRandom != classifiers[i] && classifiers[i].useCount >= useCountThreshold && classifiers[i].fitness >= fitnessThreshold && classifiers[i].sameAction(cRandom) && cRandom.distance(classifiers[i]) <= 3){
						C cNew = (C) cRandom.merge(classifiers[i]);
						classifiers.Remove(classifiers[i]);
						classifiers.Remove(cRandom);
						this.Add(cNew);
						return cNew;
					}
				}
			}
		}
		return null;
	}
	
	public void increaseLastUse(){
		foreach (C c in classifiers){
			c.lastUse++;
		}
	}
	
	public string getStringInfo(){
		string str = "";
		for (int i=0;i<classifiers.Count;i++){
			str += i+1 + " :\n" + classifiers[i].getStringInfo() + "\n--------------------------------------------------------------------------------------------------------------\n";
		}
		return str;
	}
	
	public void dispInFile(string filePath){
		System.IO.File.WriteAllText(filePath,getStringInfo());
	}
	
	/** Creates a file that contains all the classifiers. */
	public void saveAllInBinary(string filePath){ //for IA medium
		using (BinaryWriter writer = new BinaryWriter(File.Open(filePath,FileMode.Create))){
			writer.Write(classifiers.Count);
			foreach (C c in classifiers){
				c.saveInBinary(writer);
			}
		}
	}

	public void saveAllInBinary(){ //for IA hard
		string conn = "URI=file:" + Application.streamingAssetsPath + "/Save/classifiers.db"; //Path to database.
		IDbConnection dbconn;
		dbconn = (IDbConnection) new SqliteConnection(conn);
        dbconn.Open();
		IDbCommand dbcmd = dbconn.CreateCommand();
		dbcmd.CommandText = "begin";
		dbcmd.ExecuteNonQuery();
		
		dbcmd.CommandText = "DELETE FROM rules"; //on vide la bdd pour qu'il n'y ait pas de doublons
		dbcmd.ExecuteNonQuery();
		dbcmd.CommandText = "DELETE FROM infoChar";
		dbcmd.ExecuteNonQuery();

		foreach (C c in classifiers){
			c.saveDB(dbcmd);
		}
		
		dbcmd.CommandText = "end";
		dbcmd.ExecuteNonQuery();
		
		
        dbconn.Close();
        dbconn = null;
	}
	
	public void loadAllInBinary(){ //for IA hard
		if(!(File.Exists( Application.streamingAssetsPath +"/Save/classifiers.db")))
		{
			Debug.Log("Ca VA tout créer");
			Mono.Data.Sqlite.SqliteConnection.CreateFile( Application.streamingAssetsPath +"/Save/classifiers.db");
			string conna = "URI=file:" + Application.streamingAssetsPath + "/Save/classifiers.db"; //Path to database.
			IDbConnection dbconna  = (IDbConnection) new SqliteConnection(conna);
			dbconna.Open(); //Open connection to the database
			IDbCommand dbcmda = dbconna.CreateCommand();
			dbcmda.CommandText = "BEGIN TRANSACTION";
			dbcmda.ExecuteNonQuery();					
			dbcmda.CommandText = "CREATE TABLE IF NOT EXISTS 'infoChar' ('class'	INTEGER,'hp'	INTEGER,	'threat'	INTEGER,'distance'	INTEGER, 'characterClass'	INTEGER ) ";
			dbcmda.ExecuteNonQuery();	
			dbcmda.CommandText = "CREATE TABLE IF NOT EXISTS 'rules' (	'id'	INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, 'class'	INTEGER,	'hp'	INTEGER,'pa'	INTEGER,'skillA'	INTEGER,'threat'	INTEGER,	'maxTargets'	INTEGER,	'countAllies'	INTEGER,	'countEnemies'	INTEGER,	'action'	INTEGER NOT NULL,	'fitness'	REAL NOT NULL,	'useCount'	INTEGER NOT NULL)";
			dbcmda.ExecuteNonQuery();
			dbcmda.CommandText = " COMMIT";
			dbcmda.ExecuteNonQuery();
			Debug.Log("Ca a tout créer");
		}

		string conn = "URI=file:" + Application.streamingAssetsPath + "/Save/classifiers.db"; //Path to database.
		IDbConnection dbconn  = (IDbConnection) new SqliteConnection(conn);
		dbconn.Open(); //Open connection to the database
		IDbCommand dbcmd = dbconn.CreateCommand();
		dbcmd.CommandText = "SELECT class, hp, pa, skillA, threat, maxTargets, countAllies, countEnemies, action, fitness, useCount FROM rules";
		IDataReader reader = dbcmd.ExecuteReader();
		
		while(reader.Read())
		{
			IDbCommand dbcmdChar = dbconn.CreateCommand();
			dbcmdChar.CommandText = "SELECT class, hp, threat, distance, characterClass FROM infoChar";
			IDataReader readerChar = dbcmdChar.ExecuteReader();

			C c = (C)FormatterServices.GetUninitializedObject(typeof(C)); // Allocate memory without calling a constructor.
			c.loadDB(reader, readerChar);
			classifiers.Add(c);

			readerChar.Close();
			readerChar = null;
		}

		Classifier mute = AIHard.rules.mutateRandomClassifiers(0.25f,10);
		if(mute != null)
			classifiers.Add((C)mute);
		
		Classifier bad = AIHard.rules.removeBadClassifier(5,0.33333f);

		reader.Close();
		reader= null;
		
		dbconn.Close();
		dbconn = null;

			/*
			if (File.Exists(filePath)){ to write in binari file
				using (BinaryReader readera = new BinaryReader(File.Open(filePath,FileMode.Open))){
				int nbClassifiers = readera.ReadInt32();
				for (int i=0;i<nbClassifiers;i++){
					C c = (C)FormatterServices.GetUninitializedObject(typeof(C)); // Allocate memory without calling a constructor.
					c.loadInBinary(readera);
					classifiers.Add(c);
				}
			}*/
	}
	public void loadAllInBinary(string filePath){ //for IA medium
		if (File.Exists(filePath)){
			using (BinaryReader reader = new BinaryReader(File.Open(filePath,FileMode.Open))){
				int nbClassifiers = reader.ReadInt32();

				for (int i=0;i<nbClassifiers;i++){
					C c = (C)FormatterServices.GetUninitializedObject(typeof(C)); // Allocate memory without calling a constructor.
					c.loadInBinary(reader);
					classifiers.Add(c);
				}
			}
		}
	}
	
}
}