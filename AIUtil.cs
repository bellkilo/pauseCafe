using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Misc;
using Hexas;
using Characters;
using CharactersCPU;
using AI_Class;
using static UtilCPU.UtilCPU;
using static MainGame;

namespace AI_Util {


///<summary>
///HexaDamage class
///</summary>
public class HexaDamage{
	public int x;
	public int y;
	public int value;
	
	public HexaDamage(int x,int y,int value){
		this.x = x;
		this.y = y;
		this.value = value;
	}
}

///<summary>
///AIUtil class
///</summary>
public class AIUtil{
	public static HexaGrid hexaGrid;
	
	///<summary>
	/// Calculates how much damage can potentially be taken on each hexa.
	///</summary>
	///<param name = team> AI team </param>
	///<returns> Returns a list of w*h that contains the data. </returns>
	public static int[] calculateThreat(int team){
		int w_h = hexaGrid.w * hexaGrid.h;
		int[] finalList = new int[w_h];
		int[] list = new int[w_h];
		for (int i=0;i<w_h;i++){
			finalList[i] = 0;
			list[i] = 0;
		}
		
		foreach (Character c in hexaGrid.charList){
			if (c.team != team && c.charClass != CharClass.SOIGNEUR && c.charClass != CharClass.ENVOUTEUR){
				int damage = c.getClassData().basicAttack.effectValue;
				// 0 PM
				List<Point> listHexasInSight = hexaGrid.findHexasInSight2(c.x,c.y,c.getClassData().basicAttack.range);
				foreach (Point p in listHexasInSight){
					int pos = p.x + p.y*hexaGrid.w;
					if (list[pos] < damage*c.PA) list[pos] = damage*c.PA;
				}
				
				// 1+ PM
				for (int i=1;i<c.PA;i++){
					List<Point> listH = hexaGrid.findAllPaths(c.x,c.y,c.PM*i);
					foreach (Point charpos in listH){
						listHexasInSight = hexaGrid.findHexasInSight2(charpos.x,charpos.y,c.getClassData().basicAttack.range);
						foreach (Point p in listHexasInSight){
							int pos = p.x + p.y*hexaGrid.w;
							if (list[pos] < damage*(c.PA-i)) list[pos] = damage*(c.PA-i);
						}
					}
				}
				// Add to the list
				for (int i=0;i<w_h;i++){
					finalList[i] += list[i];
					list[i] = 0;
				}
			}
		}
		return finalList;
	}
	
	///<summary>
	/// Calculates how much damage can potentially be dealt on each hexa.
	///</summary>
	///<param name = currentChar> The current character controlled by the AI </param>
	///<returns> Returns a list of w*h that contains the data. </returns>
	public static int[] calculateDamage(Character currentChar){
		//Character currentChar = hexaGrid.charList[charID];
		int w_h = hexaGrid.w * hexaGrid.h;
		int[] list = new int[w_h];
		for (int i=0;i<w_h;i++){
			list[i] = 0;
		}
		
		int damage = currentChar.getClassData().basicAttack.effectValue;
		// 0 PM
		foreach (Character c in hexaGrid.charList){
			if (c.team != currentChar.team){
				if (hexaGrid.hexaInSight(currentChar.x,currentChar.y,c.x,c.y,currentChar.getClassData().basicAttack.range)){
					int value = damage*currentChar.PA;
					if (c.HP <= value) value += 10;
					int pos = currentChar.x + currentChar.y*hexaGrid.w;
					if (list[pos] < value) list[pos] = value;
				}
			}
		}
		// 1+ PM
		for (int i=1;i<currentChar.PA;i++){
			List<Point> listH = hexaGrid.findAllPaths(currentChar.x,currentChar.y,currentChar.PM*i);
			foreach (Point charpos in listH){
				foreach (Character c in hexaGrid.charList){
					if (c.team != currentChar.team){
						if (hexaGrid.hexaInSight(charpos.x,charpos.y,c.x,c.y,currentChar.getClassData().basicAttack.range)){
							int value = damage*(currentChar.PA-i);
							if (c.HP <= value) value += 10;
							int pos = charpos.x + charpos.y*hexaGrid.w;
							if (list[pos] < value) list[pos] = value;
						}
					}
				}
			}
		}
		
		return list;
	}

	///<summary>
	/// Calculates how much healing can potentially be done on each hexa
	/// assuming the character is a healer.
	///</summary>
	///<param name = currentChar> The current character controlled by the AI </param>
	///<returns> Returns a list of w*h that contains the data. </returns>	
	public static int[] calculateHealing(Character currentChar){
		//Character currentChar = hexaGrid.charList[charID];
		int w_h = hexaGrid.w * hexaGrid.h;
		int[] list = new int[w_h];
		for (int i=0;i<w_h;i++){
			list[i] = 0;
		}
		
		int healing = currentChar.getClassData().basicAttack.effectValue;
		// 0 PM
		foreach (Character c in hexaGrid.charList){
			if (c.team == currentChar.team){
				if (hexaGrid.hexaInSight(currentChar.x,currentChar.y,c.x,c.y,currentChar.getClassData().basicAttack.range)){
					int value = healing*currentChar.PA;
					if (c.HP+value > c.HPmax) value = c.HPmax-c.HP;
					int pos = currentChar.x + currentChar.y*hexaGrid.w;
					if (list[pos] < value) list[pos] = value;
				}
			}
		}
		// 1+ PM
		for (int i=1;i<currentChar.PA;i++){
			List<Point> listH = hexaGrid.findAllPaths(currentChar.x,currentChar.y,currentChar.PM*i);
			foreach (Point charpos in listH){
				foreach (Character c in hexaGrid.charList){
					if (c.team == currentChar.team){
						if (hexaGrid.hexaInSight(charpos.x,charpos.y,c.x,c.y,currentChar.getClassData().basicAttack.range)){
							int value = healing*(currentChar.PA-i);
							if (c.HP+value > c.HPmax) value = c.HPmax-c.HP;
							int pos = charpos.x + charpos.y*hexaGrid.w;
							if (list[pos] < value) list[pos] = value;
						}
					}
				}
			}
		}
		
		return list;
	}

	///<summary>
	/// Calculates how many PAs can potentially be given on each hexa
	///	assuming the character is a envouteur.
	///</summary>
	///<param name = currentChar> The current character controlled by the AI </param>
	///<returns> Returns a list of w*h that contains the data. </returns>	
	public static int[] calculateBuff(Character currentChar){
		//Character currentChar = hexaGrid.charList[charID];
		int w_h = hexaGrid.w * hexaGrid.h;
		int[] list = new int[w_h];
		for (int i=0;i<w_h;i++){
			list[i] = 0;
		}
		
		int healing = currentChar.getClassData().basicAttack.effectValue;
		// 0 PM
		foreach (Character c in hexaGrid.charList){
			if (c.team == currentChar.team){
				if (hexaGrid.hexaInSight(currentChar.x,currentChar.y,c.x,c.y,currentChar.getClassData().basicAttack.range)){
					int value;
					switch (c.charClass){
						case CharClass.GUERRIER  : value = 3; break;
						case CharClass.VOLEUR    : value = 6; break;
						case CharClass.ARCHER    : value = 4; break;
						case CharClass.MAGE      : value = 5; break;
						case CharClass.SOIGNEUR  : value = 2; break;
						case CharClass.ENVOUTEUR : value = 1; break;
						default : value = 0; break;
					}
					int pos = currentChar.x + currentChar.y*hexaGrid.w;
					if (list[pos] < value) list[pos] = value;
				}
			}
		}
		// 1+ PM
		for (int i=1;i<currentChar.PA;i++){
			List<Point> listH = hexaGrid.findAllPaths(currentChar.x,currentChar.y,currentChar.PM*i);
			foreach (Point charpos in listH){
				foreach (Character c in hexaGrid.charList){
					if (c.team == currentChar.team){
						if (hexaGrid.hexaInSight(charpos.x,charpos.y,c.x,c.y,currentChar.getClassData().basicAttack.range)){
							int value;
							switch (c.charClass){
								case CharClass.GUERRIER  : value = 3; break;
								case CharClass.VOLEUR    : value = 6; break;
								case CharClass.ARCHER    : value = 4; break;
								case CharClass.MAGE      : value = 5; break;
								case CharClass.SOIGNEUR  : value = 2; break;
								case CharClass.ENVOUTEUR : value = 1; break;
								default : value = 0; break;
							}
							int pos = charpos.x + charpos.y*hexaGrid.w;
							if (list[pos] < value) list[pos] = value;
						}
					}
				}
			}
		}
		
		return list;
	}
	///<summary>
	/// Searches the hexas where the value is at its maximum in the list of possible hexas.
	///</summary>
	///<param name = possibleHexas> The list of possible hexas </param>
	///<param name = v> An empty list  </param>
	///<returns> Returns a list of the position of the hexas where the value is at its maximum in the list of possible hexas. </returns>	
	public static List<Point> findHexasWhereValueIsMax(List<Point> possibleHexas,int[] v){
		// find best value
		int maxValue = v[possibleHexas[0].x+possibleHexas[0].y*hexaGrid.w];
		foreach (Point p in possibleHexas){
			int vmax = v[p.x+p.y*hexaGrid.w];
			if (vmax > maxValue) maxValue = vmax;
		}
		// find all hexas with best value
		List<Point> bestHexas = new List<Point>();
		foreach (Point p in possibleHexas){
			int vmax = v[p.x+p.y*hexaGrid.w];
			if (vmax == maxValue) bestHexas.Add(p);
		}
		return bestHexas;
	}
	
	///<summary>
	/// Searches the hexas where the least amount of potential damage will be taken in the list of possibles hexas.
	///</summary>
	///<param name = possibleHexas> The list of possible hexas  </param>
	///<returns> Returns the position of the hexas where the least amount of potential damage will be taken in the list of possible hexas. </returns>
	public static List<Point> findSafestHexas(List<Point> possibleHexas){
		return null; // TO DO
	}
	
	///<summary>
	/// Searches the hexas where the character will be closest to the lowest Enemy in the list of possible hexas.
	///</summary>
	///<param name = currentChar> The current character controlled by the AI </param>
	///<param name = possibleHexas> The list of possible hexas  </param>
	///<returns> Returns the position of the hexas where the character will be closest to the lowest Enemy in the list of possible hexas.</returns>
	public static List<Point> findHexasClosestToLowestEnemy(Character currentChar,List<Point> possibleHexas){
		//Character currentChar = hexaGrid.charList[charID];
		int minDistance = 100000;
		Character cLowest = AIUtil.findEnemy(currentChar);
		Debug.Log(cLowest.charClass.ToString());
		// find best value
		List<int> possibleHexasValues = new List<int>();
		foreach (Point p in possibleHexas){
			int d = hexaGrid.getWalkingDistance(p.x,p.y,cLowest.x,cLowest.y);
			possibleHexasValues.Add(d);
			if (d != -1) if (d < minDistance) minDistance = d;
		}
		// find all hexas with best value
		List<Point> bestHexas = new List<Point>();
		for (int i=0;i<possibleHexas.Count;i++) if (possibleHexasValues[i] == minDistance) bestHexas.Add(possibleHexas[i]);
		return bestHexas;
	}

	//test method
	/*
	public static List <Point> findHexasClosestToEnemyAlphaTest(int charID, Character enemyChar, List <Point> possibleHexas){
		Character currentChar = hexaGrid.charList[charID];
		int minDistance = 100000;
		List <int> possibleHexasValues = new List <int> ();
		switch(currentChar.charClass){
			case CharClass.ARCHER:
			case CharClass.GUERRIER:
			case CharClass.MAGE:{
				foreach(Point p in possibleHexas){
					int distance = hexaGrid.getWalkingDistance(p.x, p.y, enemyChar.x, enemyChar.y);
					possibleHexasValues.Add(distance);
					if (distance != -1) if(distance < minDistance) minDistance = distance;
				}
			} break;
			case CharClass.VOLEUR:{
				if(enemyChar.charClass != CharClass.GUERRIER){
					foreach(Point p in possibleHexas){
						int distance = hexaGrid.getWalkingDistance(p.x, p.y, enemyChar.x, enemyChar.y);
						possibleHexasValues.Add(distance);
						if (distance != -1) if(distance < minDistance) minDistance = distance;
					}
				} break;
			}
		}
		List <Point> bestHexas = new List <Point> ();
		for(int i = 0; i < possibleHexas.Count; i++){
			if(possibleHexasValues[i] == minDistance){
				bestHexas.Add(possibleHexas[i]);
			}
		}
		return bestHexas;
	} */

