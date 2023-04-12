using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Hexas;
using Misc;
using Characters;
using AI_Util;
using Classifiers;
using Classifiers1;
using Classifiers2;
using Stats;
using static MainGame;

namespace AI_Class {
	
///<summary>
///ActionAIPos class
///</summary>
public class ActionAIPos{
	public MainGame.ActionType action;
	public Point pos;
	
	public ActionAIPos(MainGame.ActionType action,Point pos){
		this.action = action;
		this.pos = pos;
	}
}

///<summary>
///AI class
///</summary>
public class AI{
	public static HexaGrid hexaGrid;
	
	public static List<ActionAIPos> decide(Character currentChar){
		List<ActionAIPos> sequence = new List<ActionAIPos>();
		sequence.Add(new ActionAIPos(MainGame.ActionType.SKIP,null));
		return sequence;
	}
}

///<summary>
///AI Easy class 
///</summary>
public class AIEasy : AI {

	///<summary>
	/// AI Easy behavior
	///</summary>
	///<param name = charID> Character controlled by the AI </param>
	///<returns> Return the action that the AI have to do </returns>
	new public static List<ActionAIPos> decide(Character currentChar){
		//Character currentChar = hexaGrid.charList[charID];
		List<ActionAIPos> sequence = new List<ActionAIPos>();
		
		switch (currentChar.charClass){
			case CharClass.GUERRIER :
			case CharClass.VOLEUR :
			case CharClass.ARCHER :
			case CharClass.MAGE : {
				int[] damage = AIUtil.calculateDamage(currentChar);
				List<Point> listH = hexaGrid.findAllPaths(currentChar.x,currentChar.y,currentChar.PM*currentChar.PA);
				if (listH != null && listH.Count > 0){
					// Find hexas where damage dealt is highest
					List<Point> bestHexas  = AIUtil.findHexasWhereValueIsMax(listH,damage);
					// Find hexas where position to lowest Enemy is lowest
					List<Point> bestHexas2 = AIUtil.findHexasClosestToLowestEnemy(currentChar,bestHexas);
					Point bestHexa = bestHexas2[0];
					// find path to hexa
					sequence = AIUtil.findSequencePathToHexa(currentChar,bestHexa.x,bestHexa.y);
					// Attack the Enemy
					int nbPA = currentChar.PA - sequence.Count;
					for (int i=0;i<nbPA;i++){
						Character cAttack = AIUtil.findCharToAttack2(currentChar);
						if ((cAttack != null)){
							sequence.Add(new ActionAIPos(MainGame.ActionType.ATK1,new Point(cAttack.x,cAttack.y)));
						}else if (bestHexa.x == currentChar.x && bestHexa.y == currentChar.y){
							sequence.Add(new ActionAIPos(MainGame.ActionType.SKIP,null));
						}
					}
				}else{
					sequence.Add(new ActionAIPos(MainGame.ActionType.SKIP,null));
				}
			} break;
			case CharClass.SOIGNEUR : {
				int[] healing = AIUtil.calculateHealing(currentChar);
				List<Point> listH = hexaGrid.findAllPaths(currentChar.x,currentChar.y,currentChar.PM*currentChar.PA);
				if (listH != null && listH.Count > 0){
					// Find hexas where healing done is highest
					List<Point> bestHexas  = AIUtil.findHexasWhereValueIsMax(listH,healing);
					// Find hexas where position to lowest Enemy is lowest
					List<Point> bestHexas2 = AIUtil.findHexasClosestToLowestEnemy(currentChar,bestHexas);
					Point bestHexa = bestHexas2[0];
					// find path to hexa
					sequence = AIUtil.findSequencePathToHexa(currentChar,bestHexa.x,bestHexa.y);
					int nbPA = currentChar.PA - sequence.Count;
					// Heal allies
					for (int i=0;i<nbPA;i++){
						Character cHeal = AIUtil.findCharToHeal(currentChar);
						if (cHeal != null){
							sequence.Add(new ActionAIPos(MainGame.ActionType.ATK1,new Point(cHeal.x,cHeal.y)));
						}else if (bestHexa.x == currentChar.x && bestHexa.y == currentChar.y){
							sequence.Add(new ActionAIPos(MainGame.ActionType.SKIP,null));
						}
					}
				}else{
					sequence.Add(new ActionAIPos(MainGame.ActionType.SKIP,null));
				}
			} break;
			// TO DO
			case CharClass.ENVOUTEUR : {
				int[] healing = AIUtil.calculateBuff(currentChar);
				List<Point> listH = hexaGrid.findAllPaths(currentChar.x,currentChar.y,currentChar.PM*currentChar.PA);
				if (listH != null && listH.Count > 0){
					// Find hexas where healing done is highest
					List<Point> bestHexas  = AIUtil.findHexasWhereValueIsMax(listH,healing);
					// Find hexas where position to lowest Enemy is lowest
					List<Point> bestHexas2 = AIUtil.findHexasClosestToLowestEnemy(currentChar,bestHexas);
					Point bestHexa = bestHexas2[0];
					// find path to hexa
					sequence = AIUtil.findSequencePathToHexa(currentChar,bestHexa.x,bestHexa.y);
					int nbPA = currentChar.PA - sequence.Count;
					// Buff allies
					for (int i=0;i<nbPA;i++){
						Character cBuff = AIUtil.findCharToBuff(currentChar);
						if (cBuff != null){
							sequence.Add(new ActionAIPos(MainGame.ActionType.ATK1,new Point(cBuff.x,cBuff.y)));
						}else if (bestHexa.x == currentChar.x && bestHexa.y == currentChar.y){
							sequence.Add(new ActionAIPos(MainGame.ActionType.SKIP,null));
						}
					}
				}else{
					sequence.Add(new ActionAIPos(MainGame.ActionType.SKIP,null));
				}
			} break;
		}
		
		return sequence;
	}
}

///<summary>
///AI Medium class 
///</summary>
// Doesnt do anything
public class AIMedium : AI{
	public static ClassifierSystem<Classifiers2.Classifier2> rules;
	
