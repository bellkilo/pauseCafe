using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Misc;
using Hexas;
using Characters;
using AI_Util;
using AI_Class;
using Classifiers;
using Classifiers1;
using Classifiers2;
using Stats;
using static MainMenu;

//creat from MainGameConsole.cs and MainGame and edited by Zeyu TAO

public class MainStatistique : MonoBehaviour
{
	public int totalNbGames;
	public List<GameObject> textListeOfNb;
	public List<GameObject> textListeOfProba;
	public int nbGames;
	public int currentNbGames;
	public int nbTurns;

	public List<int> nbWin;
	public List<int> nbGame;
	
	public int tileW;
	public int tileH;
	public HexaGrid hexaGrid;
	public Character currentCharControlled;
	public int currentCharControlledID;
	public int winner;
	public StatsGame statsGame;
	public List<(Character, ActionAIPos)> decisionSequence;
	List<Hexa> bonusPoints;
	Hexa caseBonus;

	public int t1Wins;
	public int t2Wins;

	public int actionMoveErrs;
	public int actionAtkErrs;

	public float maxScore;
	public float lastScore;

	// Start is called before the first frame update
	void Start()
	{
		totalNbGames = 0;
		textListeOfNb = new List<GameObject>(8);
		textListeOfProba = new List<GameObject>(8);
		CharsDB.initCharsDB();
		// Init game data if it's not (it should be in the main menu)
		if (MainGame.startGameData == null)
		{
			initGame();
		}
	}

	// Update is called once per frame
	void Update()
	{
		if (Input.GetKey(KeyCode.P))
		{
			PauseGame();
		}
		if (Input.GetKey(KeyCode.C))
		{
			ResumeGame();
		}

		for (int i = 0; i < 8; i++)
		{
			textListeOfNb[i].GetComponent<Text>().text = nbWin[i] + "/" + nbGame[i];
			textListeOfProba[i].GetComponent<Text>().text = (nbWin[i] / nbGame[i]).ToString();
		}
		for (int aaa = 0; aaa < 5; aaa++)
		{ // 5 actions per frame
			if (winner == -1 && nbTurns < 400)
			{ // Max 400 turns to prevent infinite stalling
				PlayerType currentPlayerType = whoControlsThisChar(currentCharControlled);
				// decide what to do
				if (decisionSequence.Count == 0)
				{
					switch (currentPlayerType)
					{
						case PlayerType.HUMAN:
						case PlayerType.AI_CPU_Defense: decisionSequence = CPU.decideDefense(currentCharControlled.team, hexaGrid); break;
						case PlayerType.AI_CPU_Offense: decisionSequence = CPU.decideOffense(currentCharControlled.team, hexaGrid, caseBonus); break;
					}
					// do action after the decision is taken
				}
				else
				{
					ActionAIPos actionAIPos = decisionSequence[0]; 
					decisionSequence.RemoveAt(0);
					//Debug.Log(currentPlayerType + " : " + actionAIPos.action + ((actionAIPos.pos != null) ? (" " + actionAIPos.pos.x + " " + actionAIPos.pos.y) : ""));
					switch (actionAIPos.action)
					{
						case MainGame.ActionType.MOVE: actionMove(hexaGrid.getHexa(actionAIPos.pos)); break;
						case MainGame.ActionType.ATK1: actionUseAttack(actionAIPos.action, hexaGrid.getHexa(actionAIPos.pos)); break;
						case MainGame.ActionType.SKILL1:
							actionUseAttack(actionAIPos.action, hexaGrid.getHexa(actionAIPos.pos));
							break;
						case MainGame.ActionType.SKILL2:
							actionUseAttack(actionAIPos.action, hexaGrid.getHexa(actionAIPos.pos));
							break;
						case MainGame.ActionType.SKIP: { currentCharControlled.PA = 0; nextTurn(); } break;
						default: break;
					}
				}
				// end of all the games (go back to main menu by pressing A)
			}
			else if (winner == 10)
			{
				
				
				// next game (reset) (hold A at the end of a game to go back to main menu)
			}
			else if (winner == 11)
			{
				initGame();
			}
			else
			{
				if (winner != -1)
				{
					// increase the number each character wins
					statsGame.endGame(winner, hexaGrid);
					if (winner == 0)
					{
						t1Wins++;
						totalNbGames++;
						foreach (CharClass c in MainGame.startGameData.charsTeam1)
                        {
	                        //GUERRIER, VOLEUR, ARCHER, MAGE, SOIGNEUR, ENVOUTEUR, VALKYRIE, DRUIDE
	                        nbWin[(int)c]++;
                        }
					}
					else
					{
						t2Wins++;
						totalNbGames++;
						foreach (CharClass c in MainGame.startGameData.charsTeam1)
						{
							//GUERRIER, VOLEUR, ARCHER, MAGE, SOIGNEUR, ENVOUTEUR, VALKYRIE, DRUIDE
							nbWin[(int)c]++;
						}
					}
				}
				currentNbGames++;
				if (currentNbGames == nbGames) winner = 10;
				else winner = 11;
			}
		}

	}