	///<summary>
	/// Searches the hexas where the character will be closest to allies in the list of possible hexas.
	///</summary>
	///<param name = currentChar> The current character controlled by the AI </param>
	///<param name = possibleHexas> The list of possible hexas </param>
	///<returns> Returns the position of the hexas where the character will be closest to allies in the list of possible hexas.</returns>
	public static List<Point> findHexasClosestToAllies(Character currentChar,List<Point> possibleHexas){
		//Character currentChar = hexaGrid.charList[charID];
		Point charPos = new Point(currentChar.x,currentChar.y);
		List<Point> bestHexas = new List<Point>();
		int closest = 100000;
		// SOIGNEUR
		//Yeah this definitely doesn't work at all
		//I leave this as a comment now but I'll come back to that in another moment

		/* if (currentChar.charClass == CharClass.SOIGNEUR){
			List<int> possibleHexasValues = new List<int>();
			foreach (Point p in possibleHexas){
				currentChar.updatePos2(p.x,p.y,hexaGrid);
				Character cLowest = findCharToHeal(charID);
				if (cLowest != null){
					possibleHexasValues.Add(cLowest.HP);
					if (cLowest.HP < closest) closest = cLowest.HP;
				}else{
					possibleHexasValues.Add(100000);
				}
			}
			for (int i=0;i<possibleHexas.Count;i++) if (possibleHexasValues[i] == closest) bestHexas.Add(possibleHexas[i]);
			currentChar.updatePos2(charPos.x,charPos.y,hexaGrid);
			return bestHexas;
		// OTHERS
		}else{*/
			List<int> possibleHexasValues = new List<int>();
			foreach (Point p in possibleHexas){
				currentChar.updatePos2(p.x,p.y,hexaGrid);
				int distance = 0;
				foreach (Character c in hexaGrid.charList){
					if (c.team == currentChar.team && c != currentChar) distance += hexaGrid.getWalkingDistance(p.x,p.y,c.x,c.y);
				}
				possibleHexasValues.Add(distance);
				if (distance < closest) closest = distance;
			}
			for (int i=0;i<possibleHexas.Count;i++) if (possibleHexasValues[i] == closest) bestHexas.Add(possibleHexas[i]);
			currentChar.updatePos2(charPos.x,charPos.y,hexaGrid);
			return bestHexas;
		//}
	}

	///<summary>
	///Finds the hexas where the character will be the more distant from the enemy
	///</summary>
	///<param name = currentChar> The current character controlled by the AI </param>
	///<param name = possibleHexas> The list of possible hexas </param>
	///<returns> Returns the hexas where the character will be the more distant from the enemy.</returns>
	public static List <Point> findHexasToFlee(Character currentChar, List <Point> possibleHexas){
		//Character currentChar = hexaGrid.charList[charID];
		Point charPos = new Point(currentChar.x, currentChar.y);
		List <Point> bestHexas = new List <Point> ();
		int maxDistance = 0;

		List <int> possibleHexasValues = new List <int> ();
		foreach(Point p in possibleHexas){
			currentChar.updatePos2(p.x, p.y, hexaGrid);
			int distance = 0;
			foreach(Character c in hexaGrid.charList){
				if(c.team != currentChar.team){
					distance += hexaGrid.getWalkingDistance(p.x, p.y, c.x, c.y);
				}
			}
			possibleHexasValues.Add(distance);
			if(distance > maxDistance) maxDistance = distance;
			
		}
		//find all hexas with best value
		for(int i = 0; i < possibleHexas.Count; i++){
			
			//adds only the highest distance
			if(possibleHexasValues[i] == maxDistance){
				bestHexas.Add(possibleHexas[i]);  //adds the value only if it's not null
			}
		}
		currentChar.updatePos2(charPos.x, charPos.y, hexaGrid);
		return bestHexas;
	}

	///<summary>
	///Here a function to substitute findHexasClosestToLowestEnemy, that changes for every class. 
	///This serves as a test function for now to find a better way to chase enemies. 
	///But it could be used in the future. 
	///</summary>
	///<param name = currentChar> The current character controlled by the AI </param>
	///<param name = possibleHexas> The list of possible hexas </param>
	///<returns> Returns the hexas where the character can approach the enemy</returns>
	public static List <Point> findHexasToApproachEnemy(Character currentChar, List <Point> possibleHexas){
		//Character currentChar = hexaGrid.charList[charID];

		Point charPos = new Point(currentChar.x, currentChar.y);

		List <int> possibleHexasValues = new List<int>();
		List<Point> bestHexas = new List<Point>();

		//minimum distance from the enemy
		int minDistance = 100000;

		//for every class there's a specific way of reaching the enemy. For now the only difference will be for the thief. 
		switch(currentChar.charClass){
			case CharClass.ARCHER:
			case CharClass.GUERRIER:
			case CharClass.MAGE:
			case CharClass.SOIGNEUR:
			case CharClass.ENVOUTEUR:
			{
				Character cTarget = AIUtil.findEnemy(currentChar);
				foreach(Point p in possibleHexas){
					int distance = hexaGrid.getWalkingDistance(p.x, p.y, cTarget.x, cTarget.y);
					possibleHexasValues.Add(distance);
					if(distance != -1) if(distance < minDistance) minDistance = distance;

				}
				for(int i = 0; i < possibleHexas.Count; i++){
			
					//adds only the minimum distance
					if(possibleHexasValues[i] == minDistance){
					bestHexas.Add(possibleHexas[i]);  
					}	
				}
			} break;
			case CharClass.VOLEUR:
			{
				Character cTarget = AIUtil.findEnemyVoleur(currentChar);
				foreach(Point p in possibleHexas){
					int distance = hexaGrid.getWalkingDistance(p.x, p.y, cTarget.x, cTarget.y);
					possibleHexasValues.Add(distance);
					if(distance != -1) if(distance < minDistance) minDistance = distance;
				}
				for(int i = 0; i < possibleHexas.Count; i++){
			
					//adds only the minimum distance
					if(possibleHexasValues[i] == minDistance){
					bestHexas.Add(possibleHexas[i]);  
					}	
				}

			} break;
		}
		return bestHexas;

	}

	///<summary>
	/// Searches the path to reach a specific hexa.
	///</summary>
	///<param name = currentChar> The current character controlled by the AI </param>
	///<param name = x>  </param>
	///<param name = y>  </param>
	///<returns> Returns the path that the character have to follow to reach their destination.</returns>
	public static List<ActionAIPos> findSequencePathToHexa(Character currentChar,int x,int y){
		//Character currentChar = hexaGrid.charList[charID];
		List<ActionAIPos> sequence = new List<ActionAIPos>();
		int nbPA = currentChar.PA;

        if (x == currentChar.x && y == currentChar.y){
			
		}else{
			int d = hexaGrid.getWalkingDistance(currentChar.x,currentChar.y,x,y);
			List<Point> shortestPath = hexaGrid.findShortestPath(currentChar.x,currentChar.y,x,y,d);
			for (int i=0;i<d && nbPA > 0;i+=currentChar.PM){
                //Debug.Log(charToString(currentChar));
				Point destination = shortestPath[((i+currentChar.PM) <= d) ? (i+currentChar.PM) : d];
				sequence.Add(new ActionAIPos(MainGame.ActionType.MOVE,new Point(destination.x,destination.y)));
				nbPA--;
			}
		}

        return sequence;
	}

	///<summary>
	/// Searches the enemy with the lowest HP.
	///</summary>
	///<param name = c> An enemy character </param>
	///<returns> Returns the Enemy with the lowest amount of HP.</returns>
	public static Character findEnemy(Character c){
		int lowest = 100000;
		Character cLowest = null;
		switch(c.charClass){
			case CharClass.ARCHER :
			case CharClass.GUERRIER : 
			case CharClass.MAGE :
			case CharClass.VOLEUR :
			{
				foreach (Character cEnemy in hexaGrid.charList){
					if (cEnemy.team != c.team && cEnemy.HP < lowest){
					lowest = c.HP;
					cLowest = cEnemy;
					}
				}
			} break;
			case CharClass.SOIGNEUR :
			case CharClass.ENVOUTEUR : {
				foreach (Character cEnemy in hexaGrid.charList){
					if (cEnemy.team != c.team && cEnemy.HP < lowest){
					lowest = c.HP;
					cLowest = cEnemy;
					}
				}
			} break;
		}
		return cLowest;
	}

	///<summary>
	/// Strategy of approach for thiefs. doesnt target warriors, only targets them when there's only one remaining.  
	///</summary>
	///<param name = currentChar> The current character controlled by the AI </param>
	///<returns> Returns the thief's target </returns>
	public static Character findEnemyVoleur(Character currentChar){
		Character characterToAttack = null;
		foreach(Character c in hexaGrid.charList){
			if(c.team != currentChar.team){
				if((c.charClass != CharClass.GUERRIER) || areThereManyEnemies(currentChar) == false){
					characterToAttack = c;
				}
			}
		}
		return characterToAttack;
	}


	///<summary>
	///Detects if our character has low HP and needs to be saved
	///</summary>
	///<param name = currentChar> The current character controlled by the AI </param>
	///<returns> Returns true or false </returns>
	public static bool isCharLowHP(Character currentChar){
		//Character currentChar = hexaGrid.charList[myCharID];
		int lowest = 4;
		foreach(Character c in hexaGrid.charList){
			if((c.team == currentChar.team) && (c == currentChar) && (c.HP < lowest)){
				return true;
			}
		}

		return false;

	}


	///<summary>
	///Verifies if allies of the current controlled character are present on the field or not
	///</summary>
	///<param name = currentChar> The current character controlled by the AI </param>
	///<returns> Returns true or false </returns>
	public static bool areThereAllies(Character currentChar){
		//Character currentChar = hexaGrid.charList[myCharID];
		int numberAllies = 0;
		foreach(Character c in hexaGrid.charList){
			if((c.team == currentChar.team)){
				numberAllies++;
			}
		}
		if(numberAllies > 1){
			return true;
		}
		else return false;
	}

	///<summary>
	///Verifies if there are more than one enemy still alive on the field
	///</summary>
	///<param name = currentChar> The current character controlled by the AI </param>
	///<returns> Returns true or false </returns>
	public static bool areThereManyEnemies(Character currentChar){
		int numberEnemies = 0;
		foreach(Character c in hexaGrid.charList){
			if((c.team != currentChar.team)){
				numberEnemies++;
			}
		}
		if(numberEnemies > 1){
			return true;
		}
		else return false;
	}

	///<summary>
	///Checks if there are only allies able to cure on the field (Soigneurs or Envouteurs)
	///</summary>
	///<param name = currentChar> The current character controlled by the AI </param>
	///<returns> Returns true or false </returns>
	public static bool isAllyOnlyCureChar(Character currentChar){
		//Character currentChar = hexaGrid.charList[myCharID];
		int nbCureChar = 0;
		int nbAttackChar = 0;
		foreach(Character c in hexaGrid.charList){
			if((c.team == currentChar.team)){
				switch(c.charClass){
					case CharClass.ARCHER :
					case CharClass.GUERRIER :
					case CharClass.VOLEUR :
					case CharClass.MAGE : {
						nbAttackChar++;
					} break;
					case CharClass.SOIGNEUR :
					case CharClass.ENVOUTEUR : {
						nbCureChar++;
					} break;
				}
			}
		}
		if((nbAttackChar == 0) && (nbCureChar >= 1)){
			return true;
		}
		else return false;
	}

	///<summary>
	///Checks if there is a Mage on the field 
	///</summary>
	///<param name = currentChar> The current character controlled by the AI </param>
	///<returns> Returns true or false </returns>
	public static bool isThereAMage(Character currentChar){
		//Character currentChar = hexaGrid.charList[myCharID];
		int nbMageChar = 0;
		foreach(Character c in hexaGrid.charList){
			if((c.team != currentChar.team) && (c.HP != 0)){
				switch(c.charClass){
					case CharClass.MAGE : {
						nbMageChar++;
					} break;
				}
			}
		}
		if(nbMageChar > 0){
			return true;
		}
		else return false;
	}