	///<summary>
	/// Selection of actions that the AI Medium can do, depending of the status of the game
	///</summary>
	///<param name = charID> Character controlled by the AI </param>
	///<param name = statsgame> Classifiers rules </param>
	///<returns> Return the action that the AI have to do </returns>
	public static List<ActionAIPos> decide(Character currentChar,StatsGame statsGame){
		//Character currentChar = hexaGrid.charList[charID];
		
		Classifier2 rule = null;
		
		// Get the current situation
		Classifier2 currentSituation = new Classifier2(hexaGrid,currentChar);
		// Find matching classifiers to the current situation in the database 
		List<Classifier2> matchingRules = rules.findMatchingClassifiers(currentSituation);
		// Add the classifier to the database if no match is found
		if (matchingRules.Count == 0){
			// Create a random classifier action
			do {
				currentSituation.action = Classifier2.getRandomAction(); //Classifier2.Action.Attack;
			} while (!currentSituation.isValid());
			matchingRules.Add(currentSituation);
			foreach (Classifier2 c in matchingRules) rules.Add(c);
			//Debug.Log("Pas trouvé : on ajoute " + matchingRules.Count + " règle(s)");
		}else{
			//Debug.Log("Trouvé " + matchingRules.Count + " règle(s)");
		}
		
		// Get one classifier from matching ones :
		if (matchingRules.Count == 0){
			//Debug.LogWarning("No valid classifier found !!");
			List<ActionAIPos> sequence = new List<ActionAIPos>();
			sequence.Add(new ActionAIPos(MainGame.ActionType.SKIP,null));
			return sequence;
		}else{
			if (AIHard.learn) rule = ClassifierList<Classifier2>.getClassifierRouletteWheel(matchingRules);
			else rule = ClassifierList<Classifier2>.getClassifierRouletteWheel(matchingRules);
			statsGame.addToRules(currentChar,rule);
			//Debug.Log("Medium AI action : " + rule.action);
		}
		
		// Convert the action from the classifier to an action that can be executed in game
		switch (rule.action){
			case Classifier2.Action.Attack : return AIUtil.AIHard2.doAttack(currentChar);
			case Classifier2.Action.Skill  : return AIUtil.AIHard2.doSkill(currentChar);
			case Classifier2.Action.Flee   : return AIUtil.AIHard2.doFlee(currentChar);
			default : { List<ActionAIPos> sequence = new List<ActionAIPos>(); sequence.Add(new ActionAIPos(MainGame.ActionType.SKIP,null)); return sequence; }
		}
	}
}

///<summary>
///AI Hard behavior
///</summary>
public class AIHard : AI{
	public static ClassifierSystem<Classifiers1.Classifier1> rules = null;
	public static bool learn = true;
	