	// ##################################################################################################################################################
	// Functions used in main
	// ##################################################################################################################################################

	PlayerType whoControlsThisChar(Character c)
	{
		return (c.team == 0) ? MainGame.startGameData.player1Type : MainGame.startGameData.player2Type;
	}

	void actionMove(Hexa hexaDestination)
	{
		if (hexaDestination != null && hexaDestination.type == HexaType.GROUND)
		{
			List<Point> path = hexaGrid.findShortestPath(currentCharControlled.x, currentCharControlled.y, hexaDestination.x, hexaDestination.y, currentCharControlled.PM);
			if (path != null && path.Count > 1)
			{
				currentCharControlled.updatePos2(hexaDestination.x, hexaDestination.y, hexaGrid);
				nextTurn();
			}
			else if (whoControlsThisChar(currentCharControlled) != PlayerType.AI_EASY) actionMoveErrs++;
		}
		else if (whoControlsThisChar(currentCharControlled) != PlayerType.AI_EASY) actionMoveErrs++;
	}

	// must trust the AI to choose right
	void actionMoveNoCheck(Hexa hexaDestination)
	{
		currentCharControlled.updatePos2(hexaDestination.x, hexaDestination.y, hexaGrid);
		nextTurn();
	}

	void actionUseAttack(MainGame.ActionType attack, Hexa hexaDestination)
	{
		//Set the attack used
		CharsDB.Attack attackUsed_;
		if (attack == MainGame.ActionType.ATK1)
		{
			attackUsed_ = CharsDB.list[(int)currentCharControlled.charClass].basicAttack;
		}
		else if (attack == MainGame.ActionType.SKILL1)
		{
			attackUsed_ = CharsDB.list[(int)currentCharControlled.charClass].skill_1;
		}
		else
		{
			attackUsed_ = CharsDB.list[(int)currentCharControlled.charClass].skill_2;
		}

		if (hexaDestination != null && hexaGrid.hexaInSight(currentCharControlled.x, currentCharControlled.y, hexaDestination.x, hexaDestination.y, attackUsed_.range))
		{
			if (attack == MainGame.ActionType.SKILL1)
			{
				currentCharControlled.skillAvailable = false;
			}
			if (attack == MainGame.ActionType.SKILL2) // edited by L3C2 Rayane TALEB
			{
				currentCharControlled.skill2Available = false;
				actionType = MainGame.ActionType.SKILL1;
			}
		}
		else if (whoControlsThisChar(currentCharControlled) != PlayerType.AI_EASY) actionAtkErrs++;

		List<Character> hits = hexaGrid.getCharWithinRange(hexaDestination.x, hexaDestination.y, attackUsed_.rangeAoE);
		// Filter target(s)
		if (attackUsed_.targetsEnemies == false)
		{
			for (int i = 0; i < hits.Count; i++)
			{
				if (hits[i].team != currentCharControlled.team)
				{
					hits.RemoveAt(i); i--;
				}
			}
		}
		if (attackUsed_.targetsAllies == false)
		{
			for (int i = 0; i < hits.Count; i++)
			{
				if (hits[i].team == currentCharControlled.team)
				{
					hits.RemoveAt(i); i--;
				}
			}
		}
		if (attackUsed_.targetsSelf == false)
		{
			for (int i = 0; i < hits.Count; i++)
			{
				if (hits[i] == currentCharControlled)
				{
					hits.RemoveAt(i); i--;
				}
			}
		}
		foreach (Character c in hits)
		{
			//If the attack effect is damage, give damages
			switch (attackUsed_.attackEffect)
			{
				case CharsDB.AttackEffect.DAMAGE:
					useAttackDamageCase(c);
					break;
				//If the attack effect is healing, heal
				case CharsDB.AttackEffect.HEAL:
					useAttackHealCase(c);
					break;
				//If the attack effect is PA buffing, buff it
				case CharsDB.AttackEffect.PA_BUFF:
					useAttackPABuffCase(c);
					break;
				//If the attack effect is PM buffing, buff it
				// edited by Rayane TALEB L3C2
				case CharsDB.AttackEffect.PM_BUFF:
					useAttackPMBuffCase(c);
					break;
				//If the attack effect is damage buffing, buff it
				case CharsDB.AttackEffect.DMG_BUFF:
					useAttackDmgBuffCase(c);
					break;
				// edited by Rayane TALEB L3C2
				case CharsDB.AttackEffect.STUN:
					useAttackStunCase(c);
					break;
				// Author Rayane TALEB L3C2
				case CharsDB.AttackEffect.POISON:
					useAttackPoisonCase(c);
					break;
				// Author Rayane TALEB L3C2
				case CharsDB.AttackEffect.ROOT:
					useAttackRootCase(c);
					break;
			}
		}
		nextTurn();
	}