	///<summary>
	///Checks if there is a Healer on the field 
	///</summary>
	///<param name = currentChar> The current character controlled by the AI </param>
	///<returns> Returns true or false </returns>
	public static bool isThereAHealer(int myCharID)
	{
		Character currentChar = hexaGrid.charList[myCharID];
		int nbHealerChar = 0;
		foreach (Character c in hexaGrid.charList)
		{
			if ((c.team != currentChar.team))
			{
				switch (c.charClass)
				{
					case CharClass.SOIGNEUR:
						{
							nbHealerChar++;
						}
						break;
				}
			}
		}
		if (nbHealerChar > 0)
		{
			return true;
		}
		else return false;
	}

	///<summary>
	///Checks if there is an Archer on the field 
	///</summary>
	///<param name = currentChar> The current character controlled by the AI </param>
	///<returns> Returns true or false </returns>
	public static bool isThereAArcher(int myCharID)
	{
		Character currentChar = hexaGrid.charList[myCharID];
		int nbArcherChar = 0;
		foreach (Character c in hexaGrid.charList)
		{
			if ((c.team != currentChar.team))
			{
				switch (c.charClass)
				{
					case CharClass.ARCHER:
						{
							nbArcherChar++;
						}
						break;
			}

			}
		}
		if (nbArcherChar > 0)
		{
			return true;
		}
		else return false;
	}

	///<summary>
	///Checks if there is a Warrior on the field 
	///</summary>
	///<param name = currentChar> The current character controlled by the AI </param>
	///<returns> Returns true or false </returns>
	public static bool isThereAWarrior(int myCharID)
	{
		Character currentChar = hexaGrid.charList[myCharID];
		int nbWarriorChar = 0;
		foreach (Character c in hexaGrid.charList)
		{
			if ((c.team != currentChar.team))
			{
				switch (c.charClass)
				{
					case CharClass.GUERRIER:
						{
							nbWarriorChar++;
						}
						break;
				}
			}
		}
		if (nbWarriorChar > 0)
		{
			return true;
		}
		else return false;
	}

	///<summary>
	///Find the position of a specific enemy 
	///</summary>
	///<param name = currentChar> The current character controlled by the AI </param>
	///<returns> Returns the position </returns>
	public static Point findEnemyPosition(int myCharID, CharClass enemyClass){
		Character currentChar = hexaGrid.charList[myCharID];
		Point position = null;
		foreach (Character c in hexaGrid.charList){
			if ((c.team != currentChar.team) && (c.charClass == enemyClass)){
				position = new Point(c.x, c.y);	
			}
		}
		return position;
	}

	///<summary>
	/// Searches the enemy that either will be killed or get the lowest hp after being attacked from the current character's position.
	///</summary>
	///<param name = currentChar> The current character controlled by the AI </param>
	///<returns> Returns the ID of the Enemy that either will be killed or be lowest after being attacked from the current characters's position.</returns>
	public static Character findCharToAttack(Character currentChar){ //yoo try to change that thing for the thief
		//Character currentChar = hexaGrid.charList[myCharID];
		int lowest = 100000;
		Character cLowest = null;
		foreach (Character c in hexaGrid.charList){
			if (c.team != currentChar.team){
				if (hexaGrid.hexaInSight(currentChar.x,currentChar.y,c.x,c.y,currentChar.getClassData().basicAttack.range)){
					if (c.HP < lowest){
						lowest = c.HP;
						cLowest = c;
					}
				}
			}
		}
		return cLowest;
	}

	///<summary>
	/// An other version of the previous function.
	/// Searches the enemy that either will be killed or get the lowest hp after being attacked from the current character's position.
	///</summary>
	///<param name = currentChar> The current character controlled by the AI </param>
	///<returns> Returns the ID of the Enemy that either will be killed or be lowest after being attacked from the current characters's position.</returns>
	public static Character findCharToAttack2(Character currentChar){
		//Character currentChar = hexaGrid.charList[myCharID];
		int lowest = 100000;
		Character cAttack = null;
		switch(currentChar.charClass){
			case CharClass.ARCHER:
			case CharClass.GUERRIER:
			case CharClass.MAGE: {
				foreach(Character c in hexaGrid.charList){
					if(c.team != currentChar.team){
						if(hexaGrid.hexaInSight(currentChar.x, currentChar.y, c.x, c.y, currentChar.getClassData().basicAttack.range)){
							if(c.HP < lowest){
								lowest = c.HP;
								cAttack = c;
							}
						}
					}
				}
			} break;
			case CharClass.VOLEUR : {
				foreach(Character c in hexaGrid.charList){
					if((c.team != currentChar.team) && (c.charClass != CharClass.GUERRIER)){
						if(hexaGrid.hexaInSight(currentChar.x, currentChar.y, c.x, c.y, currentChar.getClassData().basicAttack.range)){
							if(c.HP < lowest){
								lowest = c.HP; 
								cAttack = c; 
							}
						}
					}
				}
			} break;
		}
		return cAttack;
	}

	///<summary>
	/// Searches an isolated character on the field.
	///</summary>
	///<param name = myCharID> </param>
	///<returns> Returns the position of the isolated character. </returns>
	public static Character findIsolatedChar(int myCharID){
		Character currentChar = hexaGrid.charList[myCharID];
		Character cIsolated = null;

		foreach(Character c in hexaGrid.charList){
			if(c.team != currentChar.team){
				if(hexaGrid.hexaInSight(c.x, c.y, 0, 0, currentChar.getClassData().basicAttack.range)){
					cIsolated = c;
				}
			}
		}
		return cIsolated;
	}



	///<summary>
	/// Searches the enemy that either will be killed or get the lowest hp after being attacked with a skill from the current character's position.
	///</summary>
	///<param name = currentChar> The current character controlled by the AI </param>
	///<returns> Returns the ID of the Enemy that either will be killed or be lowest after being attacked with skill from the current character's position.</returns>
	public static Character findCharToAttackSkill(Character currentChar){
		//Character currentChar = hexaGrid.charList[myCharID];
		int lowest = 100000;
		Character cLowest = null;
		foreach (Character c in hexaGrid.charList){
			if (c.team != currentChar.team){
				if (hexaGrid.hexaInSight(currentChar.x,currentChar.y,c.x,c.y,currentChar.getClassData().skill_1.range)){
					if (c.HP < lowest){
						lowest = c.HP;
						cLowest = c;
					}
				}
			}
		}
		return cLowest;
	}

	///<summary>
	/// An other version of the previous function.
	/// Searches the enemy that either will be killed or get the lowest hp after being attacked with a skill from the current character's position.
	///</summary>
	///<param name = currentChar> The current character controlled by the AI </param>
	///<returns> Returns the ID of the Enemy that either will be killed or be lowest after being attacked with skill from the current character's position.</returns>
	public static Character findCharToAttackSkill2(int myCharID){
		Character currentChar = hexaGrid.charList[myCharID];
		int lowest = 100000;
		Character cAttack = null;
		switch(currentChar.charClass){
			case CharClass.ARCHER:
			case CharClass.GUERRIER:
			case CharClass.MAGE: {
				foreach(Character c in hexaGrid.charList){
					if(c.team != currentChar.team){
						if(hexaGrid.hexaInSight(currentChar.x, currentChar.y, c.x, c.y, currentChar.getClassData().skill_1.range)){
							if(c.HP < lowest){
								lowest = c.HP;
								cAttack = c;
							}
						}
					}
				}
			} break;
			case CharClass.VOLEUR : {
				foreach(Character c in hexaGrid.charList){
					if((c.team != currentChar.team) && (c.charClass != CharClass.GUERRIER)){
						if(hexaGrid.hexaInSight(currentChar.x, currentChar.y, c.x, c.y, currentChar.getClassData().skill_1.range)){
							if(c.HP < lowest){
								lowest = c.HP; 
								cAttack = c; 
							}
						}
					}
				}
			} break;
		}
		return cAttack;
	}

	///<summary>
	/// Searches the hexa where the mage can hit the highest amount of Enemies from their current position.
	///</summary>
	///<param name = currentChar> The current character controlled by the AI </param>
	///<returns> Returns the position of the hexa that either will allow the mage to hit the highest amount of Enemies from the current position. </returns>
	public static Point findWhereToAttackMage(Character currentChar){
		int maxTargets = 0;
		//Character currentChar = hexaGrid.charList[myCharID];
		List<int> possibleHexasValues = new List<int>();
		List<Point> possibleHexas = hexaGrid.findHexasInSight2(currentChar.x,currentChar.y,currentChar.getClassData().basicAttack.range);
		foreach (Point p in possibleHexas){
			List<Character> lc = hexaGrid.getCharWithinRange(p.x,p.y,currentChar.getClassData().basicAttack.rangeAoE);
			int nb = 0;
			// filter allies
			foreach (Character c in lc){
				if (c.team != currentChar.team) nb++;
			}
			possibleHexasValues.Add(nb);
			if (nb > maxTargets) maxTargets = nb;
		}
		List<Point> bestHexas = new List<Point>();
		for (int i=0;i<possibleHexas.Count;i++) if (possibleHexasValues[i] == maxTargets) bestHexas.Add(possibleHexas[i]);
		
		if (bestHexas.Count > 0){
			return bestHexas[0]; // Improve by finding the best one to return
		}else{
			return null;
		}
	}
	///<summary>
	/// Searches the hexa where the mage can hit by using skill the highest amount of Enemies from their current position.
	///</summary>
	///<param name = currentChar> The current character controlled by the AI </param>
	///<returns>  Returns the position of the hexa that either will allow the mage to hit the highest amount of Enemies from the current position. </returns>
	public static Point findWhereToAttackMageSkill(Character currentChar){
		int maxTargets = 0;
		//Character currentChar = hexaGrid.charList[myCharID];
		List<int> possibleHexasValues = new List<int>();
		List<Point> possibleHexas = hexaGrid.findHexasInSight2(currentChar.x,currentChar.y,currentChar.getClassData().skill_1.range);
		foreach (Point p in possibleHexas){
			List<Character> lc = hexaGrid.getCharWithinRange(p.x,p.y,currentChar.getClassData().skill_1.rangeAoE);
			int nb = 0;
			// filter allies
			foreach (Character c in lc){
				if (c.team != currentChar.team) nb++;
			}
			possibleHexasValues.Add(nb);
			if (nb > maxTargets) maxTargets = nb;
		}
		List<Point> bestHexas = new List<Point>();
		for (int i=0;i<possibleHexas.Count;i++) if (possibleHexasValues[i] == maxTargets) bestHexas.Add(possibleHexas[i]);
		
		if (bestHexas.Count > 0){
			return bestHexas[0]; // Improve could be done by finding the best one to return 
		}else{
			return null;
		}
	}
	
	///<summary>
	/// Searches the hexa where the soigneur can heal the highest amount of allies from their current position.
	///</summary>
	///<param name = currentChar> The current character controlled by the AI </param>
	///<returns> Returns the position of the hexa that either will allow the soigneur to heal the highest amount of allies from the current position (aoe skill). </returns>
	public static Point findWhereToHealSoigneurSkill(Character currentChar){
		int maxTargets = 0;
		//Character currentChar = hexaGrid.charList[myCharID];
		List<int> possibleHexasValues = new List<int>();
		List<Point> possibleHexas = hexaGrid.findHexasInSight2(currentChar.x,currentChar.y,currentChar.getClassData().skill_1.range);
		foreach (Point p in possibleHexas){
			List<Character> lc = hexaGrid.getCharWithinRange(p.x,p.y,currentChar.getClassData().skill_1.rangeAoE);
			int nb = 0;
			// filter Enemies/self
			foreach (Character c in lc){
				if (c.team == currentChar.team && c != currentChar) nb++;
			}
			possibleHexasValues.Add(nb);
			if (nb > maxTargets) maxTargets = nb;
		}
		List<Point> bestHexas = new List<Point>();
		for (int i=0;i<possibleHexas.Count;i++) if (possibleHexasValues[i] == maxTargets) bestHexas.Add(possibleHexas[i]);
		
		if (bestHexas.Count > 0){
			return bestHexas[0]; // Improve by finding the best one to return
		}else{
			return null;
		}
	}
	