	///<summary>
	/// Selection of actions that the AI hard can do, depending of the status of the game
	///</summary>
	///<param name = charID> Character controlled by the AI </param>
	///<param name = statsgame> Classifiers rules </param>
	///<returns> Return the action that the AI have to do </returns>
	public static List<ActionAIPos> decide(Character currentChar,StatsGame statsGame){
		//Character currentChar = hexaGrid.charList[charID];
		List<ActionAIPos> sequence = new List<ActionAIPos>();
		Classifier1 rule = null;
		// Get the current situation
		Classifier1 currentSituation = new Classifier1(hexaGrid,currentChar);
		// Find matching classifiers to the current situation in the database 
		
		// List<Classifier1> matchingRules = rules.findMatchingClassifiers(currentSituation);
		List<Classifier1> matchingRules = new List<Classifier1>();



		//Adds classifier to the database if no match is found
		//Creates a Base Strategy when there's no classifier found in the database, in order to initiate the database
		if (matchingRules.Count == 0){
			//INSERTS NEW RULE BASED ON THE STRATEGY BELOW. 
			switch(currentChar.charClass){
				case CharClass.ARCHER :
				case CharClass.GUERRIER :
				case CharClass.VOLEUR :
				case CharClass.MAGE : { //the AI is controlling an Archer/Guerrier/Voleur/Mage

				if (AIUtil.isCharLowHP(currentChar) == false){ //the AI approaches enemies when its HPs aren't low
					//Debug.Log("Do I have low HP ? " + AIUtil.isCharLowHP(currentChar));
					currentSituation.action = Classifier1.Action.ApproachEnemy;
					bool att = currentSituation.isInRangeToUseAttack();
					bool skl = currentSituation.isInRangeToUseSkill();
					bool rangeWarrior = currentSituation.isWarriorInRange();
					if(att || skl){ //it attacks enemies when it can
						currentSituation.action = Classifier1.Action.Attack;
						if(skl) currentSituation.action = Classifier1.Action.Skill;
					}
					if(rangeWarrior){
						currentSituation.action = Classifier1.Action.Flee;
					}
				}
				else if((AIUtil.isCharLowHP(currentChar) == true) && (AIUtil.areThereManyEnemies(currentChar) == true)) { 
					if((AIUtil.areThereAllies(currentChar)) == true){
						if(AIUtil.isThereAMage(currentChar) == true){ //the AI flees if there are allies alive, there is a Mage in the opponent team, its HPs are low and there are more than one enemy alive
							//Debug.Log("A mage is here ! :( ");
							currentSituation.action = Classifier1.Action.Flee;
							bool att = currentSituation.isInRangeToUseAttack();
							bool skl = currentSituation.isInRangeToUseSkill();
							if((att || skl) && AIUtil.canBeKilled(currentChar)){ //it attacks enemies only when they can be killed at the current turn
								currentSituation.action = Classifier1.Action.Attack;
								if (skl) currentSituation.action = Classifier1.Action.Skill;
							}
						}
						else{ //there isn't a Mage, the AI approaches its allies
						//Debug.Log("I got other team members and a mage is not here :) ");
						currentSituation.action = Classifier1.Action.ApproachAlly;
						bool att = currentSituation.isInRangeToUseAttack();
						bool skl = currentSituation.isInRangeToUseSkill();
						if((att || skl) && (AIUtil.canBeKilled(currentChar))){
							currentSituation.action = Classifier1.Action.Attack;
							if(skl) currentSituation.action = Classifier1.Action.Skill;
						}
						}
					}
					else { //the AI doesn't have teammates alive, it has to flee
						//Debug.Log("Don't have any team members, need to run " + AIUtil.areThereAllies(currentChar));
						currentSituation.action = Classifier1.Action.Flee;
						bool att = currentSituation.isInRangeToUseAttack();
						bool skl = currentSituation.isInRangeToUseSkill();
						if((att || skl) && AIUtil.canBeKilled(currentChar)){
							currentSituation.action = Classifier1.Action.Attack;
							if(skl) currentSituation.action = Classifier1.Action.Skill;
						}
					}
				}
				else if((AIUtil.isCharLowHP(currentChar) == true) && (AIUtil.areThereManyEnemies(currentChar) == false)) { //the AI approaches the only enemy remaining
					//Debug.Log("I can attack this guy, I don't need to escape");  
					currentSituation.action = Classifier1.Action.ApproachEnemy;
					bool att = currentSituation.isInRangeToUseAttack();
					bool skl = currentSituation.isInRangeToUseSkill();
					if(att || skl){
						currentSituation.action = Classifier1.Action.Attack;
						if (skl) currentSituation.action = Classifier1.Action.Skill;
					}
				}
			} break;
				case CharClass.SOIGNEUR :
				case CharClass.ENVOUTEUR : { //the AI is controlling a Soigneur/Envouteur
					if((AIUtil.areThereAllies(currentChar)) == true){
						if((AIUtil.isAllyOnlyCureChar(currentChar)) == true) { //the AI flees when it is controlling only Soigneurs and Envouteurs
							//Debug.Log("Allies are only able to cure or buff");
							currentSituation.action = Classifier1.Action.Flee;
						}
						else { //the AI follows the other characters when it is controlling a Soigneur or an Envouteur
							//Debug.Log("Allies can attack I dont need to flee");
							currentSituation.action = Classifier1.Action.ApproachAlly;
							bool att = currentSituation.isInRangeToUseAttack();
							bool skl = currentSituation.isInRangeToUseSkill();
							if(att || skl){
								currentSituation.action = Classifier1.Action.Attack;
								if(skl) if(Random.Range(0,1) == 0) currentSituation.action = Classifier1.Action.Skill;
							}
						}
						
					}
					else { //the AI flees when it is controlling a Soigneur or an Envouteur and there aren't allies alive on the field
						currentSituation.action = Classifier1.Action.Flee;
					}
				} break;
		    }


			matchingRules.Add(currentSituation);
			// foreach (Classifier1 c in matchingRules) rules.Add(c);
			//Debug.Log("Pas trouvé : on ajoute " + matchingRules.Count + " règle(s)");
		}else{
			//Debug.Log("Trouvé " + matchingRules.Count + " règle(s)");
		}
		
		List<Classifier1> validRules = new List<Classifier1>();
		// Check matching classifiers validity
		foreach (Classifier1 c in matchingRules){
			if (c.isValid(currentChar,hexaGrid)){
				validRules.Add(c);
			}
		}
		
		// Get one classifier from matching/valid ones :
		//Improvement needed here, needs to do something instead of just skipping the turn
		if (validRules.Count == 0){
			//Debug.LogWarning("Hmm that's not a valid Classifier");
			//Debug.Log(" " + validRules.Count);

			/*List <Point> listH = hexaGrid.findHexasInSight2(currentChar.x, currentChar.y, currentChar.PM);
			if (listH != null && listH.Count > 0){
				// Find hexas where threat is lowest
				//int[] threat = AIUtil.calculateThreat(charID);
				//for (int i=0;i<threat.Length;i++) threat[i] = - threat[i];
				List<Point> bestHexas  = AIUtil.findHexasWhereValueIsMax(listH,AIUtil.calculateDamage(charID));
				// Find hexas where position is lowest to enemy
				List <Point> bestHexasAlly = AIUtil.findHexasClosestToAllies(charID, bestHexas);
				Point bestHexa = bestHexasAlly[0];

			if (bestHexa.x == currentChar.x && bestHexa.y == currentChar.y) sequence.Add(new ActionAIPos(MainGame.ActionType.SKIP,null));
			else sequence.Add(new ActionAIPos(MainGame.ActionType.MOVE,bestHexa));
			}
			else sequence.Add(new ActionAIPos(MainGame.ActionType.SKIP, null));*/
			sequence.Add(new ActionAIPos(MainGame.ActionType.SKIP, null));
			return sequence;
		}else{
			//Debug.Log("How many validRules do I have in the list? " + validRules.Count);
			if (learn) rule = ClassifierList<Classifier1>.getClassifierRouletteWheel(validRules);
			else rule = ClassifierList<Classifier1>.getClassifierRouletteWheel(validRules);
			statsGame.addToRules(currentChar,rule);
			//Debug.Log("Hard AI action : " + rule.action);
		}
			
		// Converts the action from the classifier to an action that can be executed in game

		//IMPORTANT STUFF THERE

		switch (rule.action){
			case Classifier1.Action.ApproachEnemy : sequence.Add(AIUtil.AIHard.doApproachEnemy(currentChar)); break;
			case Classifier1.Action.ApproachAlly   : sequence.Add(AIUtil.AIHard.doApproachAlly(currentChar)); break;
			case Classifier1.Action.Flee           : sequence.Add(AIUtil.AIHard.doFlee(currentChar)); break;
			case Classifier1.Action.Attack         : sequence.Add(AIUtil.AIHard.doAttack(currentChar)); break;
			case Classifier1.Action.Skill          : sequence.Add(AIUtil.AIHard.doSkill(currentChar,0)); break;
			default : sequence.Add(new ActionAIPos(MainGame.ActionType.SKIP,null)); break;
		}
		
		return sequence;
	}
}
public class AICPU : AI
{