	void nextTurn()
	{
		currentCharControlled.PA--;

		int reset = 0;
		int charPA = 0;

		// Next char turn
		if (currentCharControlled.PA <= 0)
		{
			nbTurns++;
			//currentCharControlled.PA = CharsDB.list[(int)currentCharControlled.charClass].basePA;
			do
			{

				foreach (Character c in hexaGrid.charList)
				{
					if (c.team == currentCharControlled.team && c != currentCharControlled)
					{
						if (c.PA > 0)
						{
							currentCharControlled = c;
							charPA = 1;
						}
					}
				}
				Debug.Log(charPA);
				if (charPA == 0)
				{
					reset = 1;
					foreach (Character c in hexaGrid.charList)
					{
						if (c.team != currentCharControlled.team && c.PA > 0)
						{
							currentCharControlled = c;
						}
					}
				}
				if (reset == 1)
				{
					foreach (Character c in hexaGrid.charList)
					{
						if (c.team != currentCharControlled.team)
						{
							c.PA = CharsDB.list[(int)c.charClass].basePA;
						}
					}
				}

			} while (currentCharControlled.HP <= 0);
			PlayerType currentPlayerType = whoControlsThisChar(currentCharControlled);
			if (currentPlayerType == PlayerType.AI_HARD || currentPlayerType == PlayerType.AI_MEDIUM)
			{
				statsGame.nextTurn(currentCharControlled);
			}
			decisionSequence = new List<ActionAIPos>();

			if (currentCharControlled.totalDamage >= 10)
			{
				currentCharControlled.totalDamage -= 10;
				currentCharControlled.skillAvailable = true;
			}
		}
	}

	void initGame()
    {
		MainGame.startGameData = new StartGameData();
		MainGame.startGameData.loadSave = false;
		Random rand = new Random();
		rand.Next(0, 8);
		MainGame.startGameData.charsTeam1 = new List<CharClass>();
		MainGame.startGameData.charsTeam2 = new List<CharClass>();
		for (int i = 0; i < 5; i++)
        {
			MainGame.startGameData.charsTeam1.Add((CharClass)rand.Next(0, 8));
			MainGame.startGameData.charsTeam2.Add((CharClass)rand.Next(0, 8));
		}
		MainGame.startGameData.player1Type = PlayerType.AI_HARD;
		MainGame.startGameData.player2Type = PlayerType.AI_HARD;
		MainGame.startGameData.mapChosen = 1;
		MainGame.startGameData.nbGames = 10;

		initHexas();

		// Put characters on the grid
		for (int i = 0; i < 5; i++)
		{
			if (i < MainGame.startGameData.charsTeam1.Count) hexaGrid.addChar2(MainGame.startGameData.charsTeam1[i], tileW / 2 - 4 + 2 + i, 2, 0);
			if (i < MainGame.startGameData.charsTeam2.Count) hexaGrid.addChar2(MainGame.startGameData.charsTeam2[i], tileW / 2 - 4 + 2 + i, tileH - 2, 1);
		}
		foreach (Character c in hexaGrid.charList) hexaGrid.getHexa(c.x, c.y).changeType2(HexaType.GROUND);
		currentCharControlledID = 0;
		currentCharControlled = hexaGrid.charList[currentCharControlledID];
		currentNbGames = 0;
		nbGames = MainGame.startGameData.nbGames;

		// Init AI
		decisionSequence = new List<ActionAIPos>();
		AI.hexaGrid = hexaGrid;
		// Init AIHard classifiers
		if (AIHard.rules == null)
		{
			AIHard.rules = new ClassifierSystem<Classifier1>();
			AIHard.rules.loadAllInBinary();
			Debug.Log("Loaded " + AIHard.rules.classifiers.Count + " Classifiers HARD.");
		}
	}