	///<summary>
	/// Searches the ally that can be healed for the most from the current character's position, assuming the character is a healer.
	///</summary>
	///<param name = currentChar> The current character controlled by the AI </param>
	///<returns> Returns the ID of the ally that can be healed for the most from the current character's position assuming the character is a healer. </returns>
	public static Character findCharToHeal(Character currentChar){
		//Character currentChar = hexaGrid.charList[myCharID];
		int highest = 0;
		Character cHighest = null;
		foreach (Character c in hexaGrid.charList){
			if (c.team == currentChar.team && c != currentChar){
				if (hexaGrid.hexaInSight(currentChar.x,currentChar.y,c.x,c.y,currentChar.getClassData().basicAttack.range)){
					if (c.HPmax-c.HP > highest){
						highest = c.HP;
						cHighest = c;
					}
				}
			}
		}
		return cHighest;
	}

	///<summary>
	///Detects if the character we're attacking can be killed or not
	///</summary>
	///<param name = currentChar> The current character controlled by the AI </param>
	///<returns> Returns true or false </returns>
	public static bool canBeKilled(Character currentChar){
		foreach(Character c in hexaGrid.charList){
			if((c.team != currentChar.team) && (c != currentChar)){
				if(hexaGrid.hexaInSight(currentChar.x, currentChar.y, c.x, c.y, currentChar.getClassData().basicAttack.range)){
					if((c.HP < currentChar.getClassData().basicAttack.effectValue * 2) || (c.HP < currentChar.getClassData().skill_1.effectValue)){
						return true;
					}
				}
			}
		}
		return false;
	}


	
	///<summary>
	/// Searches the ally that can be buffed for the most from the current character's position, assuming the character is a envouteur.
	///</summary>
	///<param name = currentChar> The current character controlled by the AI </param>
	///<returns> Returns the ID of the ally that can be buffed for the most from the current character's position, assuming the character is a envouteur. </returns>
	public static Character findCharToBuff(Character currentChar){
		//Character currentChar = hexaGrid.charList[myCharID];
		int highest = 0;
		Character cHighest = null;
		foreach (Character c in hexaGrid.charList){
			if (c.team == currentChar.team && c != currentChar){
				if (hexaGrid.hexaInSight(currentChar.x,currentChar.y,c.x,c.y,currentChar.getClassData().basicAttack.range)){
					int classPrio;
					switch (c.charClass){
						case CharClass.GUERRIER  : classPrio = 5; break;
						case CharClass.VOLEUR    : classPrio = 6; break;
						case CharClass.ARCHER    : classPrio = 4; break;
						case CharClass.MAGE      : classPrio = 3; break;
						case CharClass.SOIGNEUR  : classPrio = 2; break;
						case CharClass.ENVOUTEUR : classPrio = 1; break;
						default : classPrio = 0; break;
					}
					if (classPrio > highest){
						highest = classPrio;
						cHighest = c;
					}
				}
			}
		}
		return cHighest;
	}

	// -----------------------------------------------------------------------------------------------------------------------------------------------------------------
	// Functions used for AI HARD
	// -----------------------------------------------------------------------------------------------------------------------------------------------------------------

	public static int calculateThreatAtHexa(int x,int y,int charID){
		/*Character currentChar = hexaGrid.charList[charID];
		int threat = 0;
		bool stop = false;
		for (int j=charID+1;;j++){
			Character c = hexaGrid.charList[j%(hexaGrid.charList.Count)];
			if (c == currentChar) break;
			
			if (c.team != currentChar.team && c.charClass != CharClass.SOIGNEUR && c.charClass != CharClass.ENVOUTEUR){
				int damage = 0;
				// 0 PM
				if (c.skillAvailable){
					if (hexaGrid.hexaInSight(c.x,c.y,x,y,c.getClassData().skill_1.range)){
						damage = c.getClassData().skill_1.effectValue * c.PA;
					}
					if (hexaGrid.hexaInSight(c.x,c.y,x,y,c.getClassData().basicAttack.range)){
						damage += c.getClassData().basicAttack.effectValue * (c.PA-1);
					}
				}else{
					if (hexaGrid.hexaInSight(c.x,c.y,x,y,c.getClassData().basicAttack.range)){
						damage = c.getClassData().basicAttack.effectValue * c.PA;
					}
				}
				
				// 1+ PM
				for (int i=1;i<c.PA && damage == 0;i++){
					Point realCharPos = new Point(c.x,c.y);
					List<Point> listH = hexaGrid.findAllPaths(c.x,c.y,c.PM*i);
					foreach (Point charpos in listH){
						c.updatePos2(charPos.x,charPos.y,hexaGrid);
						if (c.skillAvailable){
							if (hexaGrid.hexaInSight(charpos.x,charpos.y,x,y,c.getClassData().skill_1.range)){
								damage = c.getClassData().skill_1.effectValue * c.PA;
							}
							if (hexaGrid.hexaInSight(charpos.x,charpos.y,x,y,c.getClassData().basicAttack.range)){
								damage += c.getClassData().basicAttack.effectValue * (c.PA-1);
							}
							if (damage > 0) break;
						}else{
							if (hexaGrid.hexaInSight(charpos.x,charpos.y,x,y,c.getClassData().basicAttack.range)){
								damage = c.getClassData().basicAttack.effectValue * c.PA;
								break;
							}
						}
					}
					c.updatePos2(realCharPos.x,realCharPos.y,hexaGrid);
				}
				threat -= damage;
				if (currentChar.HP - threat <= 0) return -currentChar.HP - 10;
			}else if (c.team == currentChar.team && c.charClass == CharClass.SOIGNEUR){
				
			}
		}
		return threat;*/
		return 0;
	}
	
	///<summary>
	/// Calculates the maximum amount of targets from the current character's position.
	///</summary>
	///<param name = currentChar> The current character controlled by the AI </param>
	///<returns> Returns the maximum amount of targets from the current character's position. </returns>
	public static int getNbMaxTargets(Character currentChar){
		int maxTargets = 0;
		//Character currentChar = hexaGrid.charList[charID];
		if (currentChar.skillAvailable){
			List<Point> hexas = hexaGrid.findHexasInSight2(currentChar.x,currentChar.y,currentChar.getClassData().skill_1.range);
			foreach (Point p in hexas){
				List<Character> lc = hexaGrid.getCharWithinRange(p.x,p.y,currentChar.getClassData().skill_1.rangeAoE);
				int nb = 0;
				// filter allies / Enemies
				if (currentChar.getClassData().skill_1.targetsEnemies){
					foreach (Character c in lc){
						if (c.team != currentChar.team) nb++;
					}
				}
				if (currentChar.getClassData().skill_1.targetsAllies){
					foreach (Character c in lc){
						if (c.team == currentChar.team && c != currentChar) nb++;
					}
				}
				// Soigneur : filter allies with full hp
				if (currentChar.charClass == CharClass.SOIGNEUR){
					foreach (Character c in lc){
						if (c.team == currentChar.team && c != currentChar && c.HP == c.HPmax) nb--;
					}
				}
				if (nb > maxTargets) maxTargets = nb;
			}
		}else{
			List<Point> hexas = hexaGrid.findHexasInSight2(currentChar.x,currentChar.y,currentChar.getClassData().basicAttack.range);
			foreach (Point p in hexas){
				List<Character> lc = hexaGrid.getCharWithinRange(p.x,p.y,currentChar.getClassData().basicAttack.rangeAoE);
				int nb = 0;
				// filter allies / Enemies
				if (currentChar.getClassData().basicAttack.targetsEnemies){
					foreach (Character c in lc){
						if (c.team != currentChar.team) nb++;
					}
				}
				if (currentChar.getClassData().basicAttack.targetsAllies){
					foreach (Character c in lc){
						if (c.team == currentChar.team && c != currentChar) nb++;
					}
				}
				// Soigneur : filter allies with full hp
				if (currentChar.charClass == CharClass.SOIGNEUR){
					foreach (Character c in lc){
						if (c.team == currentChar.team && c != currentChar && c.HP == c.HPmax) nb--;
					}
				}
				if (nb > maxTargets) maxTargets = nb;
			}
		}
		return maxTargets;
	}
	
	///<summary>
	/// Determines if the target is within the current character's range attack
	///</summary>
	///<param name = CharID> The current character ID </param>
	///<param name = TargetID> The target ID </param>
	///<returns> Returns true or false </returns>
	public static bool isCharWithinRangeAttack(int charID,int targetID){
		Character currentChar = hexaGrid.charList[charID];
		Character targetChar = hexaGrid.charList[targetID];
		Point charPos = new Point(currentChar.x,currentChar.y);
		
		if (hexaGrid.hexaInSight(currentChar.x,currentChar.y,targetChar.x,targetChar.y,currentChar.getClassData().basicAttack.range)) return true;
		int i = currentChar.PM * (currentChar.PA-1);
		if (i > 0){
			List<Point> listH = hexaGrid.findAllPaths(currentChar.x,currentChar.y,i);
			foreach (Point charpos in listH){
				currentChar.updatePos2(charpos.x,charpos.y,hexaGrid);
				if (hexaGrid.hexaInSight(currentChar.x,currentChar.y,targetChar.x,targetChar.y,currentChar.getClassData().basicAttack.range)){
					currentChar.updatePos2(charPos.x,charPos.y,hexaGrid);
					return true;
				}
			}
		}
		currentChar.updatePos2(charPos.x,charPos.y,hexaGrid);
		return false;
	}
	
	///<summary>
	/// Determines if the target is within the current character's range skill
	///</summary>
	///<param name = CharID> The current character ID </param>
	///<param name = TargetID> The target ID</param>
	///<returns> Returns true or false  </returns>
	public static bool isCharWithinRangeSkill(int charID,int targetID){
		Character currentChar = hexaGrid.charList[charID];
		Character targetChar = hexaGrid.charList[targetID];
		Point charPos = new Point(currentChar.x,currentChar.y);
		
		if (hexaGrid.hexaInSight(currentChar.x,currentChar.y,targetChar.x,targetChar.y,currentChar.getClassData().skill_1.range)) return true;
		int i = currentChar.PM * (currentChar.PA-1);
		if (i > 0){
			List<Point> listH = hexaGrid.findAllPaths(currentChar.x,currentChar.y,i);
			foreach (Point charpos in listH){
				currentChar.updatePos2(charpos.x,charpos.y,hexaGrid);
				if (hexaGrid.hexaInSight(currentChar.x,currentChar.y,targetChar.x,targetChar.y,currentChar.getClassData().skill_1.range)){
					currentChar.updatePos2(charPos.x,charPos.y,hexaGrid);
					return true;
				}
			}
		}
		currentChar.updatePos2(charPos.x,charPos.y,hexaGrid);
		return false;
	}
	
	///<summary>
	/// Searches the hexas where the damage is the highest in a list of possible hexas.
	///</summary>
	///<param name = CharID> The current character ID </param>
	///<param name = possibleHexas> The list of possible hexas </param>
	///<returns> Returns a list of hexas where the damage is the highest in a list of possible hexas. </returns>
	public static List<Point> findHexasWhereDamageIsHighest(int charID,List<Point> possibleHexas){
		Character currentChar = hexaGrid.charList[charID];
		int minDistance = 100000;
		Character cLowest = AIUtil.findEnemy(currentChar);
		// find best value
		List<int> possibleHexasValues = new List<int>();
		foreach (Point p in possibleHexas){
			int d = hexaGrid.getWalkingDistance(p.x,p.y,cLowest.x,cLowest.y);
			possibleHexasValues.Add(d);
			if (d != -1) if (d < minDistance) minDistance = d;
		}
		// find all hexas with best value
		List<Point> bestHexas = new List<Point>();
		for (int i=0;i<possibleHexas.Count;i++){
			if (possibleHexasValues[i] == minDistance){
				bestHexas.Add(possibleHexas[i]);
			}
		}
		return bestHexas;
	}
	
	///<summary>
	/// AI Hard class
	///</summary>
	public class AIHard {

