using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Hexas;
using Misc;
using Characters;
using CharactersCPU;
using AI_Util;
using Classifiers;
using Classifiers1;
using Classifiers2;
using Stats;
using AI_Class;
using static UtilCPU.UtilCPU;
using static MainGame;
using static ScriptsCPU.ScriptsCPU;

public class CPU : AI {

    /// <summary>
    /// Method where the CPU choose beetwen the offensive and defensive strategy
    /// </summary>
    /// <param name="currentTeam">The current team in play</param>
    /// <param name="hexas">The game board</param>
    /// <returns>A list of (Character, ActionAIPos) tuples</returns>
    public static List<(Character, ActionAIPos)> chooseStrategy(int currentTeam, HexaGrid hexas, Hexa caseBonus) {

        if (teamListCount(currentTeam, hexas) > 3)
            return decideDefense(currentTeam, hexas);

        return decideOffense(currentTeam, hexas, caseBonus);
    }

    /// <summary>
    /// Method where the defensive CPU decides which script/strategy to employ.
    /// </summary>
    /// <param name="currentTeam">The current team in play</param>
    /// <param name="hexas">The game board</param>
    /// <returns>A list of (Character, ActionAIPos) tuples</returns>
    public static List<(Character, ActionAIPos)> decideDefense(int currentTeam, HexaGrid hexas)
	{
        Debug.Log("decideDefense ->");

        CharactersCPU.CharacterCPU.charCPUList = new List<CharacterCPU>();
        List<(Character, ActionAIPos)> decide;
        List<CharacterCPU> enemyList = closestToFarthestEnemySorter(currentTeam, hexas);

        // Enemy team in range
        if (canTargetEnemyTeam(currentTeam, hexas))
        {
            // One enemy in range in a 4v4 or at most two enemies in a 5v5
            // & the whole team is healed
            if (teamListCount(currentTeam, hexas) - enemyInRangeCounter(currentTeam, hexas) > 3 
                && allTeamFullLife(currentTeam, hexas))
                decide = prone(currentTeam, hexas);
            else
            {
                // At least one unit has low HP
                if (healthCheck(currentTeam, hexas))
                    decide = patchUpUnit(currentTeam, hexas, enemyList);
                // Full offense
                else
                    decide = offense(currentTeam, hexas, enemyList);
            }
        }
        else
            decide = formation(currentTeam, hexas);

        Debug.Log(actionListPrinter(decide));

        return decide;
	}


    /// Edited by Rayane TALEB L3C2
	/// <summary>
	/// Method where the offensive CPU decides which script/strategy to employ.
	/// </summary>
	/// <param name="currentTeam">The current team in play</param>
	/// <param name="hexas">The game board</param>
	/// <returns>A list of (Character, ActionAIPos) tuples</returns>
	public static List<(Character chr, ActionAIPos act)> decideOffense(int currentTeam, HexaGrid hexas, Hexa caseBonus)
    {
        Debug.Log("decideOffense ->");

        CharactersCPU.CharacterCPU.charCPUList = new List<CharacterCPU>();
        List<CharacterCPU> cpuTeamList = getTeamList(currentTeam, hexas); 

		// Danger check
		foreach (CharacterCPU c in cpuTeamList)
			if (isInDanger(c.Character, hexas) && (c.Character.getPA() > 0)) {
				Debug.Log("EN DANGER !");
				if (gotHealerInTeam(currentTeam))
					return fleeWithHealer(c, hexas);
			}
        Debug.Log("class Turn->");
        // Class turn
        foreach (CharacterCPU c in cpuTeamList) {
            if (c.Character.charClass == CharClass.SOIGNEUR && (c.Character.getPA() > 0)){
                Debug.Log("Soigneur ->");
                return heal(c, currentTeam, hexas);
                
            }
            if (c.Character.charClass == CharClass.ENVOUTEUR && (c.Character.getPA() > 0)){
                Debug.Log("Envouteur ->");
                return envouteur(c, currentTeam, hexas);
                
            }
            if (c.Character.charClass == CharClass.MAGE && (c.Character.getPA() > 0)){
                Debug.Log("Mage ->");
                return mage(c, currentTeam, hexas);
                
            }
            Debug.Log("class Turn2->");
            Debug.Log("c.Character.getPA() " + c.Character.getPA());
            if (c.Character.charClass == CharClass.VOLEUR && (c.Character.getPA() > 0)){
                Debug.Log("Voleur ->");
                return voleur(c, currentTeam, hexas, caseBonus);
                
            }
            if (c.Character.charClass == CharClass.ARCHER && (c.Character.getPA() > 0)){
                Debug.Log("Archer ->");
                return archer(c, currentTeam, hexas);
            }
            if (c.Character.charClass == CharClass.VALKYRIE && (c.Character.getPA() > 0)){
                Debug.Log("Valkyrie ->");
                return valkyrie(c, currentTeam, hexas, caseBonus);
            }
            if (c.Character.charClass == CharClass.DRUIDE && (c.Character.getPA() > 0))
            {
                Debug.Log("Druide ->");
                return druide(c, currentTeam, hexas);
            }
            if (c.Character.charClass == CharClass.GUERRIER && (c.Character.getPA() > 0)){
                Debug.Log("Guerrier ->");
                return guerrier(c, currentTeam, hexas);
            }
            Debug.Log("BUG CPU");
        }
        Debug.Log("on a atteint le return cpu");
        return attack(currentTeam, hexas);
	}
}