	// Init hexa grid
	void initHexas()
    {
		hexaGrid = new HexaGrid();
		if (MainGame.startGameData.mapChosen == 0)
		{
			hexaGrid.createGridFromFile2(Application.dataPath + "/Data/Map/ruins");
			tileW = hexaGrid.w; tileH = hexaGrid.h;
		}
		else if (MainGame.startGameData.mapChosen >= 1)
		{
			hexaGrid.createRandomRectGrid2(tileW, tileH);
		}
		caseBonus = MainGame.initBonus();
	}

	//Give the damage effect
	//Author : Rayane TALEB, L3C2
	void useAttackDamageCase(Character c)
	{
		/*if (whoControlsThisChar(c) == PlayerType.AI_HARD) statsGame.addToDamageTaken(c, attackUsedAttack.effectValue);
        if (whoControlsThisChar(currentCharControlled) == PlayerType.AI_HARD) statsGame.addToDamageDealt(currentCharControlled, attackUsedAttack.effectValue);*/

		if (currentCharControlled.dmgbuff == true)
		{
			//Debug.Log("Action Type = " + actionType);
			// Check if VOLEUR steal HP Edited by Rayane TALEB L3C2
			if (currentCharControlled.charClass == CharClass.VOLEUR && actionType == MainGame.ActionType.SKILL1) //edited by rayane
			{
				currentCharControlled.HP += 1;
			}

			// Check if GUERRIER steal PA Edited by Rayane TALEB L3C2
			if (currentCharControlled.charClass == CharClass.GUERRIER && actionType == MainGame.ActionType.SKILL1) //edited by rayane
			{
				currentCharControlled.PA += 1;
			}
			currentCharControlled.totalDamage += (attackUsedAttack.effectValue + 1);
			currentCharControlled.totalDamage2 += (attackUsedAttack.effectValue + 1);// edited by L3C2 Rayane TALEB
			c.HP -= (attackUsedAttack.effectValue + 1);
		}
		else
		{
			if (currentCharControlled.charClass == CharClass.VOLEUR && actionType == MainGame.ActionType.SKILL1) // edited by L3C2 Rayane TALEB
			{
				currentCharControlled.HP += 1;
			}
			if (currentCharControlled.charClass == CharClass.GUERRIER && actionType == MainGame.ActionType.SKILL1) // edited by L3C2 Rayane TALEB
			{
				currentCharControlled.PA += 1;
			}
			currentCharControlled.totalDamage += attackUsedAttack.effectValue;
			currentCharControlled.totalDamage2 += attackUsedAttack.effectValue;
			c.HP -= attackUsedAttack.effectValue;
		}
		// Enemy dies
		if (c.HP <= 0)
		{
			/*if (whoControlsThisChar(c) == PlayerType.AI_HARD) statsGame.setDead(c, true);
            if (whoControlsThisChar(currentCharControlled) == PlayerType.AI_HARD) statsGame.addToKills(currentCharControlled, 1);*/
			c.HP = 0;
			hexaGrid.getHexa(c.x, c.y).charOn = null;
			GameObject.Destroy(c.go);
			for (int i = 0; i < hexaGrid.charList.Count; i++)
			{
				if (hexaGrid.charList[i] == c)
				{
					GameObject.Destroy(UICharTurnsList[i]);
					UICharTurnsList.RemoveAt(i);
					hexaGrid.charList.RemoveAt(i);
				}
			}
			// update currentCharControlled ID
			for (int i = 0; i < hexaGrid.charList.Count; i++)
			{
				if (hexaGrid.charList[i] == currentCharControlled) currentCharControlledID = i;
			}
			// force AI to make a new decision
			decisionSequence = new List<ActionAIPos>();
			// check if there is a winner
			int nbT1 = 0;
			int nbT2 = 0;
			foreach (Character c2 in hexaGrid.charList)
			{
				if (c2.team == 0) nbT1++;
				else nbT2++;
			}
			if (nbT1 == 0) winner = 1;
			else if (nbT2 == 0) winner = 0;
		}
	}