		///<summary>
		/// The current character approach the closest enemy by going to the best hexa
		/// or skip their turn if their current position is the best.
		///</summary>
		///<param name = currentChar> The current character ID </param>
		///<returns> Move or Skip turn </returns>
		public static ActionAIPos doApproachEnemy(Character currentChar){
			//Character currentChar = hexaGrid.charList[myCharID];
			//Character cLowest = AIUtil.findEnemy(currentChar); useless
			//Debug.Log("Enemy type : " + cLowest.charClass.ToString());
			List<Point> listH = hexaGrid.findAllPaths(currentChar.x,currentChar.y,currentChar.PM);
			if (listH != null && listH.Count > 0){
				// Find hexas where damage dealt is highest
				//int[] threat = AIUtil.calculateThreat(myCharID);
				//for (int i=0;i<threat.Length;i++) threat[i] = - threat[i];
				//List<Point> bestHexas  = AIUtil.findHexasWhereValueIsMax(listH, threat); //this was stupid, removing it makes everything good now!!!
				// Find hexas where position to lowest Enemy is lowest
				List<Point> bestHexas2 = AIUtil.findHexasToApproachEnemy(currentChar, listH); //put back lowest if problems
				Point bestHexa = bestHexas2[0];
				if (bestHexa.x == currentChar.x && bestHexa.y == currentChar.y) return new ActionAIPos(MainGame.ActionType.SKIP,null);
				else return new ActionAIPos(MainGame.ActionType.MOVE,bestHexa);
			}else{
				return new ActionAIPos(MainGame.ActionType.SKIP,null);
			}
		}

		//This function above is important

		///<summary>
		/// The current character approach the closest ally by going to the best hexa.
		/// or skip their turn if their current position is the best.
		///</summary>
		///<param name = currentChar> The current character ID </param>
		///<returns> Move or Skip turn </returns>
		public static ActionAIPos doApproachAlly(Character currentChar){
			//Character currentChar = hexaGrid.charList[myCharID];
			Character cLowest = AIUtil.findEnemy(currentChar);
			List<Point> listH = hexaGrid.findAllPaths(currentChar.x,currentChar.y,currentChar.PM);
			if (listH != null && listH.Count > 0){
				// Find hexas where position is closest to allies
				//This might be the problem... Actually it is a problem for soigneurs
				List<Point> bestHexas  = AIUtil.findHexasClosestToAllies(currentChar,listH);
				// Find hexas where threat is lowest
				int[] threat = AIUtil.calculateThreat(currentChar.team);
				for (int i=0;i<threat.Length;i++) threat[i] = - threat[i];
				List<Point> bestHexas2  = AIUtil.findHexasWhereValueIsMax(bestHexas,threat);
				// Find hexas where position to lowest Enemy is lowest
				//List<Point> bestHexas3 = AIUtil.findHexasToApproachEnemy(myCharID,bestHexas2);
				Point bestHexa = bestHexas2[0];
				if (bestHexa.x == currentChar.x && bestHexa.y == currentChar.y) return new ActionAIPos(MainGame.ActionType.SKIP,null);
				else return new ActionAIPos(MainGame.ActionType.MOVE,bestHexa);
			}else{
				return new ActionAIPos(MainGame.ActionType.SKIP,null);
			}
		}
		
		///<summary>
		/// The current character do a random movement.
		///</summary>
		///<param name = myCharID> The current character ID </param>
		///<returns> Skip turn </returns>
		public static ActionAIPos doRandomMovement(int myCharID){
			return new ActionAIPos(MainGame.ActionType.SKIP,null);
		}
		
		///<summary>
		/// The current character flee by going to the best hexa
		/// or skip their turn if their current position is the best.
		///</summary>
		///<param name = currentChar> </param>
		///<returns> Move or Skip turn </returns>

		//Remake this function 
		
		public static ActionAIPos doFlee(Character currentChar){
			//Character currentChar = hexaGrid.charList[myCharID];
			Character cLowest = AIUtil.findEnemy(currentChar);
			List<Point> listH = hexaGrid.findAllPaths(currentChar.x,currentChar.y, currentChar.PM);
			if (listH != null && listH.Count > 0){
				// Find hexas where threat is lowest
				int[] threat = AIUtil.calculateThreat(currentChar.team);
				for (int i=0;i<threat.Length;i++) threat[i] = - threat[i];
				List<Point> bestHexas  = AIUtil.findHexasWhereValueIsMax(listH,threat);
				// Find hexas where position is closest to allies
				List<Point> bestHexas2 = AIUtil.findHexasClosestToAllies(currentChar,bestHexas); //change here
				// Find hexas where position is closest to lowest Enemy
				//List<Point> bestHexas3 = AIUtil.findHexasToApproachEnemy(myCharID,bestHexas2);

				Point bestHexa = bestHexas2[0];

				// Debug.Log("Here the paths you got : " + bestHexa.x + " " + bestHexa.y);

				if (bestHexa.x == currentChar.x && bestHexa.y == currentChar.y) return new ActionAIPos(MainGame.ActionType.SKIP,null);
				else return new ActionAIPos(MainGame.ActionType.MOVE,bestHexa);
			}else{
				return new ActionAIPos(MainGame.ActionType.SKIP,null);
			}
		}

		//Other version of the Flee method
		public static ActionAIPos doEscape(Character currentChar){
			List <Point> listH = hexaGrid.findAllPaths(currentChar.x, currentChar.y, currentChar.PM);
			if(listH != null && listH.Count > 0){
				List <Point> bestHexas = AIUtil.findHexasToFlee(currentChar, listH);

				Point bestHexa = null;

				foreach(Point p in bestHexas){
					if(p != null){
						bestHexa = p;
					}
				}

				if(bestHexa.x == currentChar.x && bestHexa.y == currentChar.y) return new ActionAIPos(MainGame.ActionType.SKIP, null);
				else return new ActionAIPos(MainGame.ActionType.MOVE, bestHexa);
			}else{
				return new ActionAIPos(MainGame.ActionType.SKIP, null);
			}
		}
		
		///<summary>
		/// The current character attacks a target if they can
		///</summary>
		///<param name = currentChar> The current character ID </param>
		///<returns> Attack or Skip turn </returns>
		public static ActionAIPos doAttack(Character currentChar){
			//Character currentChar = hexaGrid.charList[myCharID];
			Point posAttack = null;
			switch (currentChar.charClass){
				case CharClass.GUERRIER :
				case CharClass.VOLEUR :
				case CharClass.ARCHER : {
					Character cAttack = AIUtil.findCharToAttack(currentChar); 
					if (cAttack != null) posAttack = new Point(cAttack.x,cAttack.y);
				} break;
				case CharClass.MAGE : {
					posAttack = AIUtil.findWhereToAttackMage(currentChar);
				} break;
				case CharClass.SOIGNEUR : {
					Character cAttack = AIUtil.findCharToHeal(currentChar);
					if (cAttack != null) posAttack = new Point(cAttack.x,cAttack.y);
				} break;
				case CharClass.ENVOUTEUR: {
					Character cAttack = AIUtil.findCharToBuff(currentChar);
					if (cAttack != null) posAttack = new Point(cAttack.x,cAttack.y);
				} break;
			}
			if (posAttack != null){
				return new  ActionAIPos(MainGame.ActionType.ATK1,posAttack);
			}else{
				return new ActionAIPos(MainGame.ActionType.SKIP,null);
			}
		}
		
		///<summary>
		/// The current character uses their skill on a target if they can
		///</summary>
		///<param name = currentChar> The current character ID </param>
		///<param name = targetID> The target ID </param>
		///<returns> Skill or Skip turn </returns>
		public static ActionAIPos doSkill(Character currentChar,int targetID){
			//Character currentChar = hexaGrid.charList[myCharID];
			Point posAttack = null;
			switch (currentChar.charClass){
				case CharClass.GUERRIER :
				case CharClass.VOLEUR :
				case CharClass.ARCHER : {
					Character cAttack = AIUtil.findCharToAttackSkill(currentChar); 
					if (cAttack != null) posAttack = new Point(cAttack.x,cAttack.y);
				} break;
				case CharClass.MAGE : {
					posAttack = AIUtil.findWhereToAttackMageSkill(currentChar);
				} break;
				case CharClass.SOIGNEUR : {
					//changed stuff here
					posAttack = AIUtil.findWhereToHealSoigneurSkill(currentChar); // Add skill function
				} break;
				case CharClass.ENVOUTEUR: {
					posAttack = AIUtil.findWhereToHealSoigneurSkill(currentChar); // Add skill function
				} break;
			}
			if (posAttack != null){
				return new ActionAIPos(MainGame.ActionType.SKILL1,posAttack);
			}else{
				return new ActionAIPos(MainGame.ActionType.SKIP,null);
			}
		}
	}


	
	// -----------------------------------------------------------------------------------------------------------------------------------------------------------------
	// 2.0
	// These functions are used for the behavior of the AI of Classifier2 
	// -----------------------------------------------------------------------------------------------------------------------------------------------------------------
	
	///<summary>
	/// Determines if an enemy can be target for the current character
	///</summary>
	///<param name = cX> Current Character x position</param>
	///<param name = cY> Current Character y position</param>
	///<param name = targetX> Target x position</param>
	///<param name = targetY> Target y position</param>
	///<param name = range> </param>
	///<param name = rangeAoE> </param>
	///<returns> True or false </returns>
	public static bool canTarget(int cX,int cY,int targetX,int targetY,int range,int rangeAoE){
		if (rangeAoE > 0){
			if (hexaGrid.hexaInSight(cX,cY,targetX,targetY,range)){
				return true;
			}else if (hexaGrid.getDistance(cX,cY,targetX,targetY) <= range + rangeAoE){
				List<Point> hexas = hexaGrid.findHexasInSight2(cX,cY,range);
				foreach (Point h in hexas){
					List<Character> chars = hexaGrid.getCharWithinRange(h.x,h.y,rangeAoE);
					foreach (Character c2 in chars){
						if (c2.x == targetX && c2.y == targetY) return true;
					}
				}
				return false;
			}else{
				return false;
			}
		}else{
			return hexaGrid.hexaInSight(cX,cY,targetX,targetY,range);
		}
	}
	
	///<summary>
	/// Determines if the current character can attack their target
	///</summary>
	///<param name = c> Current Character </param>
	///<param name = target> Target </param>
	///<returns> True or false </returns>
	public static bool canTargetAttack(Character c,Character target){ 
		switch (c.charClass){
			case CharClass.GUERRIER :
			case CharClass.ARCHER : 
			case CharClass.MAGE :
				return target.team != c.team && canTarget(c.x,c.y,target.x,target.y,c.getClassData().basicAttack.range,c.getClassData().basicAttack.rangeAoE);
			case CharClass.VOLEUR : 
				return target.team != c.team && /*target.charClass != CharClass.GUERRIER &&*/ canTarget(c.x,c.y,target.x,target.y,c.getClassData().basicAttack.range,c.getClassData().basicAttack.rangeAoE);
			case CharClass.SOIGNEUR :
				return target.team == c.team && target != c && target.HP < target.HPmax && canTarget(c.x,c.y,target.x,target.y,c.getClassData().basicAttack.range,c.getClassData().basicAttack.rangeAoE);
			case CharClass.ENVOUTEUR :
				return target.team == c.team && target != c && target.PA <= target.getClassData().basePA && canTarget(c.x,c.y,target.x,target.y,c.getClassData().basicAttack.range,c.getClassData().basicAttack.rangeAoE);
			default : return false;
		}
	}
	
	///<summary>
	/// Determines if the current character can use their skill on their target
	///</summary>
	///<param name = c> Current Character </param>
	///<param name = target> Target </param>
	///<returns> True or false </returns>
	public static bool canTargetSkill(Character c,Character target){ 
		if (c.skillAvailable){
			switch (c.charClass){
				case CharClass.GUERRIER :
				case CharClass.ARCHER : 
				case CharClass.MAGE :
					return target.team != c.team && canTarget(c.x,c.y,target.x,target.y,c.getClassData().skill_1.range,c.getClassData().skill_1.rangeAoE);
				case CharClass.VOLEUR :
					return target.team != c.team && /*target.charClass != CharClass.GUERRIER &&*/ canTarget(c.x,c.y,target.x,target.y,c.getClassData().skill_1.range,c.getClassData().skill_1.rangeAoE);
				case CharClass.SOIGNEUR :
					return target.team == c.team && target != c && target.HP < target.HPmax && canTarget(c.x,c.y,target.x,target.y,c.getClassData().skill_1.range,c.getClassData().skill_1.rangeAoE);	
				case CharClass.ENVOUTEUR :
					return target.team == c.team && target != c && target.PA <= target.getClassData().basePA && canTarget(c.x,c.y,target.x,target.y,c.getClassData().skill_1.range,c.getClassData().skill_1.rangeAoE);
				default : return false;
			}
		}else{
			return false;
		}
	}
	