	///<summary>
	/// AI CPU, copied from AI Easy behavior
	///</summary>
	///<param name = charID> Character controlled by the AI </param>
	///<returns> Return the action that the AI have to do </returns>
	public static List<ActionAIPos> decideEasy(Character currentChar)
	{
		//Character currentChar = hexaGrid.charList[charID];
		List<ActionAIPos> sequence = new List<ActionAIPos>();

		switch (currentChar.charClass)
		{
			case CharClass.GUERRIER:
			case CharClass.VOLEUR:
			case CharClass.ARCHER:
			case CharClass.MAGE:
				{
					int[] damage = AIUtil.calculateDamage(currentChar);
					List<Point> listH = hexaGrid.findAllPaths(currentChar.x, currentChar.y, currentChar.PM * currentChar.PA);
					if (listH != null && listH.Count > 0)
					{
						// Find hexas where damage dealt is highest
						List<Point> bestHexas = AIUtil.findHexasWhereValueIsMax(listH, damage);
						// Find hexas where position to lowest Enemy is lowest
						List<Point> bestHexas2 = AIUtil.findHexasClosestToLowestEnemy(currentChar, bestHexas);
						Point bestHexa = bestHexas2[0];
						// find path to hexa
						sequence = AIUtil.findSequencePathToHexa(currentChar, bestHexa.x, bestHexa.y);
						// Attack the Enemy
						int nbPA = currentChar.PA - sequence.Count;
						for (int i = 0; i < nbPA; i++)
						{
							Character cAttack = AIUtil.findCharToAttack2(currentChar);
							if ((cAttack != null))
							{
								sequence.Add(new ActionAIPos(MainGame.ActionType.ATK1, new Point(cAttack.x, cAttack.y)));
							}
							else if (bestHexa.x == currentChar.x && bestHexa.y == currentChar.y)
							{
								sequence.Add(new ActionAIPos(MainGame.ActionType.SKIP, null));
							}
						}
					}
					else
					{
						sequence.Add(new ActionAIPos(MainGame.ActionType.SKIP, null));
					}
				}
				break;
			case CharClass.SOIGNEUR:
				{
					int[] healing = AIUtil.calculateHealing(currentChar);
					List<Point> listH = hexaGrid.findAllPaths(currentChar.x, currentChar.y, currentChar.PM * currentChar.PA);
					if (listH != null && listH.Count > 0)
					{
						// Find hexas where healing done is highest
						List<Point> bestHexas = AIUtil.findHexasWhereValueIsMax(listH, healing);
						// Find hexas where position to lowest Enemy is lowest
						List<Point> bestHexas2 = AIUtil.findHexasClosestToLowestEnemy(currentChar, bestHexas);
						Point bestHexa = bestHexas2[0];
						// find path to hexa
						sequence = AIUtil.findSequencePathToHexa(currentChar, bestHexa.x, bestHexa.y);
						int nbPA = currentChar.PA - sequence.Count;
						// Heal allies
						for (int i = 0; i < nbPA; i++)
						{
							Character cHeal = AIUtil.findCharToHeal(currentChar);
							if (cHeal != null)
							{
								sequence.Add(new ActionAIPos(MainGame.ActionType.ATK1, new Point(cHeal.x, cHeal.y)));
							}
							else if (bestHexa.x == currentChar.x && bestHexa.y == currentChar.y)
							{
								sequence.Add(new ActionAIPos(MainGame.ActionType.SKIP, null));
							}
						}
					}
					else
					{
						sequence.Add(new ActionAIPos(MainGame.ActionType.SKIP, null));
					}
				}
				break;
			// TO DO
			case CharClass.ENVOUTEUR:
				{
					int[] healing = AIUtil.calculateBuff(currentChar);
					List<Point> listH = hexaGrid.findAllPaths(currentChar.x, currentChar.y, currentChar.PM * currentChar.PA);
					if (listH != null && listH.Count > 0)
					{
						// Find hexas where healing done is highest
						List<Point> bestHexas = AIUtil.findHexasWhereValueIsMax(listH, healing);
						// Find hexas where position to lowest Enemy is lowest
						List<Point> bestHexas2 = AIUtil.findHexasClosestToLowestEnemy(currentChar, bestHexas);
						Point bestHexa = bestHexas2[0];
						// find path to hexa
						sequence = AIUtil.findSequencePathToHexa(currentChar, bestHexa.x, bestHexa.y);
						int nbPA = currentChar.PA - sequence.Count;
						// Buff allies
						for (int i = 0; i < nbPA; i++)
						{
							Character cBuff = AIUtil.findCharToBuff(currentChar);
							if (cBuff != null)
							{
								sequence.Add(new ActionAIPos(MainGame.ActionType.ATK1, new Point(cBuff.x, cBuff.y)));
							}
							else if (bestHexa.x == currentChar.x && bestHexa.y == currentChar.y)
							{
								sequence.Add(new ActionAIPos(MainGame.ActionType.SKIP, null));
							}
						}
					}
					else
					{
						sequence.Add(new ActionAIPos(MainGame.ActionType.SKIP, null));
					}
				}
				break;
		}

		return sequence;
	}
}

}