	//Give the heal effect
	//Author : Rayane TALEB, L3C2

	void useAttackHealCase(Character c)
	{
		int heal = attackUsedAttack.effectValue;
		if (heal > c.HPmax - c.HP) heal = c.HPmax - c.HP;
		/*if (whoControlsThisChar(currentCharControlled) == PlayerType.AI_HARD) statsGame.addToHeal(currentCharControlled, heal);*/
		if (heal > 5)
		{
			currentCharControlled.totalDamage += heal / 2;
			currentCharControlled.totalDamage2 += heal / 2;
			c.HP += heal;
		}
		else
		{
			currentCharControlled.totalDamage += heal;
			currentCharControlled.totalDamage2 += heal;// edited by L3C2 Rayane TALEB
			c.HP += heal;
		}
	}

	//Give the pa buff effect
	//Author : Rayane TALEB, L3C2

	void useAttackPABuffCase(Character c)
	{
		Debug.Log("1: " + c.getPA() + "\n2: " + c.getClassData().basePA);
		if (c.characterPA == c.getClassData().basePA)
		{
			/*if (whoControlsThisChar(currentCharControlled) == PlayerType.AI_HARD) statsGame.addToDamageDealt(currentCharControlled, attackUsedAttack.effectValue);*/
			currentCharControlled.totalDamage += attackUsedAttack.effectValue;
			currentCharControlled.totalDamage2 += attackUsedAttack.effectValue;
			c.isBuffPA = true;// edited by L3C2 Rayane TALEB
		}
	}

	//Give the pm buff effect
	//Author : Rayane TALEB, L3C2

	void useAttackPMBuffCase(Character c)
	{
		Debug.Log("1: " + c.getPM() + "\n2: " + c.getClassData().basePM);
		if (c.characterPM == c.getClassData().basePM)
		{
			currentCharControlled.totalDamage += attackUsedAttack.effectValue;
			currentCharControlled.totalDamage2 += attackUsedAttack.effectValue;
			c.isBuffPM = true;
		}
	}

	//Give the Damage Buff effect
	//Author : Rayane TALEB, L3C2
	void useAttackDmgBuffCase(Character c)
	{
		if (c.dmgbuff == false)
		{
			c.dmgbuff = true;
		}
	}

	//Give the stun effect
	//Author : Rayane TALEB, L3C2
	void useAttackStunCase(Character c)
	{
		currentCharControlled.totalDamage += attackUsedAttack.effectValue * 2;
		currentCharControlled.totalDamage2 += attackUsedAttack.effectValue * 2;
		c.HP -= attackUsedAttack.effectValue;
		c.isStun = true;
	}

	//Give the poison effect
	//Author : Rayane TALEB, L3C2
	void useAttackPoisonCase(Character c)
	{
		c.isPoisoned = true;
		c.cptPoison += 1;
		currentCharControlled.totalDamage += attackUsedAttack.effectValue;
		currentCharControlled.totalDamage2 += attackUsedAttack.effectValue * 2;
		c.HP -= attackUsedAttack.effectValue;
	}

	//Give the root effect
	//Author : Rayane TALEB, L3C2
	void useAttackRootCase(Character c)
	{
		c.isRooted = true;
		currentCharControlled.totalDamage += attackUsedAttack.effectValue * 2;
		currentCharControlled.totalDamage2 += attackUsedAttack.effectValue * 2;
		c.HP -= attackUsedAttack.effectValue;
	}

	void PauseGame()
	{
		Time.timeScale = 0;
	}

	void ResumeGame()
	{
		Time.timeScale = 1;
	}
}