	///<summary>
	///
	///</summary>
	///<param name = c> Current Character </param>
	///<param name = target> Target </param>
	///<returns> True or false </returns>
	public static bool canTargetWithMovementAttack(Character c,Character target){
		int cX = c.x; int cY = c.y;
		for (int i=1;i<c.PA;i++){
			List<Point> listH = hexaGrid.findAllPaths(c.x,c.y,c.PM*i);
			foreach (Point charPos in listH){
				c.updatePos2(charPos.x,charPos.y,hexaGrid);
				if (canTargetAttack(c,target)){
					c.updatePos2(cX,cY,hexaGrid);
					return true;
				}
			}
		}
		c.updatePos2(cX,cY,hexaGrid);
		return false;
	}

	//public static bool canKillWithAttack(Character c, Character target) {
		//to do maybe? 
	//}
	
	///<summary>
	/// 
	///</summary>
	///<param name = c> Current Character </param>
	///<param name = target> Target </param>
	///<returns> True or false </returns>
	public static bool canTargetWithMovementSkill(Character c,Character target){
		if (c.skillAvailable){
			int cX = c.x; int cY = c.y;
			for (int i=1;i<c.PA;i++){
				List<Point> listH = hexaGrid.findAllPaths(c.x,c.y,c.PM*i);
				foreach (Point charPos in listH){
					c.updatePos2(charPos.x,charPos.y,hexaGrid);
					if (canTargetSkill(c,target)){
						c.updatePos2(cX,cY,hexaGrid);
						return true;
					}
				}
			}
			c.updatePos2(cX,cY,hexaGrid);
		}
		return false;
	}
	
	///<summary>
	/// Calculates the threat of a character at hexa (x,y) from currentCharID's turn to theirs (charToCheckID).
	///</summary>
	///<param name = x>  </param>
	///<param name = y>  </param>
	///<param name = currentChar> The current character </param>
	///<param name = toCheckChar> Character we are checking  </param>
	///<returns> returns a value the indicates an estimation of the amount of HP potentially
	/// gained until next turn. (negative values indicate damage taken).
	/// If the value -1000 is returned, the character would die. 
	///</returns>
	public static int threatAtHexa(int x,int y,Character currentChar,Character toCheckChar){
		//currentCharID = currentCharID%(hexaGrid.charList.Count);
		//Character toCheckChar = hexaGrid.charList[toCheckCharID];
		int toCheckCharHP = toCheckChar.HP;
		int toCheckCharX = toCheckChar.x;
		int toCheckCharY = toCheckChar.y;
		toCheckChar.updatePos2(x,y,hexaGrid);
		
		int threat = 0;
		foreach (Character c in hexaGrid.charList){
			//Character c = hexaGrid.charList[j%(hexaGrid.charList.Count)];
			if (c == toCheckChar) break;
			
			// damage
			if (c.team != toCheckChar.team && c.charClass != CharClass.SOIGNEUR && c.charClass != CharClass.ENVOUTEUR){
				int damage = 0;
				// 0 PM
				{
					bool skill = canTargetSkill(c,toCheckChar);
					bool attack = canTargetAttack(c,toCheckChar);
					damage = ((skill) ? (c.getClassData().skill_1.effectValue) : 0) + ((attack) ? (c.getClassData().basicAttack.effectValue * ((skill) ? (c.PA-1) : (c.PA))) : 0);
				}
				
				// 1+ PM
				int cX = c.x; int cY = c.y;
				for (int i=1;i<c.PA && damage == 0;i++){
					List<Point> listH = hexaGrid.findAllPaths(c.x,c.y,c.PM*i);
					foreach (Point charPos in listH){
						c.updatePos2(charPos.x,charPos.y,hexaGrid);
						bool skill = canTargetSkill(c,toCheckChar);
						bool attack = canTargetAttack(c,toCheckChar);
						damage = ((skill) ? (c.getClassData().skill_1.effectValue) : 0) + ((attack) ? (c.getClassData().basicAttack.effectValue * ((skill) ? (c.PA-i-1) : (c.PA-i))) : 0);
						if (damage > 0) break;
					}
					c.updatePos2(cX,cY,hexaGrid);
				}
				
				toCheckChar.HP -= damage;
				if (toCheckChar.HP <= 0) break;
			// heal
			}else if (c.team == toCheckChar.team && c.charClass == CharClass.SOIGNEUR){
				int heal = 0;
				// 0 PM
				{
					bool skill = canTargetSkill(c,toCheckChar);
					bool attack = canTargetAttack(c,toCheckChar);
					heal = ((skill) ? (c.getClassData().skill_1.effectValue) : 0) + ((attack) ? (c.getClassData().basicAttack.effectValue * ((skill) ? (c.PA-1) : (c.PA))) : 0);
				}
				
				// 1+ PM
				int cX = c.x; int cY = c.y;
				for (int i=1;i<c.PA && heal == 0;i++){
					Point realCharPos = new Point(c.x,c.y);
					List<Point> listH = hexaGrid.findAllPaths(c.x,c.y,c.PM*i);
					foreach (Point charPos in listH){
						c.updatePos2(charPos.x,charPos.y,hexaGrid);
						bool skill = canTargetSkill(c,toCheckChar);
						bool attack = canTargetAttack(c,toCheckChar);
						heal = ((skill) ? (c.getClassData().skill_1.effectValue) : 0) + ((attack) ? (c.getClassData().basicAttack.effectValue * ((skill) ? (c.PA-i-1) : (c.PA-i))) : 0);
						if (heal > 0) break;
					}
					c.updatePos2(cX,cY,hexaGrid);
				}
				
				toCheckChar.HP += heal;
				if (toCheckChar.HP > toCheckChar.HPmax) toCheckChar.HP = toCheckChar.HPmax;
			}
		}
		
		toCheckChar.updatePos2(toCheckCharX,toCheckCharY,hexaGrid);
		if (toCheckChar.HP <= 0) threat = -1000;
		else threat = toCheckChar.HP - toCheckCharHP;
		toCheckChar.HP = toCheckCharHP;
		return threat;
	}
	
	///<summary>
	/// 
	///</summary>
	///<param name = hexas>  List of hexas </param>
	///<param name = currentChar> The current character </param>
	///<param name = toCheckChar> Character we are checking </param>
	///<returns>  </returns>
	public static List<HexaDamage> threatAtHexas(List<Point> hexas,Character currentChar,Character toCheckChar){
		List<HexaDamage> r = new List<HexaDamage>();
		foreach (Point h in hexas) r.Add(new HexaDamage(h.x,h.y,threatAtHexa(h.x,h.y,currentChar,toCheckChar)));
		return r;
	}	
	
	///<summary>
	/// Searches the hexas where the threat is low
	///</summary>
	///<param name = hexas> List of hexas </param>
	///<param name = currentChar> The current character </param>
	///<param name = toCheckChar> Character we are checking  </param>
	///<returns> Returns a list of hexas where the threat is low</returns>
	public static List<Point> getHexasWhereThreatIsMin(List<Point> hexas,Character currentChar,Character toCheckChar){
		List<Point> hexas2 = new List<Point>();
		if (hexas.Count > 0){
			List<HexaDamage> hexas2_ = threatAtHexas(hexas,currentChar,toCheckChar);
			int best = hexas2_[0].value;
			foreach (HexaDamage hd in hexas2_) if (hd.value >  best) best = hd.value;
			foreach (HexaDamage hd in hexas2_) if (hd.value == best) hexas2.Add(new Point(hd.x,hd.y));
		}
		return hexas2;
	}
	
	///<summary>
	/// Searches the hexas where the current character can get their target in their attack's range.
	/// Can be used to know where an enemy is within range of being attacked.
	/// Can also be used to know where a SOINGEUR is in range to heal an ally. 
	///</summary>
	///<param name = hexas> List of hexas </param>
	///<param name = c> The current character </param>
	///<param name = target> Target </param>
	///<returns> Returns a list of hexas </returns>
	public static List<Point> getHexasWhereCharIsInRangeAttack(List<Point> hexas,Character c,Character target){
		List<Point> hexas2 = new List<Point>();
		if (hexas.Count > 0){
			int cX = c.x; int cY = c.y;
			foreach (Point p in hexas){
				c.updatePos2(p.x,p.y,hexaGrid);
				if (canTargetAttack(c,target)) hexas2.Add(new Point(p.x,p.y));
			}
			c.updatePos2(cX,cY,hexaGrid);
		}
		return hexas2;
	}
	
	///<summary>
	/// Searches the hexas where the current character can get their target in their skill's range.
	/// Can be used to know where an enemy is within range of being attacked.
	/// Can also be used to know where a SOINGEUR is in range to heal an ally. 
	///</summary>
	///<param name = hexas> List of hexas </param>
	///<param name = c> The current character </param>
	///<param name = target> Target </param>
	///<returns> Returns a list of hexas </returns>
	public static List<Point> getHexasWhereCharIsInRangeSkill(List<Point> hexas,Character c,Character target){
		List<Point> hexas2 = new List<Point>();
		if (hexas.Count > 0){
			int cX = c.x; int cY = c.y;
			foreach (Point p in hexas){
				c.updatePos2(p.x,p.y,hexaGrid);
				if (canTargetSkill(c,target)) hexas2.Add(new Point(p.x,p.y));
			}
			c.updatePos2(cX,cY,hexaGrid);
		}
		return hexas2;
	}
	
	///<summary>
	/// Search a path with the lowest walking distance between two characters.
	///</summary>
	///<param name = hexas> List of hexas</param>
	///<param name = c1> Character number 1</param>
	///<param name = c2> Character number 2</param>
	///<returns> Returns the path with the lowest walking distance </returns>
	/** (Lowest PA spent) */
	public static List<Point> getHexasWhereWalkingDistanceIsLowest(List<Point> hexas,Character c1,Character c2){
		List<Point> r = new List<Point>();
		if (hexas.Count > 0){
			//Character c1 = hexaGrid.charList[charID];
			//Character c2 = hexaGrid.charList[targetID];
			Point charPos = new Point(c1.x,c1.y);
			List<int> walkingDistanceTmp = new List<int>();
			c1.updatePos2(hexas[0].x,hexas[0].y,hexaGrid);
			int minWalkingDistanceTmp = hexaGrid.getWalkingDistance(c1.x,c1.y,c2.x,c2.y);
			minWalkingDistanceTmp = (minWalkingDistanceTmp == 0) ? 0 : ((minWalkingDistanceTmp-1)/c1.PM + 1);
			walkingDistanceTmp.Add(minWalkingDistanceTmp);
			int minWalkingDistance = walkingDistanceTmp[0];
			for (int i=1;i<hexas.Count;i++){
				c1.updatePos2(hexas[i].x,hexas[i].y,hexaGrid);
				int walkingDistance = hexaGrid.getWalkingDistance(c1.x,c1.y,c2.x,c2.y);
				walkingDistance = (walkingDistance == 0) ? 0 : ((walkingDistance-1)/c1.PM + 1);
				if (minWalkingDistance > walkingDistance) minWalkingDistance = walkingDistance;
				walkingDistanceTmp.Add(walkingDistance);
			}
			for (int i=0;i<hexas.Count;i++) if (walkingDistanceTmp[i] == minWalkingDistance) r.Add(new Point(hexas[i].x,hexas[i].y));
			c1.updatePos2(charPos.x,charPos.y,hexaGrid);
		}
		
		return r;
	}
	
	///<summary>
	/// Searches the hexas with the lowest amount of PA to use to get to
	///</summary>
	///<param name = hexas> List of hexas </param>
	///<param name = currentChar> The current char </param>
	///<returns> Returns a list of hexas with the lowest amount of PA to use to get to </returns>
	public static List<Point> getHexasWhereMovementIsLowest(List<Point> hexas,Character currentChar){
		List<Point> r = new List<Point>();
		if (hexas.Count > 0){
			List<int> values = new List<int>();
			foreach (Point h in hexas) values.Add(findSequencePathToHexa(currentChar,h.x,h.y).Count);
			int lowest = values[0];
			for (int i=0;i<values.Count;i++) if (values[i] < lowest) lowest = values[i];
			for (int i=0;i<values.Count;i++) if (values[i] == lowest) r.Add(new Point(hexas[i].x,hexas[i].y));
		}
		return r;
	}

	///<summary>
	/// 
	///</summary>
	///<param name = c> </param>
	///<returns>  </returns>	
	public static List<Character> getTargetableCharsInRangeAttack(Character c){
		List<Character> r = new List<Character>();
		foreach (Character c2 in hexaGrid.charList) if (canTargetAttack(c,c2)) r.Add(c2);
		return r;
	}
	
	///<summary>
	/// 
	///</summary>
	///<param name = c> </param>
	///<returns>  </returns>
	public static List<Character> getTargetableCharsInRangeSkill(Character c){
		List<Character> r = new List<Character>();
		foreach (Character c2 in hexaGrid.charList) if (canTargetSkill(c,c2)) r.Add(c2);
		return r;
	}

	///<summary>
	/// 
	///</summary>
	///<param name = c> </param>
	///<returns> Finds the target with highest threat within range. (Also works for soigneur) </returns>
	public static Character getMainTargetAttack(Character c){  //need to continue replacing that big shit
		//Character c = hexaGrid.charList[charID];
		List<Character> l = getTargetableCharsInRangeAttack(c);
		// Target char in range
		if (l.Count > 0){
			List<int> threatTmp = new List<int>();
			threatTmp.Add(threatAtHexa(l[0].x,l[0].y,c,l[0]));
			int minThreat = threatTmp[0];
			for (int i=1;i<l.Count;i++){
				int threat = threatAtHexa(l[i].x,l[i].y,c,l[i]);
				if (minThreat > threat) minThreat = threat;
				threatTmp.Add(threat);
			}
			for (int i=0;i<l.Count;i++) if (threatTmp[i] == minThreat) return l[i];
			return l[0];
		}else{
			// Target char in range after movement
			foreach (Character c2 in hexaGrid.charList){
				switch (c.charClass){
					case CharClass.GUERRIER : 
					case CharClass.VOLEUR :
					case CharClass.ARCHER :
					case CharClass.MAGE :
						if (c.team != c2.team && canTargetWithMovementAttack(c,c2)) l.Add(c2); break;
					
					case CharClass.SOIGNEUR :
					case CharClass.ENVOUTEUR:
						if (c.team == c2.team && c != c2 && canTargetWithMovementAttack(c,c2)) l.Add(c2); break;
				}
			}
			if (l.Count > 0){
				List<int> threatTmp = new List<int>();
				threatTmp.Add(threatAtHexa(l[0].x,l[0].y,c,l[0]));
				int minThreat = threatTmp[0];
				for (int i=1;i<l.Count;i++){
					int threat = threatAtHexa(l[i].x,l[i].y,c,l[i]);
					if (minThreat > threat) minThreat = threat;
					threatTmp.Add(threat);
				}
				for (int i=0;i<l.Count;i++) if (threatTmp[i] == minThreat) return l[i];
				return l[0];
			// Get closer to char
			}else{
				foreach (Character c2 in hexaGrid.charList){
					switch (c.charClass){
						case CharClass.GUERRIER : 
						case CharClass.VOLEUR :
						case CharClass.ARCHER :
						case CharClass.MAGE :
							if (c.team != c2.team) l.Add(c2); break;
						
						case CharClass.SOIGNEUR :
						case CharClass.ENVOUTEUR:
							if (c.team == c2.team && c != c2) l.Add(c2); break;
					}
				}
				if (l.Count > 0){
					List<int> threatTmp = new List<int>();
					threatTmp.Add(threatAtHexa(l[0].x,l[0].y,c,l[0]));
					int minThreat = threatTmp[0];
					for (int i=1;i<l.Count;i++){
						int threat = threatAtHexa(l[i].x,l[i].y,c,l[i]);
						if (minThreat > threat) minThreat = threat;
						threatTmp.Add(threat);
					}
					for (int i=0;i<l.Count;i++) if (threatTmp[i] == minThreat) return l[i];
					return l[0];
				}
			}
		}
		return null;
	}
	
	///<summary>
	/// 
	///</summary>
	///<param name = c> </param>
	///<returns>  </returns>
	public static Character getMainTargetSkill(Character c){
		//Character c = hexaGrid.charList[charID];
		List<Character> l = getTargetableCharsInRangeSkill(c);
		if (l.Count > 0){
			List<int> threatTmp = new List<int>();
			threatTmp.Add(threatAtHexa(l[0].x,l[0].y,c,l[0]));
			int minThreat = threatTmp[0];
			for (int i=1;i<l.Count;i++){
				int threat = threatAtHexa(l[i].x,l[i].y,c,l[i]);
				if (minThreat > threat) minThreat = threat;
				threatTmp.Add(threat);
			}
			for (int i=0;i<l.Count;i++) if (threatTmp[i] == minThreat) return l[i];
			return l[0];
		}else{
			foreach (Character c2 in hexaGrid.charList){
				switch (c.charClass){
					case CharClass.GUERRIER : 
					case CharClass.VOLEUR :
					case CharClass.ARCHER :
					case CharClass.MAGE :
						if (c.team != c2.team && canTargetWithMovementSkill(c,c2)) l.Add(c2); break;
					
					case CharClass.SOIGNEUR :
					case CharClass.ENVOUTEUR:
						if (c.team == c2.team && c != c2 && canTargetWithMovementSkill(c,c2)) l.Add(c2); break;
				}
			}
			if (l.Count > 0){
				List<int> threatTmp = new List<int>();
				threatTmp.Add(threatAtHexa(l[0].x,l[0].y,c,l[0]));
				int minThreat = threatTmp[0];
				for (int i=1;i<l.Count;i++){
					int threat = threatAtHexa(l[i].x,l[i].y,c,l[i]);
					if (minThreat > threat) minThreat = threat;
					threatTmp.Add(threat);
				}
				for (int i=0;i<l.Count;i++) if (threatTmp[i] == minThreat) return l[i];
				return l[0];
			}else{
				foreach (Character c2 in hexaGrid.charList){
					switch (c.charClass){
						case CharClass.GUERRIER : 
						case CharClass.VOLEUR :
						case CharClass.ARCHER :
						case CharClass.MAGE :
							if (c.team != c2.team) l.Add(c2); break;
						
						case CharClass.SOIGNEUR :
						case CharClass.ENVOUTEUR:
							if (c.team == c2.team && c != c2) l.Add(c2); break;
					}
				}
				if (l.Count > 0){
					List<int> threatTmp = new List<int>();
					threatTmp.Add(threatAtHexa(l[0].x,l[0].y,c,l[0]));
					int minThreat = threatTmp[0];
					for (int i=1;i<l.Count;i++){
						int threat = threatAtHexa(l[i].x,l[i].y,c,l[i]);
						if (minThreat > threat) minThreat = threat;
						threatTmp.Add(threat);
					}
					for (int i=0;i<l.Count;i++) if (threatTmp[i] == minThreat) return l[i];
					return l[0];
				}
			}
		}
		return null;
	}

	///<summary>
	/// Search the hexa that should be targeted to attack. Assumes mainTarget is targetable
	///</summary>
	///<param name = c > The character controlled by the AI </param>
	///<param name = mainTarget > The main target </param>
	///<returns> Returns the hexa that should be targeted to attack. Assumes mainTarget is targetable </returns>
	public static Point getPosToUseAttack(Character c,Character mainTarget){
		if (c.getClassData().basicAttack.rangeAoE > 0){
			List<Point> hexas = hexaGrid.findHexasInSight2(c.x,c.y,c.getClassData().basicAttack.range);
			HexaDamage pos = new HexaDamage(hexas[0].x,hexas[0].y,0);
			foreach (Point h in hexas){
				List<Character> chars = hexaGrid.getCharWithinRange(h.x,h.y,c.getClassData().basicAttack.rangeAoE);
				
				if (!c.getClassData().basicAttack.targetsEnemies){
					for (int i=0;i<chars.Count;i++){
						if (chars[i].team != c.team){
							chars.RemoveAt(i); i--;
						}
					}
				}
				if (!c.getClassData().basicAttack.targetsAllies){
					for (int i=0;i<chars.Count;i++){
						if (chars[i].team == c.team){
							chars.RemoveAt(i); i--;
						}
					}
				}
				
				foreach (Character c2 in chars){
					if (c2 == mainTarget){
						if (chars.Count > pos.value){
							pos.x = h.x;
							pos.y = h.y;
							pos.value = chars.Count;
						}
					}
				}
			}
			return new Point(pos.x,pos.y);
		}else{
			return new Point(mainTarget.x,mainTarget.y);
		}
	}
	
	///<summary>
	/// Search the hexa that should be targeted to attack (skill). Assumes mainTarget is targetable 
	///</summary>
	///<param name = c > the character controlled by the AI </param>
	///<param name = mainTarget > The main target  </param>
	///<returns> Returns the hexa that should be targeted to attack (skill). Assumes mainTarget is targetable </returns>
	public static Point getPosToUseSkill(Character c,Character mainTarget){
		if (c.getClassData().skill_1.rangeAoE > 0){
			List<Point> hexas = hexaGrid.findHexasInSight2(c.x,c.y,c.getClassData().skill_1.range);
			HexaDamage pos = new HexaDamage(hexas[0].x,hexas[0].y,0);
			foreach (Point h in hexas){
				List<Character> chars = hexaGrid.getCharWithinRange(h.x,h.y,c.getClassData().skill_1.rangeAoE);
				
				if (!c.getClassData().skill_1.targetsEnemies){
					for (int i=0;i<chars.Count;i++){
						if (chars[i].team != c.team){
							chars.RemoveAt(i); i--;
						}
					}
				}
				if (!c.getClassData().skill_1.targetsAllies){
					for (int i=0;i<chars.Count;i++){
						if (chars[i].team == c.team){
							chars.RemoveAt(i); i--;
						}
					}
				}
				
				foreach (Character c2 in chars){
					if (c2 == mainTarget){
						if (chars.Count > pos.value){
							pos.x = h.x;
							pos.y = h.y;
							pos.value = chars.Count;
						}
					}
				}
			}
			return new Point(pos.x,pos.y);
		}else{
			return new Point(mainTarget.x,mainTarget.y);
		}
	}
		
	///<summary>
	/// Search the hexas where the current character can get closser to their allies
	///</summary>
	///<param name = hexas > List of hexas </param>
	///<param name = currentChar > The current character </param>
	///<returns> Returns a list of the hexas where the current character can get closer to their allies </returns>
	public static List<Point> getHexasClosestToAllies(List<Point> hexas,Character currentChar){
		//Character currentChar = hexaGrid.charList[charID];
		List<Character> alliesList = new List<Character>();
		foreach (Character c in hexaGrid.charList){
			if (c.team == currentChar.team && c != currentChar) alliesList.Add(c);
		}
		if (alliesList.Count > 0 && hexas.Count > 0){
			int charPosX = currentChar.x;
			int charPosY = currentChar.y;
			switch (currentChar.charClass){
				case CharClass.GUERRIER : 
				case CharClass.VOLEUR :
				case CharClass.ARCHER :
				case CharClass.MAGE :
				case CharClass.SOIGNEUR :
				case CharClass.ENVOUTEUR: {
					List<Point> r = new List<Point>();
					List<int> walkingDistanceList = new List<int>();
					int walkingDistance = 0;
					currentChar.updatePos2(hexas[0].x,hexas[0].y,hexaGrid);
					foreach (Character c in alliesList) walkingDistance += hexaGrid.getWalkingDistance(currentChar.x,currentChar.y,c.x,c.y) * ((c.charClass == CharClass.SOIGNEUR) ? 3 : 1);
					walkingDistanceList.Add(walkingDistance);
					int minWalkingDistance = walkingDistanceList[0];
					
					for (int i=1;i<hexas.Count;i++){
						walkingDistance = 0;
						currentChar.updatePos2(hexas[i].x,hexas[i].y,hexaGrid);
						foreach (Character c in alliesList) walkingDistance += hexaGrid.getWalkingDistance(currentChar.x,currentChar.y,c.x,c.y) * ((c.charClass == CharClass.SOIGNEUR) ? 3 : 1);
						walkingDistanceList.Add(walkingDistance);
						if (minWalkingDistance > walkingDistance) minWalkingDistance = walkingDistance;
					}
					
					for (int i=0;i<hexas.Count;i++){
						if (walkingDistanceList[i] == minWalkingDistance) r.Add(hexas[i]);
					}
					
					currentChar.updatePos2(charPosX,charPosY,hexaGrid);
					return r;
				}
			}
		}
		return hexas;
	}
	
	public class AIHard2 {

		///<summary>
		/// 
		///</summary>
		///<param name = currentChar > </param>
		///<returns>  </returns>
		public static Point doFleePos(Character currentChar){
			//Character currentChar = hexaGrid.charList[myCharID];
			Character toCheckChar = null;
			foreach(Character c in hexaGrid.charList){
				if(c.team != currentChar.team){ //I changed the parameter here
					toCheckChar = c;
				}
			}
			List<Point> hexas1  = hexaGrid.findAllPaths(currentChar.x,currentChar.y,currentChar.PM*currentChar.PA);
			List<Point> hexas2 = getHexasWhereThreatIsMin(hexas1,currentChar,toCheckChar);
			List<Point> hexas3 = getHexasClosestToAllies(hexas2,currentChar);
			List<Point> hexas4 = getHexasWhereMovementIsLowest(hexas3, currentChar);
			return hexas4[0];
		}

		///<summary>
		/// 
		///</summary>
		///<param name = currentChar > </param>
		///<returns>  </returns>
		public static List<ActionAIPos> doFlee(Character currentChar){
			//Character currentChar = hexaGrid.charList[myCharID];
			Point hexaPos = doFleePos(currentChar); 
			List<ActionAIPos> sequence = new List<ActionAIPos>();
			
			if (!(hexaPos.x == currentChar.x && hexaPos.y == currentChar.y)){
				sequence = findSequencePathToHexa(currentChar,hexaPos.x,hexaPos.y);
			}
			
			if (sequence.Count < currentChar.PA){
				int charposX = currentChar.x;
				int charposY = currentChar.y;
				currentChar.updatePos2(hexaPos.x,hexaPos.y,hexaGrid);
				// Get target
				List<Character> targetList = getTargetableCharsInRangeAttack(currentChar);
				// Attack target
				if (targetList.Count > 0){
					Character target = targetList[0];
					if (canTargetAttack(currentChar,target)){
						Point targetHexa = getPosToUseAttack(currentChar,target);
						for (int i=sequence.Count;i<currentChar.PA;i++){
							sequence.Add(new ActionAIPos(MainGame.ActionType.ATK1,new Point(targetHexa.x,targetHexa.y)));
						}
					}
				}
				currentChar.updatePos2(charposX,charposY,hexaGrid);
			}
			
			sequence.Add(new ActionAIPos(MainGame.ActionType.SKIP,null));
			return sequence;
		}
	
		///<summary>
		/// 
		///</summary>
		///<param name = currentChar > </param>
		///<returns>  </returns>
		public static Point doAttackPos(Character currentChar,Character target){
			//Character currentChar = hexaGrid.charList[myCharID];
			//Character target = hexaGrid.charList[targetID];
			List<Point> hexas1 = hexaGrid.findAllPaths(currentChar.x,currentChar.y,currentChar.PM*(currentChar.PA-1));
			List<Point> hexas2 = getHexasWhereCharIsInRangeAttack(hexas1,currentChar,target);
			List<Point> hexas3;
			List<Point> hexas4;
			if (hexas2.Count == 0){
				hexas1 = hexaGrid.findAllPaths(currentChar.x,currentChar.y,currentChar.PM*(currentChar.PA));
				hexas2 = getHexasWhereThreatIsMin(hexas1,currentChar,target);
				hexas3 = getHexasWhereWalkingDistanceIsLowest(hexas2,target,currentChar);
				hexas4 = getHexasClosestToAllies(hexas3,target);
				return hexas4[0];
			}else{
				hexas3 = getHexasWhereMovementIsLowest(hexas2,currentChar);
				hexas4 = getHexasWhereThreatIsMin(hexas3,target,currentChar);
				List<Point> hexas5 = getHexasClosestToAllies(hexas4,currentChar);
				return hexas5[0];
			}
		}
		
		///<summary>
		/// 
		///</summary>
		///<param name = currenChar > </param>
		///<returns>  </returns>
		public static List<ActionAIPos> doAttack(Character currentChar){
			//Character currentChar = hexaGrid.charList[myCharID];
			List<ActionAIPos> sequence = new List<ActionAIPos>();
			
			// Get target
			Character target = getMainTargetAttack(currentChar); if (target == null){sequence.Add(new ActionAIPos(MainGame.ActionType.SKIP,null));return sequence;}
			// Get destination to target
			Point hexaPos = doAttackPos(currentChar,target);
			// Get path to destination
			if (!(hexaPos.x == currentChar.x && hexaPos.y == currentChar.y)){
				sequence = findSequencePathToHexa(currentChar,hexaPos.x,hexaPos.y);
			}
			
			int charposX = currentChar.x;
			int charposY = currentChar.y;
			currentChar.updatePos2(hexaPos.x,hexaPos.y,hexaGrid);
			// Attack target
			if (canTargetAttack(currentChar,target)){
				Point targetHexa = getPosToUseAttack(currentChar,target);
				for (int i=sequence.Count;i<currentChar.PA;i++){
					sequence.Add(new ActionAIPos(MainGame.ActionType.ATK1,new Point(targetHexa.x,targetHexa.y)));
				}
			}
			
			currentChar.updatePos2(charposX,charposY,hexaGrid);
			sequence.Add(new ActionAIPos(MainGame.ActionType.SKIP,null));
			return sequence;
		}
	
		///<summary>
		/// 
		///</summary>
		///<param name = currentChar > </param>
		///<returns>  </returns>
		public static Point doSkillPos(Character currentChar,Character target){
			//Character currentChar = hexaGrid.charList[currentChar];
			//Character target = hexaGrid.charList[targetID];
			List<Point> hexas1  = hexaGrid.findAllPaths(currentChar.x,currentChar.y,currentChar.PM*(currentChar.PA-1));
			List<Point> hexas2 = getHexasWhereCharIsInRangeSkill(hexas1,currentChar,target);
			List<Point> hexas3;
			List<Point> hexas4;
			if (hexas2.Count == 0){
				hexas1 = hexaGrid.findAllPaths(currentChar.x,currentChar.y,currentChar.PM*(currentChar.PA));
				hexas2 = getHexasWhereThreatIsMin(hexas1,target,currentChar);
				hexas3 = getHexasWhereWalkingDistanceIsLowest(hexas2,currentChar,target);
				hexas4 = getHexasClosestToAllies(hexas3,currentChar);
				return hexas4[0];
			}else{
				hexas3 = getHexasWhereMovementIsLowest(hexas2,currentChar);
				hexas4 = getHexasWhereThreatIsMin(hexas3,target,currentChar);
				List<Point> hexas5 = getHexasClosestToAllies(hexas4,currentChar);
				return hexas5[0];
			}
		}
		
		///<summary>
		/// 
		///</summary>
		///<param name = currentChar > </param>
		///<returns>  </returns>
		public static List<ActionAIPos> doSkill(Character currentChar){
			//Character currentChar = hexaGrid.charList[myCharID];
			List<ActionAIPos> sequence = new List<ActionAIPos>();
			
			// Get target
			Character target = getMainTargetSkill(currentChar); if (target == null){sequence.Add(new ActionAIPos(MainGame.ActionType.SKIP,null));return sequence;}
			// Get destination to target
			Point hexaPos = doSkillPos(currentChar,target);
			// Get path to destination
			if (!(hexaPos.x == currentChar.x && hexaPos.y == currentChar.y)){
				sequence = findSequencePathToHexa(currentChar,hexaPos.x,hexaPos.y);
			}
			
			int charposX = currentChar.x;
			int charposY = currentChar.y;
			currentChar.updatePos2(hexaPos.x,hexaPos.y,hexaGrid);
			// Attack target (skill)
			if (canTargetSkill(currentChar,target)){
				Point targetHexa = getPosToUseSkill(currentChar,target);
				if (sequence.Count < currentChar.PA) sequence.Add(new ActionAIPos(MainGame.ActionType.SKILL1,new Point(targetHexa.x,targetHexa.y)));
			}
			// Attack target (basic attack)
			if (canTargetAttack(currentChar,target)){
				Point targetHexa = getPosToUseAttack(currentChar,target);
				for (int i=sequence.Count;i<currentChar.PA;i++){
					sequence.Add(new ActionAIPos(MainGame.ActionType.ATK1,new Point(targetHexa.x,targetHexa.y)));
				}
			}
			
			currentChar.updatePos2(charposX,charposY,hexaGrid);
			sequence.Add(new ActionAIPos(MainGame.ActionType.SKIP,null));
			return sequence;
		}
		
		///<summary>
		/// 
		///</summary>
		///<param name = currentChar > </param>
		///<returns>  </returns>
		public static int threatIfAttack(Character currentChar){
			//Character currentChar = hexaGrid.charList[charID];
			// Get target
			Character target = getMainTargetAttack(currentChar);
			if (target == null) return threatAtHexa(currentChar.x,currentChar.y,target,currentChar);
			// Get destination to target
			Point hexaPos = doAttackPos(currentChar,target);
			return threatAtHexa(hexaPos.x,hexaPos.y,target,currentChar);
		}
		
		///<summary>
		/// 
		///</summary>
		///<param name = currentChar > </param>
		///<returns>  </returns>
		public static int threatIfSkill(Character currentChar){
			//Character currentChar = hexaGrid.charList[charID];
			// Get target
			Character target = getMainTargetSkill(currentChar);
			if (target == null) return threatAtHexa(currentChar.x,currentChar.y,target,currentChar);
			// Get destination to target
			Point hexaPos = doSkillPos(currentChar,target);
			return threatAtHexa(hexaPos.x,hexaPos.y,target,currentChar);
		}
		
		///<summary>
		/// 
		///</summary>
		///<param name = currentChar > </param>
		///<returns>  </returns>
		public static int threatIfFlee(Character currentChar, Character target){
			//Character currentChar = hexaGrid.charList[charID];
			Point hexaPos = doFleePos(currentChar); //check at the end
			return threatAtHexa(hexaPos.x,hexaPos.y,target,currentChar); //check old version if it doesn't work
		}
		
		///<summary>
		/// 
		///</summary>
		///<param name = currentChar > </param>
		///<returns>  </returns>
		public static int threatEnemyIfAttack(Character currentChar){
			//Character currentChar = hexaGrid.charList[charID];
			// Get target
			Character target = getMainTargetAttack(currentChar);
			if (target == null) return 0;
			// Get destination to target
			Point hexaPos = doAttackPos(currentChar,target);
			int walkingDistanceTmp = hexaGrid.getWalkingDistance(currentChar.x,currentChar.y,hexaPos.x,hexaPos.y);
			walkingDistanceTmp = (walkingDistanceTmp == 0) ? 0 : ((walkingDistanceTmp-1)/currentChar.PM + 1);
			
			bool skillAvailable = currentChar.skillAvailable;
			int nbPA = currentChar.PA;
			currentChar.skillAvailable = false;
			currentChar.PA = currentChar.PA - walkingDistanceTmp;
			int threat = threatAtHexa(target.x,target.y,currentChar,target);
			currentChar.skillAvailable = skillAvailable;
			currentChar.PA = nbPA;
			return threat;
		}
		
		///<summary>
		/// 
		///</summary>
		///<param name = currentChar > </param>
		///<returns>  </returns>
		public static int threatEnemyIfSkill(Character currentChar){
			//Character currentChar = hexaGrid.charList[charID];
			// Get target
			Character target = getMainTargetSkill(currentChar);
			if (target == null) return 0;
			// Get destination to target
			Point hexaPos = doSkillPos(currentChar,target);
			int walkingDistanceTmp = hexaGrid.getWalkingDistance(currentChar.x,currentChar.y,hexaPos.x,hexaPos.y);
			walkingDistanceTmp = (walkingDistanceTmp == 0) ? 0 : ((walkingDistanceTmp-1)/currentChar.PM + 1);
			
			int nbPA = currentChar.PA;
			currentChar.PA = currentChar.PA - walkingDistanceTmp;
			int threat = threatAtHexa(target.x,target.y,currentChar,target);
			currentChar.PA = nbPA;
			return threat;
		}
		
		///<summary>
		/// 
		///</summary>
		///<param name = currentChar > </param>
		///<returns>  </returns>
		public static int threatEnemyIfFlee(Character currentChar){
			//Character currentChar = hexaGrid.charList[charID];
			// Get target
			Character target = getMainTargetAttack(currentChar);
			if (target == null) return threatAtHexa(currentChar.x,currentChar.y,target,currentChar);
			//return threatAtHexa(target.x,target.y,charID+1,hexaGrid.getCharID(target));
			return threatAtHexa(target.x, target.y, currentChar, target);
		}
	}
}

}