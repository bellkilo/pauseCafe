using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Misc;
using Characters;
using CharactersCPU;
using static ScriptsCPU.ScriptsCPU;
using static MainGame;
using Hexas;
using AI_Util;
using AI_Class;
using System.Linq;


namespace UtilCPU
{

    public class UtilCPU
    {
        /*
         * Cette méthode est redondante avec isInRangeToAttack/isInRangeToUseSkill
            public static bool isAdjacent(HexaGrid grid, Character c, Character target)
            { //boolean?
                return (grid.getDistance(c.getX(), c.getY(), target.getX(), target.getY()) == 1);
            }
        */


        /// <summary>
        /// TODO: Incomplete
        /// Verifies if a unit can be killed by the whole enemy team by calculating the best
        /// action sequence to deal damage to a unit with the available units, and stores that sequence in a variable.
        /// </summary>
        /// <param name="group">The team attacking</param>
        /// <param name="target">The target of the attacks</param>
        /// <param name="decide">The modified action sequence</param>
        /// <returns>True if the unit can be killed, false else</returns>
        public static bool canGroupKill(List<Character> group, Character target, List<(Character, ActionAIPos)> decide)
        {
            return maxDamageToTarget(group, target, decide) >= target.getHP(); //define getHP() in Characters.cs
        }


        /// <summary>
        /// TODO: Incomplete
        /// Verifies if a unit can be killed by the whole enemy team by calculating the best
        /// action sequence to deal damage to a unit with the available units.
        /// </summary>
        /// <param name="group">The team attacking</param>
        /// <param name="target">The target of the attacks</param>
        /// <returns>True if the unit can be killed, false else</returns>
        public static bool canGroupKill(List<Character> group, Character target)
        {
            int damageToTarget = maxDamageToTarget(group, target);
            Debug.Log("MaxDamageToTarget: " + damageToTarget);
            return damageToTarget >= target.getHP(); //define getHP() in Characters.cs
        }


        /// <summary>
        /// TODO: Incomplete
        /// Verifies if a unit can be killed by the whole enemy team by calculating the best
        /// action sequence to deal damage to a unit with the available units.
        /// </summary>
        /// <param name="group">The team attacking</param>
        /// <param name="target">The target of the attacks</param>
        /// <returns>True if the unit can be killed, false else</returns>
        public static bool canGroupKill(int maxDamageToTarget, Character target)
        {
            return maxDamageToTarget >= target.getHP();
        }




        /// <summary>
        /// TODO: Incomplete
        /// Calculates how much damage a team can deal with its available units 
        /// to a target by calculating the best action sequence and stores that sequence in a variable.
        /// </summary>
        /// <param name="group">The team attacking</param>
        /// <param name="target">The target of the attacks</param>
        /// <param name="actions">The modified action sequence</param>
        /// <returns>The maximum amount damage dealt to that unit</returns>
        public static int maxDamageToTarget(List<Character> group, Character target, List<(Character, ActionAIPos)> actions)
        { //boolean groupKill

            List<Character> tempGroup;
            List<Character> envouteurList;
            List<Character> envouteurSkillRdyList;

            envouteurList = new List<Character>();
            envouteurSkillRdyList = new List<Character>();
            tempGroup = new List<Character>();
            actions.Clear();

            foreach (Character c in group)
            {
                // Le CPU pourrait aussi bien être le joueur 1 que le joueur 2
                Character tempChar = (new Character(c.getCharClass(), c.getX(), c.getY(), c.team, false));
                tempChar.setTotalDamage(c.getTotalDamage());
                tempChar.setTotalDamage2(c.getTotalDamage());
                tempChar.setHP(c.getHP());
                tempChar.setPA(c.getPA());
                tempGroup.Add(tempChar);
            }

            for (int i = 0; i < tempGroup.Count; i++)
            {
                if (tempGroup[i].getCharClass() == CharClass.ENVOUTEUR)
                {
                    if (tempGroup[i].isSkillUp())
                        envouteurSkillRdyList.Add(tempGroup[i]);
                    else envouteurList.Add(tempGroup[i]);
                }
            }



            //SKILL IS NOT READY --- not needed because envouteurs only get 1 skill per game and cant refresh it 
            /*for (int i=0;i<envouteurList.Count;i++) {

                setBestEnvouteurAttackVsTarget(envouteurList[i], target, tempGroup);

                if (envouteurList[i].getPA() > 0 && envouteurList[i].isSkillUp()) {
                    envouteurSkillRdyList.Add(envouteurList[i]);
                }
            }*/

            //SKILL IS READY
            for (int i = 0; i < envouteurSkillRdyList.Count; i++)
            {
                setBestEnvouteurSkillVsTarget(envouteurSkillRdyList[i], target, tempGroup, actions);
            }

            //SKILL IS NOT READY -OR- EXTRA PA AFTER USING SKILL
            for (int i = 0; i < envouteurList.Count; i++)
            {
                if (envouteurList[i].getPA() > 0)
                {
                    setBestEnvouteurAttackVsTarget(envouteurList[i], target, tempGroup, actions);
                }
            }

            //CALCULATING TEAM DAMAGE ON TARGET
            int totalDamage;
            Point shortestDist;
            // Nommer cette variable PA créé un conflit de noms avec Character
            int tempPA, tempSP;
            Point tempPos;

            totalDamage = 0;

            foreach (Character c in tempGroup)
            {
                if ((c.getCharClass() != CharClass.SOIGNEUR) && (c.getCharClass() != CharClass.ENVOUTEUR))
                {

                    tempPA = c.getPA();
                    tempSP = c.getTotalDamage();

                    tempPos = new Point(c.getX(), c.getY());

                    while (tempPA-- > 0)
                    {

                        if (tempSP >= 10 && isInRangeToUseSkill(c, tempPos, target))
                        {
                            totalDamage += c.getSkillDamage() + (c.getDamageBuff() ? 1 : 0);
                            tempSP = 0;
                            //actions.Add(null); //temp
                        }
                        else if (isInRangeToAttack(c, tempPos, target))
                        {
                            totalDamage += c.getDamage() + (c.getDamageBuff() ? 1 : 0);
                            tempSP += c.getDamage() + (c.getDamageBuff() ? 1 : 0);
                            //actions.Add(null); //temp
                        }
                        else
                        {
                            shortestDist = getShortestDistPoint(tempPos, target);
                            if (shortestDist != null) tempPos = shortestDist;
                            //actions.Add(null); //temp
                        }
                    }
                }
            }
            return totalDamage;
        }




        /// <summary>
        /// TODO: Incomplete
        /// Calculates how much damage a team can deal with its available units 
        /// to a target by calculating the best action sequence.
        /// </summary>
        /// <param name="group">The team attacking</param>
        /// <param name="target">The target of the attacks</param>
        /// <returns>The maximum amount damage dealt to that unit</returns>
        public static int maxDamageToTarget(List<Character> group, Character target)
        { //boolean groupKill

            List<Character> tempGroup;
            List<Character> envouteurList;
            List<Character> envouteurSkillRdyList;

            envouteurList = new List<Character>();
            envouteurSkillRdyList = new List<Character>();
            tempGroup = new List<Character>();

            foreach (Character c in group)
            {
                // Le CPU pourrait aussi bien être le joueur 1 que le joueur 2
                Character tempChar = (new Character(c.getCharClass(), c.getX(), c.getY(), c.team, false));
                tempChar.setTotalDamage(c.getTotalDamage());
                tempChar.setHP(c.getHP());
                tempChar.setPA(c.getPA());
                tempGroup.Add(tempChar);
            }

            for (int i = 0; i < tempGroup.Count; i++)
            {
                if (tempGroup[i].getCharClass() == CharClass.ENVOUTEUR)
                {
                    if (tempGroup[i].isSkillUp())
                        envouteurSkillRdyList.Add(tempGroup[i]);
                    else envouteurList.Add(tempGroup[i]);
                }
            }

            //SKILL IS READY
            for (int i = 0; i < envouteurSkillRdyList.Count; i++)
            {
                setBestEnvouteurSkillVsTarget(envouteurSkillRdyList[i], target, tempGroup);
            }

            //SKILL IS NOT READY -OR- EXTRA PA AFTER USING SKILL
            for (int i = 0; i < envouteurList.Count; i++)
            {
                if (envouteurList[i].getPA() > 0)
                {
                    setBestEnvouteurAttackVsTarget(envouteurList[i], target, tempGroup);
                }
            }

            //CALCULATING TEAM DAMAGE ON TARGET
            int totalDamage;
            Point shortestDist;
            // Nommer cette variable PA créé un conflit de noms avec Character
            int tempPA, tempSP;
            Point tempPos;

            totalDamage = 0;

            foreach (Character c in tempGroup)
            {
                if ((c.getCharClass() != CharClass.SOIGNEUR) && (c.getCharClass() != CharClass.ENVOUTEUR))
                {

                    tempPA = c.getPA();
                    tempSP = c.getTotalDamage();

                    tempPos = new Point(c.getX(), c.getY());

                    while (tempPA-- > 0)
                    {

                        if (tempSP >= 10 && isInRangeToUseSkill(c, tempPos, target))
                        {
                            totalDamage += c.getSkillDamage() + (c.getDamageBuff() ? 1 : 0);
                            tempSP = 0;
                        }
                        else if (isInRangeToAttack(c, tempPos, target))
                        {
                            totalDamage += c.getDamage() + (c.getDamageBuff() ? 1 : 0);
                            tempSP += c.getDamage() + (c.getDamageBuff() ? 1 : 0);
                        }
                        else
                        {
                            shortestDist = getShortestDistPoint(tempPos, target);
                            if (shortestDist != null) tempPos = shortestDist;
                        }
                    }
                }
            }
            //Debug.Log("MaxDamageToTarget: " + totalDamage + "Target HP: "+target.HP );
            return totalDamage;
        }




        /// <summary>
        /// Function used by maxDamageToTarget
        /// Calculates how much damage an additional AP would give to a unit  
        /// </summary>
        /// <param name="c">The character attacking</param>
        /// <param name="target">The target of the attack</param>
        /// <returns>The damage dealt with an extra AP to that unit</returns>
        public static int calcUnitExtraPADamage(Character c, Character target)
        {
            int PA, SP;
            Point pos, shortestDist;

            PA = c.getPA();
            SP = c.getTotalDamage();
            pos = new Point(c.getX(), c.getY());

            while (PA-- > 0)
            {

                // Définir la méthode avec un pos au lieu d'un personnage
                if (SP >= 10 && isInRangeToUseSkill(c, pos, target))
                {
                    SP = 0;
                }
                else if (isInRangeToAttack(c, pos, target))
                {
                    SP += c.getDamage() + (c.getDamageBuff() ? 1 : 0);
                }
                else
                {
                    shortestDist = getShortestDistPoint(pos, target);
                    if (shortestDist != null) pos = shortestDist;
                }
            }


            if (SP >= 10 && isInRangeToUseSkill(c, pos, target))
            {
                return c.getSkillDamage() + (c.getDamageBuff() ? 1 : 0);
            }
            else if (isInRangeToAttack(c, pos, target))
            {
                return c.getDamage() + (c.getDamageBuff() ? 1 : 0);
            }
            else
            {
                return 0;
            }
        }




        /// <summary>
        /// Function used by maxDamageToTarget
        /// Calculates what would be the best Hexa to use the buff skill on
        /// to maximise damage on a target, and buffes those units. 
        /// Also modifies the sequence of action required (e.g. moving) and stores it.
        /// </summary>
        /// <param name="envouteur">The sorcerer buffing</param>
        /// <param name="target">The target of the attacks</param>
        /// <param name="team">The envouteur's team</param>
        /// <param name="actions">The sequence of actions to modify</param>
        /// <returns>True if a unit can be buffed, false else</returns>
        public static bool setBestEnvouteurSkillVsTarget(Character envouteur, Character target, List<Character> team, List<(Character, ActionAIPos)> actions)
        {
            if (envouteur.getPA() > 0 && envouteur.isSkillUp())
            {
                int unitExtraPADamage;
                List<Character> maxDamageDealer;
                List<int> maxDamage;

                maxDamageDealer = new List<Character>();
                maxDamage = new List<int>();
                maxDamage.Add(0);
                unitExtraPADamage = 0;

                //CALCULATING INDIVIDUAL DAMAGE ON THE TARGET AN EXTRA PA WOULD GIVE
                foreach (Character c in team)
                {
                    if ((c.getCharClass() != CharClass.SOIGNEUR) && (c.getCharClass() != CharClass.ENVOUTEUR))
                    {
                        unitExtraPADamage = calcUnitExtraPADamage(c, target);
                        int j = -1;

                        while (unitExtraPADamage < maxDamage[++j]) ;

                        maxDamage.Insert(j, unitExtraPADamage);
                        maxDamageDealer.Insert(j, c);
                    }
                }
                if (maxDamage[0] != 0)
                {
                    List<Character> charList;
                    Hexa hex;

                    hex = takeAOEIntoAccount(envouteur, target, envouteur.getClassData().skill_1,
                            new Pair<List<Character>, List<int>>(maxDamageDealer, maxDamage), ActionType.SKILL1);
                    charList = AIUtil.hexaGrid.getCharWithinRange(hex.x, hex.y, envouteur.getClassData().skill_1.rangeAoE);

                    foreach (Character c in charList)
                    {
                        if (!System.Object.ReferenceEquals(c, envouteur))
                        {
                            c.setPA(c.getPA() + 1);
                        }
                    }

                    envouteur.setPA(envouteur.getPA() - 1);
                    //actions.Add(null); //temp

                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }


        /// <summary>
        /// Function used by maxDamageToTarget
        /// Calculates what would be the best unit to buff with ATK1
        /// to maximise damage on a target, and buffes that unit. 
        /// Also modifies the sequence of action required (e.g. moving) and stores it.
        /// </summary>
        /// <param name="envouteur">The sorcerer buffing</param>
        /// <param name="target">The target of the attacks</param>
        /// <param name="team">The envouteur's team</param>
        /// <param name="actions">The sequence of actions to modify</param>
        /// <returns>True if a unit can be buffed, false else</returns>
        public static bool setBestEnvouteurAttackVsTarget(Character envouteur, Character target, List<Character> team, List<(Character, ActionAIPos)> actions)
        {

            if (envouteur.getPA() > 0)
            {
                int PA, envouteurPA, j;
                Point pos;
                bool moved;

                moved = false;
                pos = new Point(0, 0);
                List<Character> attackChars = new List<Character>();
                List<int> attackCount = new List<int>();
                attackCount.Add(0);

                List<Character> attackCharsMoved = new List<Character>();
                List<int> attackCountMoved = new List<int>();
                attackCountMoved.Add(0);

                //CALCULATING THE NUMBER OF TIME EACH UNIT ATTACKS THE TARGET
                foreach (Character c in team)
                {
                    if (c.getDamageBuff() == false && (c.getCharClass() != CharClass.ENVOUTEUR) && (c.getCharClass() != CharClass.SOIGNEUR))
                    {

                        //---
                        envouteurPA = envouteur.getPA();
                        pos.x = envouteur.getX();
                        pos.y = envouteur.getY();

                        if (!isInRangeToAttack(envouteur, c))
                        {
                            pos = getShortestDistPoint(pos, c); //simule le déplacement de l'envouteur, pour voir s'il est possible d'être à portée de buff
                            if (isInRangeToAttack(envouteur, pos, c))
                            {
                                moved = true;
                            }
                        }
                        if (envouteurPA == envouteur.getPA() || moved)
                        {
                            pos.x = c.getX();
                            pos.y = c.getY();
                            PA = c.getPA();

                            while (PA > 0 && !isInRangeToAttack(c, pos, target))
                            {
                                pos = getShortestDistPoint(pos, target); //simule le déplacement de l'unité, pour voir s'il est possible d'être à portée d'attaque
                                PA--;
                            }

                            if (isInRangeToAttack(c, pos, target))
                            {

                                j = -1;

                                if (moved)
                                {
                                    while (PA < attackCountMoved[++j]) ;

                                    attackCharsMoved.Insert(j, c);
                                    attackCountMoved.Insert(j, PA);
                                }
                                else
                                {
                                    while (PA < attackCount[++j]) ;

                                    attackChars.Insert(j, c);
                                    attackCount.Insert(j, PA);
                                }
                            }
                        }
                    }
                }

                //---
                if (attackCharsMoved.Count != 0 && attackChars.Count != 0)
                {

                    if (attackCharsMoved.Count == 0)
                    {
                        for (j = 0; j < envouteur.getPA() && j < attackChars.Count; j++)
                        {
                            attackChars[j].setDamageBuff(true);
                            //actions.Add(null); //temp
                        }
                        envouteur.setPA(envouteur.getPA() - j);
                    }
                    else if (attackChars.Count == 0)
                    {
                        pos.x = envouteur.getX();
                        pos.y = envouteur.getY();


                        j = 0; //j: nbBuffed
                        while (j < envouteur.getPA() && j < attackCharsMoved.Count)
                        {
                            if (isInRangeToAttack(envouteur, pos, attackCharsMoved[j]))
                            {
                                attackCharsMoved[j].setDamageBuff(true);
                                j++;
                                //actions.Add(null); //temp: buffing
                            }
                            else
                            {
                                pos = getShortestDistPoint(pos, attackCharsMoved[j]);
                                envouteur.setPA(envouteur.getPA() - 1);
                                //actions.Add(null); //temp: movement
                            }
                        }

                        envouteur.setPA(envouteur.getPA() - j);
                    }
                    else
                    {
                        //WITHIN RANGE FIRST
                        for (j = 0; j < envouteur.getPA() && j < attackChars.Count; j++)
                        {
                            attackChars[j].setDamageBuff(true);
                            //actions.Add(null); //temp   
                        }
                        envouteur.setPA(envouteur.getPA() - j);

                        //OUTSIDE RANGE
                        pos.x = envouteur.getX();
                        pos.y = envouteur.getY();

                        j = 0; //j: nbBuffed
                        while (j < envouteur.getPA() && j < attackCharsMoved.Count)
                        {
                            if (isInRangeToAttack(envouteur, pos, attackCharsMoved[j]))
                            {
                                attackCharsMoved[j].setDamageBuff(true);
                                j++;
                                //actions.Add(null); //temp: buffing
                            }
                            else
                            {
                                pos = getShortestDistPoint(pos, attackCharsMoved[j]);
                                envouteur.setPA(envouteur.getPA() - 1);
                                //actions.Add(null); //temp: movement
                            }
                        }
                        envouteur.setPA(envouteur.getPA() - j);
                    }
                }
                else
                {
                    return false;
                }

                return true;
            }
            else
            {
                return false;
            }
        }


        /// <summary>
        /// Function used by maxDamageToTarget
        /// Calculates what would be the best Hexa to use the buff skill on
        /// to maximise damage on a target, and buffes that unit. 
        /// Doesn't create an action sequence as this is only used for calculations of the enemy team's movements.
        /// </summary>
        /// <param name="envouteur">The sorcerer buffing</param>
        /// <param name="target">The target of the attacks</param>
        /// <param name="team">The envouteur's team</param>
        /// <returns>True if a unit can be buffed, false else</returns>
        public static bool setBestEnvouteurSkillVsTarget(Character envouteur, Character target, List<Character> team)
        {
            if (envouteur.getPA() > 0 && envouteur.isSkillUp())
            {
                int unitExtraPADamage;
                List<Character> maxDamageDealer;
                List<int> maxDamage;

                maxDamageDealer = new List<Character>();
                maxDamage = new List<int>();
                maxDamage.Add(0);
                unitExtraPADamage = 0;

                //CALCULATING INDIVIDUAL DAMAGE ON THE TARGET AN EXTRA PA WOULD GIVE
                foreach (Character c in team)
                {
                    if ((c.getCharClass() != CharClass.SOIGNEUR) && (c.getCharClass() != CharClass.ENVOUTEUR))
                    {
                        unitExtraPADamage = calcUnitExtraPADamage(c, target);
                        int j = -1;

                        while (unitExtraPADamage < maxDamage[++j]) ;

                        maxDamage.Insert(j, unitExtraPADamage);
                        maxDamageDealer.Insert(j, c);
                    }
                }
                if (maxDamage[0] != 0)
                {
                    List<Character> charList;
                    Hexa hex;

                    hex = takeAOEIntoAccount(envouteur, target, envouteur.getClassData().skill_1,
                            new Pair<List<Character>, List<int>>(maxDamageDealer, maxDamage), ActionType.SKILL1);
                    charList = AIUtil.hexaGrid.getCharWithinRange(hex.x, hex.y, envouteur.getClassData().skill_1.rangeAoE);

                    foreach (Character c in charList)
                    {
                        if (!System.Object.ReferenceEquals(c, envouteur))
                        {
                            c.setPA(c.getPA() + 1);
                        }
                    }

                    envouteur.setPA(envouteur.getPA() - 1);

                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }


        /// <summary>
        /// Function used by maxDamageToTarget()
        /// Calculates what would be the best unit to buff with ATK1
        /// to maximise damage on a target, and buffes that unit. 
        /// Doesn't create an action sequence as this is only used for calculations of the enemy team's movements.
        /// </summary>
        /// <param name="envouteur">The sorcerer buffing</param>
        /// <param name="target">The target of the attacks</param>
        /// <param name="team">The envouteur's team</param>
        /// <returns>True if a unit can be buffed, false else</returns>
        public static bool setBestEnvouteurAttackVsTarget(Character envouteur, Character target, List<Character> team)
        {

            if (envouteur.getPA() > 0)
            {
                int PA, envouteurPA, j;
                Point pos;
                bool moved;

                moved = false;
                pos = new Point(0, 0);
                List<Character> attackChars = new List<Character>();
                List<int> attackCount = new List<int>();
                attackCount.Add(0);

                List<Character> attackCharsMoved = new List<Character>();
                List<int> attackCountMoved = new List<int>();
                attackCountMoved.Add(0);

                //CALCULATING THE NUMBER OF TIME EACH UNIT ATTACKS THE TARGET
                foreach (Character c in team)
                {
                    if (c.getDamageBuff() == false && (c.getCharClass() != CharClass.ENVOUTEUR) && (c.getCharClass() != CharClass.SOIGNEUR))
                    {

                        //---
                        envouteurPA = envouteur.getPA();
                        pos.x = envouteur.getX();
                        pos.y = envouteur.getY();

                        if (!isInRangeToAttack(envouteur, c))
                        {
                            pos = getShortestDistPoint(pos, c); //simule le déplacement de l'envouteur, pour voir s'il est possible d'être à portée de buff
                            if (isInRangeToAttack(envouteur, pos, c))
                            {
                                moved = true;
                            }
                        }
                        if (envouteurPA == envouteur.getPA() || moved)
                        {
                            pos.x = c.getX();
                            pos.y = c.getY();
                            PA = c.getPA();

                            while (PA > 0 && !isInRangeToAttack(c, pos, target))
                            {
                                pos = getShortestDistPoint(pos, target); //simule le déplacement de l'unité, pour voir s'il est possible d'être à portée d'attaque
                                PA--;
                            }

                            if (isInRangeToAttack(c, pos, target))
                            {

                                j = -1;

                                if (moved)
                                {
                                    while (PA < attackCountMoved[++j]) ;

                                    attackCharsMoved.Insert(j, c);
                                    attackCountMoved.Insert(j, PA);
                                }
                                else
                                {
                                    while (PA < attackCount[++j]) ;

                                    attackChars.Insert(j, c);
                                    attackCount.Insert(j, PA);
                                }
                            }
                        }
                    }
                }

                //---
                if (attackCharsMoved.Count != 0 && attackChars.Count != 0)
                {

                    if (attackCharsMoved.Count == 0)
                    {
                        for (j = 0; j < envouteur.getPA() && j < attackChars.Count; j++)
                        {
                            attackChars[j].setDamageBuff(true);
                        }
                        envouteur.setPA(envouteur.getPA() - j);
                    }
                    else if (attackChars.Count == 0)
                    {
                        pos.x = envouteur.getX();
                        pos.y = envouteur.getY();

                        j = 0; //j: nbBuffed
                        while (j < envouteur.getPA() && j < attackCharsMoved.Count)
                        {
                            if (isInRangeToAttack(envouteur, pos, attackCharsMoved[j]))
                            {
                                attackCharsMoved[j].setDamageBuff(true);
                                j++;
                            }
                            else
                            {
                                pos = getShortestDistPoint(pos, attackCharsMoved[j]);
                                envouteur.setPA(envouteur.getPA() - 1);
                            }
                        }
                        envouteur.setPA(envouteur.getPA() - j);
                    }
                    else
                    {
                        //WITHIN RANGE FIRST
                        for (j = 0; j < envouteur.getPA() && j < attackChars.Count; j++)
                        {
                            attackChars[j].setDamageBuff(true);
                        }
                        envouteur.setPA(envouteur.getPA() - j);

                        //OUTSIDE RANGE
                        pos.x = envouteur.getX();
                        pos.y = envouteur.getY();

                        j = 0; //j: nbBuffed
                        while (j < envouteur.getPA() && j < attackCharsMoved.Count)
                        {
                            if (isInRangeToAttack(envouteur, pos, attackCharsMoved[j]))
                            {
                                attackCharsMoved[j].setDamageBuff(true);
                                j++;
                            }
                            else
                            {
                                pos = getShortestDistPoint(pos, attackCharsMoved[j]);
                                envouteur.setPA(envouteur.getPA() - 1);
                            }
                        }
                        envouteur.setPA(envouteur.getPA() - j);
                    }
                }
                else
                {
                    return false;
                }

                return true;
            }
            else
            {
                return false;
            }
        }




        /// <summary>
        /// Overload of getShortestDistPoint()
        /// Calculates the <c>Point</c> at the shortest distance within movementSpeed (3) units
        /// between a <c>Character</c> c and a <c>Character</c> target.
        /// </summary>
        /// <param name="c">The character moving</param>
        /// <param name="target">The target character the unit is moving to</param>
        /// <returns>The <c>Point</c> 3 units away the character should move to</returns>
        public static Point getShortestDistPoint(Character c, Character target)
        {
            return getShortestDistPoint(c.getX(), c.getY(), target.getX(), target.getY(), 3); //all units have 3 movespeed
        }

        /// <summary>
        /// Overload of getShortestDistPoint()
        /// Calculates the <c>Point</c> at the shortest distance within movementSpeed (3) units
        /// between the coordinates <c>Point</c> pos and a <c>Character</c> target.
        /// </summary>
        /// <param name="pos">The coordinates of the starting point</param>
        /// <param name="target">The target character the unit is moving to</param>
        /// <returns>The <c>Point</c> 3 units away the character should move to</returns>
        public static Point getShortestDistPoint(Point pos, Character target)
        {
            return getShortestDistPoint(pos.x, pos.y, target.getX(), target.getY(), 3); //all units have 3 movespeed
        }


        /// <summary>
        /// Overload of getShortestDistPoint()
        /// Calculates the <c>Point</c> at the shortest distance within distance units
        /// between the coordinates <c>Point</c> pos and a <c>Character</c> target.
        /// </summary>
        /// <param name="pos">The coordinates of the starting point</param>
        /// <param name="target">The target character the unit is moving to</param>
        /// <param name="distance">The number of units we are allowed to move</param>
        /// <returns>The <c>Point</c> 3 units away the character should move to</returns>
        public static Point getShortestDistPoint(Character c, Character target, int distance)
        {
            return getShortestDistPoint(c.getX(), c.getY(), target.getX(), target.getY(), distance); //all units have 3 movespeed
        }


        /// <summary>
        /// Overload of getShortestDistPoint()
        /// Calculates the <c>Point</c> at the shortest distance within moveSpeed units
        /// between the coordinates (<c>int</c> startX;<c>int</c> startY)
        ///  and (<c>int</c> destX;<c>int</c> destY).
        /// </summary>
        /// <param name="startX">The coordinate of the starting point in the X axis</param>
        /// <param name="startY">The coordinate of the starting point in the Y axis</param>
        /// <param name="destX">The coordinates of the destination point in the X axis</param>
        /// <param name="destY">The coordinates of the destination point in the Y axis</param>
        /// <param name="moveSpeed">The number of units we are allowed to move</param>
        /// <returns>The <c>Point</c> moveSpeed units away the character should move to</returns>
        public static Point getShortestDistPoint(int startX, int startY, int destX, int destY, int moveSpeed)
        {
            List<Point> path;
            Point destination;
            int distance, index;

            path = pointsAroundHexa(new Point(destX, destY), 1);
            //path = findHexasInSight2(destX,destY,1);
            //index = path.Count;

            for (index = path.Count - 1; index > 0 && AIUtil.hexaGrid.getHexa(path[index]).charOn != null; index--) ;
            //while (--index>0 && Hexas.getHexa(path[index]).charOn!=null));

            if (AIUtil.hexaGrid.getHexa(destination = path[index]).charOn == null)
            {

                distance = AIUtil.hexaGrid.getWalkingDistance(startX, startY, destination.x, destination.y);
                path = AIUtil.hexaGrid.findShortestPath(startX, startY, destination.x, destination.y, distance);

                if (path == null) Debug.Log("null returned; x,y: " + destination.x + "," + destination.y + " --- charOn: " + AIUtil.hexaGrid.getHexa(destination));

                IEnumerable<Point> pathTrimmed;
                pathTrimmed = path.Take(moveSpeed + 1);

                /*Debug.Log("Inside getShortestDist:");

                Debug.Log("Character position (x): "+startX);
                Debug.Log("Character position (y): "+startY);
                Debug.Log("Target point (x): "+destX);
                Debug.Log("Target point (y): "+destY);

                Debug.Log("Original path size:" +path.Count);
                Debug.Log("Trimmed path size:" +pathTrimmed.Count());
                Debug.Log("Returned:" +(pathTrimmed.Count() -1));

                String pathString = "[";
                String trimmedPathString = "[";
                trimmedPathString+=pathTrimmed.ElementAt(0).x + ","+pathTrimmed.ElementAt(0).y;
                
                for (int i=1;i<pathTrimmed.Count();i++) {
                    trimmedPathString+=" - "+pathTrimmed.ElementAt(i).x + ","+pathTrimmed.ElementAt(i).y;
                }
                trimmedPathString+="]";

                pathString+=path[0];
                for (int i=1;i<path.Count;i++) {
                    pathString+=" - "+path[i].x+","+path[i].y;
                }
                pathString+="]";

                Debug.Log("Path: " +pathString);
                Debug.Log("Trimmed path: " +trimmedPathString);

                Debug.Log("Outside (soon) getShortestDist:");*/

                return pathTrimmed.ElementAt(pathTrimmed.Count() - 1);
            }
            else return null;
        }


        /// <summary>
        /// Overload of getShortestDistPoint()
        /// Calculates the <c>Point</c> at the shortest distance within range units
        /// between a <c>Character</c> c and the coordinates (<c>int</c> destX;<c>int</c> destY).
        /// </summary>
        /// <param name="c">The character attacking</param>
        /// <param name="destX">The coordinates of the destination point in the X axis</param>
        /// <param name="destY">The coordinates of the destination point in the Y axis</param>
        /// <param name="range">The number of units we are allowed to move</param>
        /// <param name="checkAttack">If true, only returns a <c>Point</c> visible to the character</param>
        /// <returns>The <c>Point</c> range units away the character should target for an attack</returns>
        public static Point getShortestDistPoint(Character c, int destX, int destY, int range, bool checkAttack)
        {
            List<Point> path;
            Point destination;
            int distance, index;
            int startX = c.getX();
            int startY = c.getY();

            path = pointsAroundHexa(new Point(destX, destY), 1);

            for (index = path.Count - 1; index > 0 && AIUtil.hexaGrid.getHexa(path[index]).charOn != null; index--) ;

            if (AIUtil.hexaGrid.getHexa(destination = path[index]).charOn == null)
            {

                distance = AIUtil.hexaGrid.getWalkingDistance(startX, startY, destination.x, destination.y);
                path = AIUtil.hexaGrid.findShortestPath(startX, startY, destination.x, destination.y, distance);

                if (path == null) Debug.Log("null returned; x,y: " + destination.x + "," + destination.y + " --- charOn: " + AIUtil.hexaGrid.getHexa(destination));

                IEnumerable<Point> pathTrimmed;
                pathTrimmed = path.Take(range + 1);

                int elementAt = pathTrimmed.Count() - 1;

                if (checkAttack)
                {
                    while (!AIUtil.hexaGrid.hexaInSight(c.getX(), c.getY(), pathTrimmed.ElementAt(elementAt).x, pathTrimmed.ElementAt(elementAt).y, range) && elementAt > 0)
                        elementAt--;
                }

                return pathTrimmed.ElementAt(elementAt);
            }
            else return null;
        }




        /// <summary>
        /// findSequencePathToHexa but with CPU planning included.
        /// </summary>
        /// <param name="currentChar">The character controlled by the AI</param>
        /// <param name="x">X coordinate of where we want to go</param>
        /// <param name="y">Y coordinate of where we want to go</param>
        /// <returns>A list of ActionAIPos with the character going to (X,Y)</returns>
        public static List<ActionAIPos> findSequencePathToHexaCPU(Character currentChar, int x, int y)
        {
            List<ActionAIPos> sequence = new List<ActionAIPos>();
            List<Hexa> hexasSoonToBeMovedOn = new List<Hexa>();

            // Adding where the CPU plans to move characters
            // This is purely so CPU characters don't run into each other
            foreach (CharacterCPU cpu in CharactersCPU.CharacterCPU.charCPUList)
                if (AIUtil.hexaGrid.getHexa(cpu.X, cpu.Y).charOn == null)
                {
                    hexasSoonToBeMovedOn.Add(AIUtil.hexaGrid.getHexa(cpu.X, cpu.Y));

                    // Adding a dummy character
                    AIUtil.hexaGrid.getHexa(cpu.X, cpu.Y).charOn = cpu.TempChar;
                }

            sequence = AIUtil.findSequencePathToHexa(currentChar, x, y);

            // Removing all the invisible characters added in
            foreach (Hexa h in hexasSoonToBeMovedOn)
                h.charOn = null;

            return sequence;
        }

        /// <summary>
        /// hexaInSight but with CPU planning included.
        /// </summary>
        /// <param name="x">Unit x</param>
        /// <param name="y">Unit y</param>
        /// <param name="hexaX">Target x</param>
        /// <param name="hexaY">Target y</param>
        /// <param name="maxRange">Range</param>
        /// <returns>True if (<paramref name="hexaX"/>,<paramref name="hexaY"/>) is in sight from (<paramref name="x"/>,<paramref name="y"/>)</returns>
        public static bool hexaInSightCPU(int x, int y, int hexaX, int hexaY, int maxRange)
        {
            bool returnValue;
            List<Hexa> hexasSoonToBeMovedOn = new List<Hexa>();

            // Adding where the CPU plans to move characters
            // This is purely so CPU characters don't run into each other
            foreach (CharacterCPU cpu in CharactersCPU.CharacterCPU.charCPUList)
                if (AIUtil.hexaGrid.getHexa(cpu.X, cpu.Y).charOn == null)
                {
                    hexasSoonToBeMovedOn.Add(AIUtil.hexaGrid.getHexa(cpu.X, cpu.Y));

                    // Adding a dummy character
                    AIUtil.hexaGrid.getHexa(cpu.X, cpu.Y).charOn = cpu.TempChar;
                }

            returnValue = AIUtil.hexaGrid.hexaInSight(x, y, hexaX, hexaY, maxRange);

            // Removing all the invisible characters added in
            foreach (Hexa h in hexasSoonToBeMovedOn)
                h.charOn = null;

            return returnValue;
        }

        /*
                /// <summary>
                /// hexaInSight without planning (for attack only).
                /// </summary>
                /// <param name="x">Unit x</param>
                /// <param name="y">Unit y</param>
                /// <param name="hexaX">Target x</param>
                /// <param name="hexaY">Target y</param>
                /// <param name="maxRange">Range</param>
                /// <param name="target">checks target</param>
                /// <returns>True if (<paramref name="hexaX"/>,<paramref name="hexaY"/>) is in sight from (<paramref name="x"/>,<paramref name="y"/>)</returns>
                public static bool hexaInSightCPU(int x, int y, int hexaX, int hexaY, int maxRange, Point target)
                {
                    bool returnValue;
                    List<Hexa> hexasSoonToBeMovedOn = new List<Hexa>();

                    // Adding where the CPU plans to move characters
                    // This is purely so CPU characters don't run into each other
                    foreach (CharacterCPU cpu in CharactersCPU.CharacterCPU.charCPUList)
                        if (AIUtil.hexaGrid.getHexa(cpu.X, cpu.Y).charOn == null || (cpu.X==target.x && cpu.Y==target.y))
                        {
                            hexasSoonToBeMovedOn.Add(AIUtil.hexaGrid.getHexa(cpu.X, cpu.Y));
                        }

                    returnValue = AIUtil.hexaGrid.hexaInSight(x, y, hexaX, hexaY, maxRange);

                    // Removing all the invisible characters added in
                    foreach (Hexa h in hexasSoonToBeMovedOn)
                        h.charOn = null;

                    return returnValue;
                }

                */



        /// <summary>
        /// Checks if <code>target</code> is in <code>c</code>'s basic attack range.
        /// Checks if <code>target</code> is in <code>c</code>'s basic attack range.
        /// </summary>
        /// <param name="c">The character</param>
        /// <param name="target">The target</param>
        /// <returns>True if <code>target</code> is in range and false otherwise</returns>
        public static bool isInRangeToAttack(Character c, Character target)
        {
            return hexaInSightCPU(c.x, c.y,
                target.x, target.y,
                c.getClassData().basicAttack.range);
        }


        /// <summary>
        /// Checks if <code>target</code> is in <code>c</code>'s basic attack range.
        /// </summary>
        /// <param name="c">The character</param>
        /// <param name="target">The target's coordinates</param>
        /// <returns>True if <code>target</code> is in range and false otherwise</returns>
        public static bool isInRangeToAttack(Character c, Point target)
        {
            return hexaInSightCPU(c.x, c.y,
                target.x, target.y,
                c.getClassData().basicAttack.range);
        }


        /// <summary>
        /// Checks if <code>target</code> is in <code>c</code>'s basic attack range, from <code>pos</code>'s coordinates.
        /// </summary>
        /// <param name="c">The character</param>
        /// <param name="pos">The character's coordinates</param>
        /// <param name="target">The target</param>
        /// <returns>True if <code>target</code> is in range and false otherwise</returns>
        public static bool isInRangeToAttack(Character c, Point pos, Character target)
        {
            return hexaInSightCPU(pos.x, pos.y,
                target.x, target.y,
                c.getClassData().basicAttack.range);
        }


        /// <summary>
        /// Checks if <code>target</code> is in <code>c</code>'s basic attack range.
        /// </summary>
        /// <param name="c">The character</param>
        /// <param name="target">The target</param>
        /// <returns>True if <code>target</code> is in range and false otherwise</returns>
        public static bool isInRangeToAttack(CharacterCPU c, CharacterCPU target)
        {
            return hexaInSightCPU(c.X, c.Y,
                target.X, target.Y,
                c.Character.getClassData().basicAttack.range);
        }


        /// <summary>
        /// Checks if <code>target</code> is in <code>c</code>'s basic attack range.
        /// </summary>
        /// <param name="c">The character</param>
        /// <param name="target">The target</param>
        /// <param name="range">The range of the attack</param>
        /// <returns>True if <code>target</code> is in range and false otherwise</returns>
        public static bool isInRangeToAttack(Point c, Point target, int range)
        {
            return hexaInSightCPU(c.y, c.x,
                target.x, target.y,
                range);
        }




        /// <summary>
        /// Checks if <code>target</code> is in <code>c</code>'s skill range.
        /// </summary>
        /// <param name="c">The character</param>
        /// <param name="target">The target</param>
        /// <returns>True if <code>target</code> is in range and false otherwise</returns>
        public static bool isInRangeToUseSkill(Character c, Character target)
        {
            return hexaInSightCPU(c.x, c.y,
                target.x, target.y,
                c.getClassData().skill_1.range);
        }

        public static bool isInRangeToUseSkill2(Character c, Character target)
        {
            return hexaInSightCPU(c.x, c.y,
                target.x, target.y,
                c.getClassData().skill_2.range);
        }
        /// <summary>
        /// Checks if <code>target</code> is in <code>c</code>'s skill range.
        /// </summary>
        /// <param name="c">The character</param>
        /// <param name="target">The target's coordinates</param>
        /// <returns>True if <code>target</code> is in range and false otherwise</returns>
        public static bool isInRangeToUseSkill(Character c, Point target)
        {
            return hexaInSightCPU(c.x, c.y,
                target.x, target.y,
                c.getClassData().skill_1.range);
        }

        public static bool isInRangeToUseSkill2(Character c, Point target)
        {
            return hexaInSightCPU(c.x, c.y,
                target.x, target.y,
                c.getClassData().skill_2.range);
        }
        /// <summary>
        /// Checks if <code>target</code> is in <code>c</code>'s skill range.
        /// </summary>
        /// <param name="c">The character</param>
        /// <param name="target">The target's coordinates</param>
        /// <param name="range">The range of the attack</param>
        /// <returns>True if <code>target</code> is in range and false otherwise</returns>
        public static bool isInRangeToUseSkill(Point c, Point target, int range)
        {
            return hexaInSightCPU(c.x, c.y,
                target.x, target.y,
                range);
        }

        public static bool isInRangeToUseSkill2(Point c, Point target, int range)
        {
            return hexaInSightCPU(c.x, c.y,
                target.x, target.y,
                range);
        }
        /// <summary>
        /// Checks if <code>target</code> is in <code>c</code>'s skill range, from <code>pos</code> coordinates.
        /// </summary>
        /// <param name="c">The character</param>
        /// <param name="pos">The character's coordinates</param>
        /// <param name="target">The target</param>
        /// <returns>True if <code>target</code> is in range and false otherwise</returns>
        public static bool isInRangeToUseSkill(Character c, Point pos, Character target)
        {
            return hexaInSightCPU(pos.x, pos.y,
                target.x, target.y,
                c.getClassData().skill_1.range);
        }

        public static bool isInRangeToUseSkill2(Character c, Point pos, Character target)
        {
            return hexaInSightCPU(pos.x, pos.y,
                target.x, target.y,
                c.getClassData().skill_2.range);
        }
        /// <summary>
        /// Checks if <code>target</code> is in <code>c</code>'s skill range.
        /// </summary>
        /// <param name="c">The character</param>
        /// <param name="target">The target</param>
        /// <returns>True if <code>target</code> is in range and false otherwise</returns>
        public static bool isInRangeToUseSkill(CharacterCPU c, CharacterCPU target)
        {
            return hexaInSightCPU(c.X, c.Y,
                target.X, target.Y,
                c.Character.getClassData().skill_1.range);
        }

        public static bool isInRangeToUseSkill2(CharacterCPU c, CharacterCPU target)
        {
            return hexaInSightCPU(c.X, c.Y,
                target.X, target.Y,
                c.Character.getClassData().skill_2.range);
        }


        /// <summary>
        /// Checks if <code>target</code> is in <code>c</code>'s total range.
        /// This means the movements required to get the target in range plus the range itself.
        /// </summary>
        /// <param name="c">The character</param>
        /// <param name="target">The target</param>
        /// <returns>True if <code>target</code> is in range and false otherwise</returns>
        /// edited by Rayane TALEB L3C2
        public static bool isInTotalRange(Character c, Character target)
        {
            int totalRange = c.skill2Available ?
                (c.getClassData().basePA - 1) * c.getClassData().basePM
                + c.getSkill2Range()
                + c.getClassData().skill_2.rangeAoE
                :
                c.skillAvailable ?
                (c.getClassData().basePA - 1) * c.getClassData().basePM
                + c.getSkillRange()
                + c.getClassData().skill_1.rangeAoE
                :
                (c.getClassData().basePA - 1) * c.getClassData().basePM
                + c.getClassData().basicAttack.range
                + c.getClassData().basicAttack.rangeAoE;

            return AIUtil.hexaGrid.getWalkingDistance(c.getX(), c.getY(), target.getX(), target.getY()) <= totalRange;
        }




        /// <summary>
        /// Checks if <code>target</code> is in <code>c</code>'s total range.
        /// This means the movements required to get the target in range plus the range itself.
        /// </summary>
        /// <param name="c">The character</param>
        /// <param name="target">The target</param>
        /// <returns>True if <code>target</code> is in range and false otherwise</returns>
        /// /// edited by Rayane TALEB L3C2
        public static bool isInRange(Character c, Character target)
        {
            // "(c.getClassData().basePA - 1) * + c.getSkillRange()" ?
            // Je comprends pas cette méthode
            int totalRange = c.skill2Available ?
                (c.getClassData().basePA - 1) * c.getClassData().basePM
                + c.getSkill2Range()
                + c.getClassData().skill_2.rangeAoE
                :
                c.skillAvailable ?
                (c.getClassData().basePA - 1) *
                +c.getSkillRange()
                + c.getClassData().skill_1.rangeAoE
                :
                (c.getClassData().basePA - 1) *
                +c.getClassData().basicAttack.range
                + c.getClassData().basicAttack.rangeAoE;

            return AIUtil.hexaGrid.getWalkingDistance(c.getX(), c.getY(), target.getX(), target.getY()) <= totalRange;
        }




        /// <summary>
        /// Checks if <code>target</code> is in <code>c</code>'s total range.
        /// This means the movements required to get the target in range plus the range itself.
        /// </summary>
        /// <param name="c">The character</param>
        /// <param name="target">The target</param>
        /// <returns>True if <code>target</code> is in range and false otherwise</returns>
        /// edited Rayane TALEB L3C2
        public static bool isInTotalRange(CharacterCPU c, CharacterCPU target)
        {
            int totalRange = c.Character.skill2Available ?
                (c.Character.getClassData().basePA - 1) * c.Character.getClassData().basePM
                + c.Character.getSkill2Range()
                + c.Character.getClassData().skill_2.rangeAoE
                :
                c.Character.skillAvailable ?
                (c.Character.getClassData().basePA - 1) * c.Character.getClassData().basePM
                + c.Character.getSkillRange()
                + c.Character.getClassData().skill_1.rangeAoE
                :
                (c.Character.getClassData().basePA - 1) * c.Character.getClassData().basePM
                + c.Character.getClassData().basicAttack.range
                + c.Character.getClassData().basicAttack.rangeAoE;

            return AIUtil.hexaGrid.getWalkingDistance(c.X, c.Y, target.X, target.Y) <= totalRange;
        }




        /// <summary>
        /// Checks if <code>target</code> got a SOIGNEUR in his team
        /// </summary>
        /// <param name="c">The character</param>
        /// <param name="pos">The character's coordinates</param>
        /// <param name="target">The target</param>
        /// <returns>True if <code>target</code> is in range and false otherwise</returns>
        public static bool gotHealerInTeam(int team)
        {
            foreach (Character charac in AIUtil.hexaGrid.charList)
                if (charac.team == team)
                    if (charac.charClass == CharClass.SOIGNEUR)
                        return true;
            return false;
        }




        /// <summary>
        /// Checks if <code>target</code> got a ENVOUTEUR in his team
        /// </summary>
        /// <param name="c">The character</param>
        /// <param name="pos">The character's coordinates</param>
        /// <param name="target">The target</param>
        /// <returns>True if <code>target</code> is in range and false otherwise</returns>
        public static bool gotEnvouteurInTeam(int team)
        {
            foreach (Character charac in AIUtil.hexaGrid.charList)
                if (team == charac.team)
                    if (charac.charClass == CharClass.ENVOUTEUR)
                        return true;
            return false;
        }




        /// <summary>
        /// Return true if character can get group killed by the enemy team
        /// </summary>
        /// <param name="c">The character</param>
        /// <param name="pos">The character's coordinates</param>
        /// <param name="target">The target</param>
        /// <returns>True if <code>target</code> is in range and false otherwise</returns>
        public static bool canGetGroupKill(List<Character> enemyTeam, Character character)
        {
            //Debug.Log("Danger check: damage:" + maxDamageToTarget(enemyTeam, character) + "hp: " + character.HP);
            if (maxDamageToTarget(enemyTeam, character) > character.HP)
                return true;
            return false;
        }




        /// <summary>
        /// Return true if currentTeam can groupKill the enemy
        /// </summary>
        /// <param name="c">The character</param>
        /// <param name="pos">The character's coordinates</param>
        /// <param name="target">The target</param>
        /// <returns>True if <code>target</code> is in range and false otherwise</returns>
        public static bool weakerGetGroupKill(int currentTeam, Character character, HexaGrid hexas)
        {
            List<Character> allyTeam = getCharacterTeamList(currentTeam, hexas);

            if (maxDamageToTarget(allyTeam, character) > character.HP)
                return true;

            return false;
        }





        ///<summary>
        /// Return the easiest Enemy to kill
        ///</summary>
        ///<param name = currentChar> The current character ID </param>
        ///<returns> Move or Skip turn </returns>
        public static Character easierTargetToKill(int ourteam, Character c, HexaGrid hexas)
        {
            List<Character> enemyTeamList = getEnemyCharacterTeamList(ourteam, hexas);
            List<Character> allyTeam = getCharacterTeamList(ourteam, hexas);
            List<Character> targetChoice = new List<Character>();

            foreach (Character enemy in enemyTeamList)
                if (canGroupKill(allyTeam, enemy))
                    //if (weakerGetGroupKill(ourteam, enemy, hexas))
                    targetChoice.Add(enemy);

            if (targetChoice.Count > 1)
            {
                Debug.Log("TargetToKill => " + targetChoice[0].charClass);
                return targetChoice[0];
            }

            return enemyInMostAllyRange(ourteam, c, hexas);
        }




        /// <summary>
        /// Return true if c is in danger
        /// </summary>
        /// <param name="c">The character</param>
        /// <param name="pos">The character's coordinates</param>
        /// <param name="target">The target</param>
        /// <returns>True if <code>target</code> is in range and false otherwise</returns>
        public static bool isInDanger(Character c, HexaGrid hexas)
        {
            List<Character> enemyTeam = getEnemyCharacterTeamList(c.team, hexas);

            //if (canGetGroupKill(enemyTeam, c))
            //return true;

            if (c.HP < 3 && isInEnemyTeamRangeAttack(c))
                return true;

            return false;
        }




        /// <summary>
        /// Checks if someone in <code>target</code> team is in danger
        /// </summary>
        /// <param name="c">The character</param>
        /// <param name="pos">The character's coordinates</param>
        /// <param name="target">The target</param>
        /// <returns>True if <code>target</code> is in range and false otherwise</returns>
        public static bool teamMemberInDanger(Character currentChar, HexaGrid hexas)
        {
            List<Character> characterTeamList = getCharacterTeamList(currentChar.team, hexas);
            List<Character> enemyTeam = getEnemyCharacterTeamList(currentChar.team, hexas);

            foreach (Character ally in characterTeamList)
            {
                // canGroupKill ne marche pas, laisser en commentaire tant qu'elle n'est pas corrigée

                if (canGroupKill(enemyTeam, ally))
                    return true;

                //if (ally.HP < 7 && isInEnemyTeamRangeAttack(ally))
                //    return true;
            }
            return false;
        }

        //Author: Klaus Gouveia, L3Q1
        public static int oppositeTeam(int currentTeam){
            if(currentTeam == 1) return 0;
            else if(currentTeam == 0) return 1;
            else return -1;
        }


        ///<summary>
        /// Determines if the Character c is within the attacker range attack
        ///</summary>
        ///<param name = CharID> The current character ID </param>
        ///<param name = TargetID> The target ID </param>
        ///<returns> Returns true or false </returns>
        public static bool isInRangeAttack(Character c, Character attacker)
        {
            Point charPos = new Point(attacker.x, attacker.y);

            if (AIUtil.hexaGrid.hexaInSight(attacker.x, attacker.y, c.x, c.y, attacker.getClassData().basicAttack.range)) return true;

            int i = attacker.PM * (attacker.PA - 1);
            if (i > 0)
            {
                List<Point> listH = AIUtil.hexaGrid.findAllPaths(attacker.x, attacker.y, i);
                foreach (Point charpos in listH)
                {
                    attacker.updatePos2(charpos.x, charpos.y, AIUtil.hexaGrid);
                    if (AIUtil.hexaGrid.hexaInSight(attacker.x, attacker.y, c.x, c.y, attacker.getClassData().basicAttack.range))
                    {
                        attacker.updatePos2(charPos.x, charPos.y, AIUtil.hexaGrid);
                        return true;
                    }
                }
            }
            attacker.updatePos2(charPos.x, charPos.y, AIUtil.hexaGrid);
            return false;
        }




        ///<summary>
        /// Determines if the point is within the current character's range attack
        ///</summary>
        ///<param name = CharID> The current character ID </param>
        ///<param name = TargetID> The target ID </param>
        ///<returns> Returns true or false </returns>
        public static bool isPointInEnemyRangeAttack(Point p, Character enemy)
        {
            Point charPos = new Point(enemy.x, enemy.y);

            if (AIUtil.hexaGrid.hexaInSight(enemy.x, enemy.y, p.x, p.y, enemy.getClassData().basicAttack.range)) return true;

            int i = enemy.PM * (enemy.PA - 1);
            if (i > 0)
            {
                List<Point> listH = AIUtil.hexaGrid.findAllPaths(enemy.x, enemy.y, i);
                foreach (Point charpos in listH)
                {
                    enemy.updatePos2(charpos.x, charpos.y, AIUtil.hexaGrid);
                    if (AIUtil.hexaGrid.hexaInSight(enemy.x, enemy.y, p.x, p.y, enemy.getClassData().basicAttack.range))
                    {
                        enemy.updatePos2(charPos.x, charPos.y, AIUtil.hexaGrid);
                        return true;
                    }
                }
            }
            enemy.updatePos2(charPos.x, charPos.y, AIUtil.hexaGrid);
            return false;
        }




        ///<summary>
        /// Return how many character in the enemy team can attack the Point p
        ///</summary>
        ///<param name = CharID> The current character ID </param>
        ///<param name = TargetID> The target ID </param>
        ///<returns> Returns true or false </returns>
        public static int howManyEnemyCanAttackPoint(Point p, int currentTeam, HexaGrid hexas)
        {
            List<Character> enemyTeam = getEnemyCharacterTeamList(currentTeam, hexas);
            int value = 0;

            foreach (Character enemy in enemyTeam)
                if (enemy.charClass != CharClass.SOIGNEUR && enemy.charClass != CharClass.ENVOUTEUR && enemy.charClass != CharClass.ARCHER)
                    if (isPointInEnemyRangeAttack(p, enemy))
                        value++;

            return value;
        }




        ///<summary>
        /// Determines if the target is within the enemy team range
        ///</summary>
        ///<param name = CharID> The current character ID </param>
        ///<param name = TargetID> The target ID </param>
        ///<returns> Returns true or false </returns>
        public static bool isInEnemyTeamRangeAttack(Character c)
        {
            // Check all enemy range
            Character enemy = null;

            for (int i = 0; i < AIUtil.hexaGrid.charList.Count; i++)
                if (AIUtil.hexaGrid.charList[i].team != c.team)
                {
                    enemy = AIUtil.hexaGrid.charList[i];
                    if (isInRangeAttack(c, enemy))
                        return true;
                }
            return false;
        }




        ///<summary>
        /// Determines if the point is within the enemy team range
        ///</summary>
        ///<param name = CharID> The current character ID </param>
        ///<param name = TargetID> The target ID </param>
        ///<returns> Returns true or false </returns>
        public static bool isPointInEnemyTeamRangeAttack(Point p, int ourteam)
        {
            // Check all enemy range
            Character enemy = null;

            for (int i = 0; i < AIUtil.hexaGrid.charList.Count; i++)
                if (AIUtil.hexaGrid.charList[i].team != ourteam)
                {
                    enemy = AIUtil.hexaGrid.charList[i];
                    if (isPointInEnemyRangeAttack(p, enemy))
                        return true;
                }
            return false;
        }




        ///<summary>
        /// Determines the enemy that is in range of most of my character
        ///</summary>
        ///<param name = CharID> The current character ID </param>
        ///<param name = TargetID> The target ID </param>
        ///<returns> Returns true or false </returns>
        public static Character enemyInMostAllyRange(int ourteam, Character c, HexaGrid hexas)
        {
            List<Character> enemyTeamList = getEnemyCharacterTeamList(ourteam, hexas);
            List<Character> allyTeamList = getCharacterTeamList(ourteam, hexas);
            List<int> value = new List<int>();
            List<int> maxValueIndex = new List<int>();
            Character target = null;

            for (int i = 0; i < enemyTeamList.Count; i++)
                value.Add(0);

            int j = 0;
            bool allNotEquals = false;


            foreach (Character enemy in enemyTeamList)
            {
                foreach (Character ally in allyTeamList)
                {
                    // we check if the enemy, is within our ally range
                    if (ally.charClass != CharClass.SOIGNEUR && ally.charClass != CharClass.ENVOUTEUR && ally.charClass != CharClass.ARCHER)
                        if (isInRangeAttack(enemy, ally))
                        {
                            value[j]++;
                            allNotEquals = true;
                        }
                }
                j++;
            }

            // Not early game
            if (allNotEquals)
            {
                for (int i = 0; i < value.Count; i++)
                    if (value[i] == value.Max())
                        maxValueIndex.Add(i);

                target = enemyTeamList[value.IndexOf(value.Max())];

                foreach (int index in maxValueIndex)
                    if (enemyTeamList[index].HP < target.HP)
                        target = enemyTeamList[index];

                Debug.Log("target => " + target.charClass);
                return target;

            }

            // Early game
            return closestEnemy(ourteam, c, hexas);
        }




        ///<summary>
        /// Return the closest enemy from the currentChar
        ///</summary>
        ///<param name = currentChar> The current character ID </param>
        ///<returns> Move or Skip turn </returns>
        public static Character closestEnemy(int ourteam, Character currentChar, HexaGrid hexas)
        {
            List<Point> listH = AIUtil.hexaGrid.findAllPaths(currentChar.x, currentChar.y, currentChar.PM);
            List<Character> enemyTeamList = getEnemyCharacterTeamList(ourteam, hexas);
            Point targetLocation = null;
            Hexa goTo = null;
            int j = 0;

            List<int> value = new List<int>();
            //for (int i = 0; i < enemyTeamList.Count; i++)
            //value.Add(0);

            foreach (Character enemy in enemyTeamList)
            {
                targetLocation = new Point(enemy.x, enemy.y);
                goTo = hexas.getHexa(targetLocation.x, targetLocation.y);
                if (goTo != null && (goTo.type == HexaType.GROUND || goTo.type == HexaType.BONUS) && goTo.charOn != null)
                {
                    value.Add(AIUtil.hexaGrid.getWalkingDistance(currentChar.x, currentChar.y, enemy.x, enemy.y));
                }
                j++;
            }
            j--;

            //Debug.Log("Closest target => " + enemyTeamList[value.IndexOf(value.Min())].charClass);
            return enemyTeamList[value.IndexOf(value.Min())];
        }




        ///<summary>
        /// Return the closest enemy from the currentChar (except hunter)
        ///</summary>
        ///<param name = currentChar> The current character ID </param>
        ///<returns> Move or Skip turn </returns>
        public static Character closestEnemyRange(int ourteam, Character currentChar, HexaGrid hexas)
        {
            List<Point> listH = AIUtil.hexaGrid.findAllPaths(currentChar.x, currentChar.y, currentChar.PM);
            List<Character> enemyTeamList = getEnemyCharacterTeamList(ourteam, hexas);
            Point targetLocation = null;
            Hexa goTo = null;
            int j = 0;

            List<int> value = new List<int>();
            //for (int i = 0; i < enemyTeamList.Count; i++)
            //value.Add(0);

            foreach (Character enemy in enemyTeamList)
            {
                if (enemy.charClass != CharClass.ARCHER)
                {
                    targetLocation = new Point(enemy.x, enemy.y);
                    goTo = hexas.getHexa(targetLocation.x, targetLocation.y);
                    if (goTo != null && goTo.type == HexaType.GROUND && goTo.charOn != null)
                    {
                        value.Add(AIUtil.hexaGrid.getWalkingDistance(currentChar.x, currentChar.y, enemy.x, enemy.y));
                    }
                    j++;
                }
            }

            //Debug.Log("Closest target => " + enemyTeamList[value.IndexOf(value.Min())].charClass);
            return enemyTeamList[value.IndexOf(value.Min())];
        }




        ///<summary>
        /// Return the closest enemy from the currentChar
        ///</summary>
        ///<param name = currentChar> The current character ID </param>
        ///<returns> Move or Skip turn </returns>
        public static CharacterCPU closestEnemyCPU(int ourteam, Character currentChar, HexaGrid hexas)
        {
            List<Point> listH = AIUtil.hexaGrid.findAllPaths(currentChar.x, currentChar.y, currentChar.PM);
            List<CharacterCPU> enemyTeamList = getEnemyTeamList(ourteam, hexas);
            Point targetLocation = null;
            Hexa goTo = null;

            List<int> value = new List<int>();

            foreach (CharacterCPU enemy in enemyTeamList)
            {
                targetLocation = new Point(enemy.Character.x, enemy.Character.y);
                goTo = hexas.getHexa(targetLocation.x, targetLocation.y);
                if (goTo != null && goTo.type == HexaType.GROUND && goTo.charOn != null)
                {
                    value.Add(AIUtil.hexaGrid.getWalkingDistance(currentChar.x, currentChar.y, enemy.Character.x, enemy.Character.y));
                }
            }

            Debug.Log("Closest target => " + enemyTeamList[value.IndexOf(value.Min())].Character.charClass);
            return enemyTeamList[value.IndexOf(value.Min())];
        }




        public static Point getDistanceFromGroup(Character c)
        { //x,y coordinates away from group
            //similaire à isAdjacent, mais utilise un point spécifique qui sera le "centre" du groupe ? voir avec les formations ; à discuter
            return null;
        }




        public static List<Point> getEnemyRangeArea(Character c)
        { //x,y coordinates of each hexa
            //ou bien utiliser directement la classe Hexa ?
            //pas très utile comme méthode?
            return null;
        }




        /// <summary>
        /// Returns a list of all the characters in attack range of <code>c</code>. 
        /// Takes into account whether <code>c</code> has his skill ready or not.
        /// </summary>
        /// <param name="c">The character the method uses</param>
        /// <returns>A list of all the characters <code>c</code> can target</returns>
        public static List<Character> getTargetsAvailable(Character c)
        {
            List<Character> targetsAvailable = new List<Character>();

            foreach (Character target in AIUtil.hexaGrid.charList)
                if (isInTotalRange(c, target))
                    targetsAvailable.Add(target);

            return targetsAvailable;
        }




        /// <summary>
        /// Returns a list of all the enemy characters in attack range of <code>c</code>. 
        /// Takes into account whether <code>c</code> has his skill ready or not.
        /// </summary>
        /// <param name="c">The character the method uses</param>
        /// <returns>A list of all the characters <code>c</code> can target</returns>
        public static List<Character> getEnemyTargetsAvailable(Character c)
        {
            List<Character> targetsAvailable = new List<Character>();

            foreach (Character target in AIUtil.hexaGrid.charList)
                if (target.team != c.team)
                    if (isInTotalRange(c, target))
                        targetsAvailable.Add(target);

            return targetsAvailable;
        }




        /// <summary>
        /// Returns the base priority value of <code>c</code>. This base priority value changes according to the character.
        /// The lower the value, the more important the character is.
        /// </summary>
        /// <param name="c">The target character</param>
        /// <returns>The corresponding priority value</returns>
        public static float getDefaultPriorityValue(Character c)
        {
            // C'est juste leur ordre comme défini dans le Discord, à changer après un peu de manip bien sûr
            // On pourrait aussi définir ces valeurs dans des constantes si on veut en vrai
            switch (c.getCharClass())
            {
                case CharClass.GUERRIER:
                    return 22;
                case CharClass.VOLEUR:
                    return 15;
                case CharClass.MAGE:
                    return 30;
                case CharClass.ARCHER:
                    return 50;
                case CharClass.SOIGNEUR:
                    return 40;
                case CharClass.ENVOUTEUR:
                    return 10;
                default:
                    return 0;

            }
        }



        /// <summary>
        /// Returns the priority value of <code>c</code>. This method takes into account the damage the team can inflict to the target.
        /// The higher the value, the more important the character is.
        /// </summary>
        /// <remarks>
        /// "The higher the value" A discuter
        /// </remarks>
        /// <param name="c">The target character</param>
        /// <returns>The corresponding priority value</returns>
        public static float getTargetPriorityValue(List<Character> team, Character target)
        {
            int maxDamage = 0;
            maxDamage = maxDamageToTarget(team, target);

            // Le premier paramètre est une liste
            if (canGroupKill(maxDamage, target))
            {
                return getDefaultPriorityValue(target) * 1000;
            }
            else
            {
                return getDefaultPriorityValue(target) * maxDamage;
            }
        }




        //edited by TALEB Rayane, group: L3C2
        //Author : ?
        /// <summary>
        /// Returns the charactersCPU in the team declared in the parameters.
        /// </summary>
        /// <param name="team">The team we want the list of</param>
        /// <param name="hexas">The game board with the list of all characters on the board</param>
        /// <returns>A list of CharacterCPU objects corresponding to the team's composition</returns>
        public static List<CharacterCPU> getTeamList(int team, HexaGrid hexas)
        {
            List<CharacterCPU> teamList = new List<CharacterCPU>();

            foreach (Character c in hexas.charList)
                if (team == c.team)
                    switch (c.charClass)
                    {
                        case CharClass.GUERRIER:
                            teamList.Add(new WarriorCPU(c, hexas));
                            break;
                        case CharClass.VOLEUR:
                            teamList.Add(new ThiefCPU(c, hexas));
                            break;
                        case CharClass.MAGE:
                            teamList.Add(new MageCPU(c, hexas));
                            break;
                        case CharClass.ARCHER:
                            teamList.Add(new ArcherCPU(c, hexas));
                            break;
                        case CharClass.SOIGNEUR:
                            teamList.Add(new HealerCPU(c, hexas));
                            break;
                        case CharClass.ENVOUTEUR:
                            teamList.Add(new SorcererCPU(c, hexas));
                            break;
                        case CharClass.VALKYRIE:
                            teamList.Add(new ValkyrieCPU(c, hexas));
                            break;
                        case CharClass.DRUIDE:
                            teamList.Add(new DruideCPU(c, hexas));
                            break;
                        default:
                            break;
                    }

            return teamList;
        }




        /// <summary>
        /// Returns a list of all characters in <paramref name="team"/>.
        /// </summary>
        /// <param name="team">The team we want the list of</param>
        /// <param name="hexas">The game board with the list of all characters on the board</param>
        /// <returns>A list of the characters in <paramref name="team"/>.</returns>
        public static List<Character> getNonCPUTeamList(int team, HexaGrid hexas)
        {
            List<Character> teamList = new List<Character>();

            foreach (Character c in hexas.charList)
                if (team == c.team)
                    teamList.Add(c);

            return teamList;
        }



        //edited by Rayane TALEB, group: L3C2
        //Author : ?
        /// <summary>
        /// Returns the enemy charactersCPU in the team declared in the parameters.
        /// </summary>
        /// <param name="team">The team we want the list of</param>
        /// <param name="charList">The list of all characters on the board</param>
        /// <returns>A list of CharacterCPU objects corresponding to the team's composition</returns>
        public static List<CharacterCPU> getEnemyTeamList(int ourteam, HexaGrid hexas)
        {
            List<CharacterCPU> enemyTeamList = new List<CharacterCPU>();

            foreach (Character c in hexas.charList)
                if (ourteam != c.team)
                    switch (c.charClass)
                    {
                        case CharClass.GUERRIER:
                            enemyTeamList.Add(new WarriorCPU(c, hexas));
                            break;
                        case CharClass.VOLEUR:
                            enemyTeamList.Add(new ThiefCPU(c, hexas));
                            break;
                        case CharClass.MAGE:
                            enemyTeamList.Add(new MageCPU(c, hexas));
                            break;
                        case CharClass.ARCHER:
                            enemyTeamList.Add(new ArcherCPU(c, hexas));
                            break;
                        case CharClass.SOIGNEUR:
                            enemyTeamList.Add(new HealerCPU(c, hexas));
                            break;
                        case CharClass.ENVOUTEUR:
                            enemyTeamList.Add(new SorcererCPU(c, hexas));
                            break;
                        case CharClass.VALKYRIE:
                            enemyTeamList.Add(new ValkyrieCPU(c, hexas));
                            break;
                        case CharClass.DRUIDE:
                            enemyTeamList.Add(new DruideCPU(c, hexas));
                            break;
                        default:
                            break;
                    }

            return enemyTeamList;
        }




        /// <summary>
        /// Returns the characters in the team declared in the parameters.
        /// </summary>
        /// <param name="team">The team we want the list of</param>
        /// <param name="charList">The list of all characters on the board</param>
        /// <returns>A list of CharacterCPU objects corresponding to the team's composition</returns>
        public static List<Character> getCharacterTeamList(int team, HexaGrid hexas)
        {
            List<Character> teamList = new List<Character>();

            foreach (Character c in hexas.charList)
                if (team == c.team)
                    teamList.Add(c);

            return teamList;
        }




        /// <summary>
        /// Returns the amount of characters in the team declared in parameters.
        /// </summary>
        /// <param name="team">The team we want the count of</param>
        /// <param name="charList">The list of all characters on the board</param>
        /// <returns>How many characters are in <paramref name="team"/></returns>
        public static int teamListCount(int team, HexaGrid hexas)
        {
            int count = 0;

            foreach (Character c in hexas.charList)
                if (team == c.team)
                    count++;

            return count;
        }




        /// <summary>
        /// Returns the enemy characters in the team declared in the parameters.
        /// </summary>
        /// <param name="team">The team we want the list of</param>
        /// <param name="charList">The list of all characters on the board</param>
        /// <returns>A list of CharacterCPU objects corresponding to the team's composition</returns>
        public static List<Character> getEnemyCharacterTeamList(int team, HexaGrid hexas)
        {
            List<CharacterCPU> cpuEnemyTeamList = getEnemyTeamList(team, hexas);
            List<Character> enemyTeamList = new List<Character>();

            foreach (CharacterCPU c in cpuEnemyTeamList)
                enemyTeamList.Add(c.Character);

            return enemyTeamList;
        }




        /// <summary>
        /// Calculates the optimal <c>Hexa</c> target for an AOE move, in addition to the default target
        /// using the default priority values.
        /// </summary>
        /// <param name="character">The character using the AOE</param>
        /// <param name="target">The default target the attack needs to hit</param>
        /// <param name="attack">The <c>CharDB.Attack</c> attack</param>
        /// <param name="attackType">The type of attack (ATK1/SKILL1)</param>
        /// <returns>The hexa where the attack needs to land to hit the target,
        /// and as many other units as possible according to the default priority list</returns>
        public static Hexa takeAOEIntoAccount(Character character, Character target, CharsDB.Attack attack, ActionType attackType)
        {
            return takeAOEIntoAccount(character, target, attack, null, attackType);
        }


        /// <summary>
        /// Calculates the optimal <c>Hexa</c> target for an AOE move, in addition to the default target
        /// using custom priority values defined in priority
        /// </summary>
        /// <param name="character">The character using the AOE</param>
        /// <param name="target">The default target the attack needs to hit</param>
        /// <param name="attack">The <c>CharDB.Attack</c> attack</param>
        /// <param name="attackType">The type of attack (ATK1/SKILL1)</param>
        /// <param name="priority">null if we're using the default priority list, custom priority list else</param>
        /// <returns>The hexa where the attack needs to land to hit the target, and as many other units as possible according to the priority list in parameter</returns>
        /// edited by Rayane TALEB L3C2
        public static Hexa takeAOEIntoAccount(Character character, Character target, CharsDB.Attack attack, Pair<List<Character>, List<int>> priority, ActionType attackType)
        {
            int maxDistance, cDistance;


            maxDistance = attack.rangeAoE + attack.range;
            cDistance = AIUtil.hexaGrid.getDistance(character.getX(), character.getY(), target.getX(), target.getY());

            //IF OUT OF RANGE
            if (cDistance > maxDistance)
            {
                Debug.Log("IF OUT OF RANGE");
                return null;
            }
            //IF MAX RANGE
            else if (cDistance == maxDistance)
            {
                Debug.Log("IF MAX RANGE");
                Point p = getShortestDistPoint(character, target, attack.range);
                if (p != null) return AIUtil.hexaGrid.getHexa(p);
                else return null;
            }
            //IF LESS THAN MAX RANGE
            else
            {
                Debug.Log("IF LESS THAN MAX RANGE");
                List<Point> map;
                int[] mapValue;

                //map = AIUtil.hexaGrid.findAllPaths(character.getX(), character.getY(), maxDistance);
                map = pointsFromClosestToFarthest(new Point(character.getX(), character.getY()), character, maxDistance);
                mapValue = new int[map.Count];
                for (int i = 0; i < map.Count; i++)
                {
                    mapValue[i] = 0;
                }

                //CALCULATING TARGET LIST
                foreach (Character c in AIUtil.hexaGrid.charList)
                {
                    if (c.team == target.team)
                    {
                        //Debug.Log("Team: "+c.team);

                        //getting prio value
                        int prioValue;
                        if (priority == null)
                        {
                            prioValue = (int)getDefaultPriorityValue(c);
                            //Debug.Log("Prio value: "+prioValue);
                        }
                        else
                        {
                            int prioIndex = 0;
                            prioIndex = priority.a.FindIndex(chara => System.Object.ReferenceEquals(chara, c));
                            prioValue = priority.b[prioIndex];
                        }

                        //adding prio value
                        int i = 0;
                        while (++i < map.Count && ((map[i].x != c.x) || (map[i].y != c.y)))
                        {
                            //Debug.Log("char: "+map[i].x+";"+map[i].y+"__"+c.x+";"+c.y);
                        }
                        if (i < map.Count)
                        {
                            mapValue[i] += prioValue;

                            //Debug.Log("Before adding"+mapValue[10]);
                            foreach (Point p in AIUtil.hexaGrid.getHexasWithinRange(c.getX(), c.getY(), attack.rangeAoE))
                            {
                                //Debug.Log("Point: "+p.x+";"+p.y);
                                i = 0;
                                while (++i < map.Count && ((map[i].x != p.x) || (map[i].y != p.y))) ;
                                if (i < map.Count)
                                {
                                    //Debug.Log("Before adding"+mapValue[i]+" -- "+map[i].x+";"+map[i].y+"__"+p.x+";"+p.y);
                                    mapValue[i] += prioValue;
                                    //Debug.Log("After adding"+mapValue[i]);
                                }
                            }
                        }
                    }
                }
                //CALCULATING OPTIMAL HEXA



                /*Point maxPos=map[0];
                int max = 0;
                for (int i=0;i<map.Count;i++) {
                    if (mapValue[i] > max) {
                        if (((attackType == ActionType.SKILL1) && isInRangeToUseSkill(new Point(character.x,character.y), map[i],maxDistance))
                                || ((attackType == ActionType.ATK1) && isInRangeToAttack(new Point(character.x,character.y), map[i],maxDistance))) {
                            max = mapValue[i];
                            maxPos = map[i];
                        }

                    }
                }*/

                Point maxPosOld = map[0];
                int maxOld = 0;

                for (int i = 0; i < map.Count; i++)
                {
                    if (mapValue[i] > maxOld)
                    {
                        maxOld = mapValue[i];
                        maxPosOld = map[i];
                    }
                }

                //RETURN OPTIMAL HEXA



                /*Debug.Log("Optimal hexa: " + max+" -- "+maxPos.x+";"+maxPos.y);
                if (attackType == ActionType.SKILL1) Debug.Log("Can attack that hexa: " + isInRangeToUseSkill(new Point(character.x,character.y), maxPos,maxDistance));
                else Debug.Log("Can attack that hexa: " + isInRangeToAttack(new Point(character.x,character.y), maxPos,maxDistance));

                Debug.Log("Map count: " + map.Count);
            */
                Debug.Log("Optimal hexa without check: " + maxOld + " -- " + maxPosOld.x + ";" + maxPosOld.y);
                Debug.Log("Attack type: " + attackType);
                Debug.Log("IsSkillReady: " + character.isSkillUp());
                Debug.Log("IsSkill2Ready: " + character.isSkill2Up());
                if (attackType == ActionType.SKILL2)
                    Debug.Log("Can skill2 attack that hexa: " + (isInRangeToUseSkill2(new Point(character.x, character.y), maxPosOld, maxDistance) && AIUtil.hexaGrid.hexaInSight(character.x, character.y, maxPosOld.x, maxPosOld.y, attack.range)));
                else if (attackType == ActionType.SKILL1)
                    Debug.Log("Can skill attack that hexa: " + (isInRangeToUseSkill(new Point(character.x, character.y), maxPosOld, maxDistance) && AIUtil.hexaGrid.hexaInSight(character.x, character.y, maxPosOld.x, maxPosOld.y, attack.range)));
                else
                    Debug.Log("Can normal attack that hexa: " + (isInRangeToAttack(new Point(character.x, character.y), maxPosOld, maxDistance) && AIUtil.hexaGrid.hexaInSight(character.x, character.y, maxPosOld.x, maxPosOld.y, attack.range)));

                /* String mapString = "[";
                 String mapStringValue = "[";
                 mapString+=map[0].x+","+map[0].y;
                 mapStringValue+=mapValue[0];
                 for (int i=1;i<map.Count;i++) {
                     mapString+=" - "+map[i].x+","+map[i].y;
                     mapStringValue+=" - "+mapValue[i];
                 }
                 mapString+="]";
                 mapStringValue+="]";
                 Debug.Log("List: "+mapString);
                 Debug.Log("ListValue: "+mapStringValue);*/


                Debug.Log("Character position (x): " + character.getX());
                Debug.Log("Character position (y): " + character.getY());
                //Debug.Log("Distance to point (x): "+(character.getX() - maxPos.x));
                //Debug.Log("Distance to point (y): "+(character.getY() - maxPos.y));
                if (((attackType == ActionType.SKILL2) && isInRangeToUseSkill2(new Point(character.x, character.y), maxPosOld, maxDistance) && AIUtil.hexaGrid.hexaInSight(character.x, character.y, maxPosOld.x, maxPosOld.y, attack.range))
                                || ((attackType == ActionType.SKILL1) && isInRangeToUseSkill(new Point(character.x, character.y), maxPosOld, maxDistance) && AIUtil.hexaGrid.hexaInSight(character.x, character.y, maxPosOld.x, maxPosOld.y, attack.range))
                                || ((attackType == ActionType.ATK1) && isInRangeToAttack(new Point(character.x, character.y), maxPosOld, maxDistance) && AIUtil.hexaGrid.hexaInSight(character.x, character.y, maxPosOld.x, maxPosOld.y, attack.range)))
                {
                    //Debug.Log("If (in range)");
                    return AIUtil.hexaGrid.getHexa(maxPosOld);
                }
                else
                {
                    Point targetPoint;
                    int distance = character.getX() + character.getY() - maxPosOld.x - maxPosOld.y;
                    if (distance == 1 || distance == -1)
                    {
                        targetPoint = new Point(target.getX(), target.getY());
                        return AIUtil.hexaGrid.getHexa(targetPoint);
                    }
                    else
                    {
                        //Debug.Log("Else (not in range); range: "+attack.range);
                        targetPoint = getShortestDistPoint(character, maxPosOld.x, maxPosOld.y, attack.range, true);
                        //Debug.Log("Distance to new (x): "+targetPoint.x);
                        //Debug.Log("Distance to new (y): "+targetPoint.y);
                        return AIUtil.hexaGrid.getHexa(targetPoint);
                    }
                }
            }
        }




        /// <summary>
        /// Returns the points around a hexagon given as a parameter.
        /// Sorted from closest to farthest from <paramref name="character"/>.
        /// </summary>
        /// <param name="point">The given hexa</param>
        /// <param name="character">The character to base the sorting on</param>
        /// <param name="steps">The number of steps around the hexa</param>
        /// <returns>A list of hexas sorted as intended</returns>
        public static List<Point> pointsFromClosestToFarthest(Point point, CharacterCPU character, int steps)
        {
            List<Point> points = pointsAroundHexa(point, steps);

            points.Sort(Comparer<Point>.Create((p1, p2) =>
            {
                // J'ai tenté avec getWalkingDistanceCPU, ça rame trop pour trop peu d'intérêt
                return (AIUtil.hexaGrid.getDistance(p1.x, p1.y, character.X, character.Y)
                        - AIUtil.hexaGrid.getDistance(p2.x, p2.y, character.X, character.Y));
            }));

            return points;
        }

        /// <summary>
        /// Returns the points around a hexagon given as a parameter.
        /// Sorted from closest to farthest from <paramref name="character"/>.
        /// </summary>
        /// <param name="point">The given hexa</param>
        /// <param name="character">The character to base the sorting on</param>
        /// <param name="steps">The number of steps around the hexa</param>
        /// <returns>A list of hexas sorted as intended</returns>
        public static List<Point> pointsFromClosestToFarthest(Point point, Character character, int steps)
        {
            List<Point> points = pointsAroundHexa(point, steps);

            points.Sort(Comparer<Point>.Create((p1, p2) =>
            {
                // J'ai tenté avec getWalkingDistanceCPU, ça rame trop pour trop peu d'intérêt
                return (AIUtil.hexaGrid.getDistance(p1.x, p1.y, character.x, character.y)
                        - AIUtil.hexaGrid.getDistance(p2.x, p2.y, character.x, character.y));
            }));

            return points;
        }

        /// <summary>
        /// Returns the points around a hexagon given as a parameter.
        /// Sorted from closest to farthest from <paramref name="character"/>.
        /// </summary>
        /// <param name="point">The given hexa</param>
        /// <param name="character">The character to base the sorting on</param>
        /// <returns>A list of hexas sorted as intended</returns>
        public static List<Point> pointsFromClosestToFarthest(CharacterCPU target, CharacterCPU character, int steps)
        {
            return pointsFromClosestToFarthest(new Point(target.X, target.Y), character, steps);
        }

        /// <summary>
        /// Returns the points around a hexagon given as a parameter.
        /// Sorted from closest to farthest from <paramref name="character"/>.
        /// </summary>
        /// <param name="point">The given hexa</param>
        /// <param name="character">The character to base the sorting on</param>
        /// <returns>A list of hexas sorted as intended</returns>
        public static List<Point> pointsFromClosestToFarthest(Point point, CharacterCPU character)
        {
            return pointsFromClosestToFarthest(point, character, 3);
        }

        /// <summary>
        /// Returns the points around a hexagon given as a parameter.
        /// Sorted from closest to farthest from <paramref name="character"/>.
        /// </summary>
        /// <param name="point">The given hexa</param>
        /// <param name="character">The character to base the sorting on</param>
        /// <returns>A list of hexas sorted as intended</returns>
        public static List<Point> pointsFromClosestToFarthest(CharacterCPU target, CharacterCPU character)
        {
            return pointsFromClosestToFarthest(new Point(target.X, target.Y), character, 3);
        }

        /// <summary>
        /// Returns the points around a hexagon given as a parameter.
        /// Sorted from closest to farthest from <paramref name="character"/>.
        /// </summary>
        /// <param name="point">The given hexa</param>
        /// <param name="character">The character to base the sorting on</param>
        /// <returns>A list of hexas sorted as intended</returns>
        public static List<Point> pointsFromClosestToFarthest(Character target, Character character, int steps)
        {
            return pointsFromClosestToFarthest(new Point(target.x, target.y), character, steps);
        }

        /// <summary>
        /// Returns the points around a hexagon given as a parameter.
        /// Sorted from closest to farthest from <paramref name="character"/>.
        /// </summary>
        /// <param name="point">The given hexa</param>
        /// <param name="character">The character to base the sorting on</param>
        /// <returns>A list of hexas sorted as intended</returns>
        public static List<Point> pointsFromClosestToFarthest(Point point, Character character)
        {
            return pointsFromClosestToFarthest(point, character, 3);
        }

        /// <summary>
        /// Returns the points around a hexagon given as a parameter.
        /// Sorted from closest to farthest from <paramref name="character"/>.
        /// </summary>
        /// <param name="point">The given hexa</param>
        /// <param name="character">The character to base the sorting on</param>
        /// <returns>A list of hexas sorted as intended</returns>
        public static List<Point> pointsFromClosestToFarthest(Character target, Character character)
        {
            return pointsFromClosestToFarthest(new Point(target.x, target.y), character, 3);
        }

        /// <summary>
        /// Returns the points around a hexagon given as a parameter.
        /// </summary>
        /// <param name="point">The given hexa</param>
        /// <param name="steps">Number of points around the hexagon</param>
        /// <returns>A list of hexas around it</returns>
        public static List<Point> pointsAroundHexa(Point point, int steps)
        {
            List<Point> around = AIUtil.hexaGrid.getHexasWithinRange(point.x, point.y, steps);
            bool hexasAllFull = true;

            if (steps < 3)
            {
                foreach (Point p in around)
                    if (AIUtil.hexaGrid.getHexa(p).charOn == null)
                        hexasAllFull = false;
            }
            else hexasAllFull = false;

            if (hexasAllFull) around = AIUtil.hexaGrid.getHexasWithinRange(point.x, point.y, 3);

            return around;
        }

        /// <summary>
        /// Returns the points around a hexagon given as a parameter.
        /// </summary>
        /// <param name="point">The given hexa</param>
        /// <returns>A list of hexas around it</returns>
        public static List<Point> pointsAroundHexa(Point point)
        {
            return pointsAroundHexa(point, 3);
        }

        /// <summary>
        /// Returns the points around a character given as a parameter.
        /// </summary>
        /// <param name="character">The given character</param>
        /// <param name="steps">Number of points around the character</param>
        /// <returns>A list of hexas around it</returns>
        public static List<Point> pointsAroundHexa(Character character, int steps)
        {
            return pointsAroundHexa(new Point(character.x, character.y), steps);
        }

        /// <summary>
        /// Returns the points around a character given as a parameter.
        /// </summary>
        /// <param name="character">The given character</param>
        /// <returns>A list of hexas around it</returns>
        public static List<Point> pointsAroundHexa(Character character)
        {
            return pointsAroundHexa(new Point(character.x, character.y));
        }


        //edited by Rayane TALEB L3C2
        //Author : ?
        /// <summary>
        /// Returns a string corresponding to the character's class.
        /// </summary>
        /// <param name="c">The character</param>
        /// <returns>The character's class as a string</returns>
        public static string charToString(Character c)
        {
            if (c == null) return "null";

            switch (c.charClass)
            {
                case CharClass.GUERRIER:
                    return "GUERRIER";
                case CharClass.VOLEUR:
                    return "VOLEUR";
                case CharClass.MAGE:
                    return "MAGE";
                case CharClass.ARCHER:
                    return "ARCHER";
                case CharClass.SOIGNEUR:
                    return "SOIGNEUR";
                case CharClass.ENVOUTEUR:
                    return "ENVOUTEUR";
                case CharClass.VALKYRIE:
                    return "VALKYRIE";
                case CharClass.DRUIDE:
                    return "DRUIDE";
                default:
                    return "null";
            }
        }

        public static Character identifyAllyClassPriority(int currentTeam, HexaGrid hexas)
        {
            List<Character> characterTeamList = getCharacterTeamList(currentTeam, hexas);
            Character priority = null;
            float priorityValue = 1001;

            foreach (Character ally in characterTeamList)
            {
                // Valkyrie > Voleur > Guerrier > Archer > Mage >>> Soigneur
                switch (ally.charClass)
                {
                    case CharClass.GUERRIER:
                        if (priorityValue > 3)
                        {
                            priorityValue = 3;
                            priority = ally;
                        }
                        break;
                    case CharClass.VOLEUR:
                        if (priorityValue > 1)
                        {
                            priorityValue = 1;
                            priority = ally;
                        }
                        break;
                    case CharClass.VALKYRIE:
                        if (priorityValue > 2)
                        {
                            priorityValue = 2;
                            priority = ally;
                        }
                        break;
                    case CharClass.MAGE:
                        if (priorityValue > 4)
                        {
                            priorityValue = 4;
                            priority = ally;
                        }
                        break;
                    case CharClass.DRUIDE:
                        if (priorityValue > 5)
                        {
                            priorityValue = 5;
                            priority = ally;
                        }
                        break;
                    case CharClass.ARCHER:
                        if (priorityValue > 6)
                        {
                            priorityValue = 6;
                            priority = ally;
                        }
                        break;
                    case CharClass.ENVOUTEUR:
                        if (priorityValue > 7)
                        {
                            priorityValue = 7;
                            priority = ally;
                        }
                        break;
                    case CharClass.SOIGNEUR:
                        if (priorityValue > 1000)
                        {
                            priorityValue = 1000;
                            priority = ally;
                        }
                        break;
                    default:
                        break;
                }
            }
            return priority;
        }

        /// <summary>
        /// Returns true if all team member of the currenChar are full life, or close to full life
        /// </summary>
        /// <param name="c">The character</param>
        /// <returns>The character's class as a string</returns>
        public static bool allTeamFullLife(int currentTeam, HexaGrid hexas)
        {
            List<Character> characterTeamList = getCharacterTeamList(currentTeam, hexas);
            foreach (Character ally in characterTeamList)
                if (ally.HP <= ally.HPmax - 2)
                    return false;
            return true;
        }

        /// <summary>
        /// Returns the List<Character> ranked by their HP, from lowest to highest
        /// </summary>
        /// <param name="c">The character</param>
        /// <returns>The character's class as a string</returns>
        public static List<CharacterCPU> allyRankedByMissingHP(int currentTeam, HexaGrid hexas)
        {

            CharacterCPU temporaire = null;
            List<CharacterCPU> characterTeamList = getTeamList(currentTeam, hexas);
            List<CharacterCPU> characterTeamListRankedByMissingHP = new List<CharacterCPU>();


            foreach (CharacterCPU ally in characterTeamList)
                if (ally.Character.HP < ally.Character.HPmax && ally.Character.charClass != CharClass.SOIGNEUR)
                    characterTeamListRankedByMissingHP.Add(ally);


            for (int i = 0; i < characterTeamListRankedByMissingHP.Count; i++)
                for (int j = 0; j < characterTeamListRankedByMissingHP.Count - 1; j++)
                    if (characterTeamListRankedByMissingHP[j].HP > characterTeamListRankedByMissingHP[j + 1].HP)
                    {
                        temporaire = characterTeamListRankedByMissingHP[j + 1];
                        characterTeamListRankedByMissingHP[j + 1] = characterTeamListRankedByMissingHP[j];
                        characterTeamListRankedByMissingHP[j] = temporaire;
                    }


            return characterTeamListRankedByMissingHP;
        }

        /// <summary>
        /// Returns the List<Character> ranked by their HP, from lowest to highest
        /// </summary>
        /// <param name="c">The character</param>
        /// <returns>The character's class as a string</returns>
        public static CharacterCPU choosingAlly(List<CharacterCPU> allyRankedByMissingHP, int currentTeam, HexaGrid hexas, Character healer)
        {
            List<CharacterCPU> inRange = new List<CharacterCPU>();
            List<CharacterCPU> minus3 = new List<CharacterCPU>();

            // Find ally that are inRange
            foreach (CharacterCPU ally in allyRankedByMissingHP)
                if (ally.Character != healer && isInRangeAttack(ally.Character, healer))
                    inRange.Add(ally);

            // Find ally that lost more than 3 hp
            foreach (CharacterCPU ally in allyRankedByMissingHP)
                if (ally.Character != healer && ally.Character.HP < ally.Character.HPmax - 3)
                    minus3.Add(ally);

            // Target ally in range first
            foreach (CharacterCPU ally in inRange)
                if (ally.Character != healer && ally.Character.HP < ally.Character.HPmax - 3)
                    return ally;

            // Target lowest ally after
            foreach (CharacterCPU ally in minus3)
                if (ally.Character != healer && isInRangeAttack(ally.Character, healer))
                    return ally;

            return allyRankedByMissingHP[0];
        }

        /// <summary>
        /// Returns the List<Character> ranked by their HP, from lowest to highest
        /// </summary>
        /// <param name="c">The character</param>
        /// <returns>The character's class as a string</returns>
        public static List<CharacterCPU> allyRankedByMissingHP_Character(int currentTeam, HexaGrid hexas)
        {

            CharacterCPU temporaire = null;
            List<CharacterCPU> characterTeamList = getTeamList(currentTeam, hexas);
            List<CharacterCPU> characterTeamListRankedByMissingHP = new List<CharacterCPU>();


            foreach (CharacterCPU ally in characterTeamList)
                if (ally.Character.HP < ally.Character.getClassData().maxHP)
                    characterTeamListRankedByMissingHP.Add(ally);


            for (int i = 0; i < characterTeamListRankedByMissingHP.Count; i++)
                for (int j = 0; j < characterTeamListRankedByMissingHP.Count - 1; j++)
                    if (characterTeamListRankedByMissingHP[j].HP > characterTeamListRankedByMissingHP[j + 1].HP)
                    {
                        temporaire = characterTeamListRankedByMissingHP[j + 1];
                        characterTeamListRankedByMissingHP[j + 1] = characterTeamListRankedByMissingHP[j];
                        characterTeamListRankedByMissingHP[j] = temporaire;
                    }


            return characterTeamListRankedByMissingHP;
        }

        /// <summary>
        /// Returns true if an ally isn't full life
        /// </summary>
        /// <param name="c">The character</param> 
        /// <returns>The character's class as a string</returns>
        public static bool allyInNeed(int currentTeam, HexaGrid hexas)
        {
            List<Character> characterTeam = getCharacterTeamList(currentTeam, hexas);

            foreach (Character ally in characterTeam)
                if (ally.HP < ally.HPmax && ally.charClass != CharClass.SOIGNEUR)
                    return true;

            return false;
        }

        /// <summary>
        /// Sorts toSort putting characters in the order of charsInOrder.
        /// </summary>
        /// <param name="toSort">The list to sort</param>
        /// <param name="charsInOrder">The sorting order</param>
        /// <returns>The sorted list</returns>
        public static List<CharacterCPU> order(List<CharacterCPU> toSort, params CharClass[] charsInOrder)
        {
            List<CharacterCPU> newList = new List<CharacterCPU>();

            foreach (CharClass classOrder in charsInOrder)
                foreach (CharacterCPU c in toSort)
                    if (c.Character.getCharClass() == classOrder)
                        newList.Add(c);

            foreach (CharacterCPU c in toSort)
                if (!newList.Contains(c))
                    newList.Add(c);

            return newList;
        }

        /// <summary>
        /// Returns an action list in string form.
        /// </summary>
        /// <param name="actionList">The list of actions</param>
        /// <returns>A string detailing the contents of the list</returns>
        public static string actionListPrinter(List<(Character, ActionAIPos)> actionList)
        {
            StringBuilder printer = new StringBuilder();
            foreach ((Character chr, ActionAIPos act) o in actionList)
                if (o.act.action == ActionType.SKIP)
                    printer.Append(charToString(o.chr) + " " + o.chr.x + " " + o.chr.y + " : "
                    + o.act.action + "\n");
                else
                    printer.Append(charToString(o.chr) + " " + o.chr.x + " " + o.chr.y + " : "
                            + o.act.action + " to " + o.act.pos.x + " " + o.act.pos.y + "\n");

            return printer.ToString();
        }

        /// <summary>
        /// Returns a position according to a character's class and the target/leader axis.
        /// </summary>
        /// <param name="target">Group target</param>
        /// <param name="leader">Group leader</param>
        /// <param name="charClass">The class of the character who called this method</param>
        /// <param name="hexas">The game board</param>
        /// <returns>The point the character should stand on</returns>
        public static Point getTacticalFormation(CharacterCPU target, CharacterCPU leader, CharClass charClass, HexaGrid hexas)
        {
            Point tacticalPos;
            double targetLeaderAxisUnitX = (target.X - leader.X) /
                    (double)hexas.getDistance(target.X, target.Y, leader.X, leader.Y),
                targetLeaderAxisUnitY = (target.Y - leader.Y) /
                    (double)hexas.getDistance(target.X, target.Y, leader.X, leader.Y);

            switch (charClass)
            {
                case CharClass.GUERRIER:
                case CharClass.VOLEUR:
                    tacticalPos = new Point((int)Math.Round(leader.X - targetLeaderAxisUnitY * 2),
                        (int)Math.Round(leader.Y + targetLeaderAxisUnitX * 2));
                    break;

                case CharClass.MAGE:
                    tacticalPos = new Point((int)Math.Round(leader.X + targetLeaderAxisUnitY * 3),
                        (int)Math.Round(leader.Y - targetLeaderAxisUnitX * 3));
                    break;

                case CharClass.ARCHER:
                    tacticalPos = new Point((int)Math.Round(leader.X - targetLeaderAxisUnitX * 2 - targetLeaderAxisUnitY),
                        (int)Math.Round(leader.Y - targetLeaderAxisUnitY * 2 + targetLeaderAxisUnitX));
                    break;

                case CharClass.SOIGNEUR:
                    tacticalPos = new Point((int)Math.Round(leader.X - targetLeaderAxisUnitX * 4 + targetLeaderAxisUnitY),
                        (int)Math.Round(leader.Y - targetLeaderAxisUnitY * 4 - targetLeaderAxisUnitX));
                    break;
                case CharClass.ENVOUTEUR:
                    tacticalPos = new Point((int)Math.Round(leader.X - targetLeaderAxisUnitX * 3 + targetLeaderAxisUnitY),
                        (int)Math.Round(leader.Y - targetLeaderAxisUnitY * 3 - targetLeaderAxisUnitX));
                    break;
                case CharClass.DRUIDE:
                    tacticalPos = new Point((int)Math.Round(leader.X - targetLeaderAxisUnitX * 2 - targetLeaderAxisUnitY),
                        (int)Math.Round(leader.Y - targetLeaderAxisUnitY * 2 + targetLeaderAxisUnitX));
                    break;
                case CharClass.VALKYRIE:
                default:
                    tacticalPos = new Point(leader.X, leader.Y);
                    Debug.Log("Default case called in UtilCPU.getTacticalFormation()");
                    break;
            }

            /*Debug.Log(charClass + " : " + tacticalPos.x + " " + tacticalPos.y
                + "\nXaxis = " + targetLeaderAxisUnitX + ", Yaxis = " + targetLeaderAxisUnitY);*/

            return tacticalPos;
        }

        /// <summary>
        /// Checks if any unit in currentTeam can hit someone in the enemy team.
        /// </summary>
        /// <param name="currentTeam">The team of the current player</param>
        /// <param name="hexas">The game board</param>
        /// <returns>True if currentTeam can hit the enemy team and false otherwise</returns>
        public static bool canTargetEnemyTeam(int currentTeam, HexaGrid hexas)
        {
            List<Character> ourTeam = getNonCPUTeamList(currentTeam, hexas),
                enemyTeam = getNonCPUTeamList(currentTeam == 0 ? 1 : 0, hexas);

            foreach (Character ally in ourTeam)
                foreach (Character enemy in enemyTeam)
                    if (isInTotalRange(ally, enemy))
                        return true;

            return false;
        }

        /// <summary>
        /// Counts how many units in the enemy team can hit someone in currentTeam.
        /// </summary>
        /// <param name="currentTeam">The team of the current player</param>
        /// <param name="hexas">The game board</param>
        /// <returns>True if currentTeam can hit the enemy team and false otherwise</returns>
        public static int enemyInRangeCounter(int currentTeam, HexaGrid hexas)
        {
            int returnValue = 0;
            List<Character> ourTeam = getNonCPUTeamList(currentTeam, hexas),
                enemyTeam = getNonCPUTeamList(currentTeam == 0 ? 1 : 0, hexas);

            foreach (Character enemy in enemyTeam)
                foreach (Character ally in ourTeam)
                    if (isInTotalRange(enemy, ally))
                        returnValue++;

            return returnValue;
        }
        /// <summary>
        /// Returns a list sorted by proximity to the current team.
        /// </summary>
        /// <param name="currentTeam">The team of the current player</param>
        /// <param name="hexas">The game board</param>
        /// <returns>A list sorted from closest to farthest enemy</returns>
        public static List<CharacterCPU> closestToFarthestEnemySorter(int currentTeam, HexaGrid hexas)
        {
            List<CharacterCPU> ourTeam = getTeamList(currentTeam, hexas),
                enemyTeam = getTeamList(currentTeam == 0 ? 1 : 0, hexas);

            // Sorting from closest to farthest based on the sum of distances between the enemy and all allies
            enemyTeam.Sort(Comparer<CharacterCPU>.Create((enemy1, enemy2) =>
            {
                int enemyOneDistanceScore = 0,
                    enemyTwoDistanceScore = 0;

                foreach (CharacterCPU ally in ourTeam)
                {
                    enemyOneDistanceScore += hexas.getWalkingDistance(ally.X, ally.Y, enemy1.X, enemy1.Y);
                    enemyTwoDistanceScore += hexas.getWalkingDistance(ally.X, ally.Y, enemy2.X, enemy2.Y);
                }

                return enemyOneDistanceScore - enemyTwoDistanceScore;
            }));

            return enemyTeam;
        }

        /// <summary>
        /// Checks the amount of health of each character in <paramref name="currentTeam"/>.
        /// </summary>
        /// <remarks>This method specifically checks for units who've lost more than what the healer can heal in a turn.</remarks>
        /// <param name="currentTeam">The team we want to check</param>
        /// <param name="hexas">The game board</param>
        /// <returns>True if a unit has lost more than what the healer can heal in a turn.</returns>
        public static bool healthCheck(int currentTeam, HexaGrid hexas)
        {
            CharsDB.CharacterDB healerData = CharsDB.list[(int)CharClass.SOIGNEUR];

            foreach (Character c in getNonCPUTeamList(currentTeam, hexas))
                if (c.HP <= c.getClassData().maxHP - healerData.basicAttack.effectValue * healerData.basePA)
                    if (c.charClass != CharClass.SOIGNEUR)
                        return gotHealerInTeam(currentTeam);

            return false;
        }

        /// <summary>
        /// Returns a CharacterCPU list with this configuration : 
        /// 1) The character to heal
        /// 2) Healers
        /// 3) Sorcerers
        /// 4) The other units
        /// </summary>
        /// <param name="currentTeam">The team of the current player</param>
        /// <param name="hexas">The game board</param>
        /// <returns>See the summary</returns>
        public static List<CharacterCPU> getHealingFocusedList(int currentTeam, HexaGrid hexas)
        {
            List<CharacterCPU> teamList = order(getTeamList(currentTeam, hexas),
                CharClass.SOIGNEUR, CharClass.ENVOUTEUR);
            CharacterCPU toHeal = teamList[0];

            for (int i = 0; i < teamList.Count; i++)
                if (teamList[i].Character.charClass != CharClass.SOIGNEUR)
                {
                    // if teamList[i] has lost more health than toHeal
                    if (teamList[i].Character.getClassData().maxHP - teamList[i].Character.HP >
                        toHeal.Character.getClassData().maxHP - toHeal.Character.HP)

                        toHeal = teamList[i];
                }

            teamList.Remove(toHeal);
            teamList.Insert(0, toHeal);

            /*
            Debug.Log("Returning : ");

            foreach (CharacterCPU c in teamList)
                Debug.Log(charToString(c.Character) + " ( " + c.Character.HP + "/" + c.Character.getClassData().maxHP + " )");
                */

            return teamList;
        }

        /// <summary>
        /// Returns a list of characters that haven't already played their turn.
        /// </summary>
        /// <param name="teamList">The team list</param>
        /// <param name="actions">The actions that have been called this turn</param>
        /// <returns>See summary</returns>
        public static List<CharacterCPU> remaining(List<CharacterCPU> teamList, List<(Character, ActionAIPos)> actions)
        {
            List<CharacterCPU> remaining = new List<CharacterCPU>();
            remaining.AddRange(teamList);

            foreach ((Character chr, ActionAIPos act) called in actions)
                foreach (CharacterCPU c in teamList)
                    if (c.Character == called.chr)
                        remaining.Remove(c);

            return remaining;
        }

        /// <summary>
        /// Return the best Point for a healer to use his spell
        /// </summary>
        /// <param name="toSort">The list to sort</param>
        /// <param name="charsInOrder">The sorting order</param>
        /// <returns>The sorted list</returns>
        public static Point bestPointForAOEHeal(int currentTeam, HexaGrid hexas, Character healer)
        {
            List<Point> spellRange = AIUtil.hexaGrid.findHexasInSight2(healer.x, healer.y, 5);

            List<int> bestHexa = new List<int>();
            for (int j = 0; j < spellRange.Count; j++)
                bestHexa.Add(0);

            int i = 0;
            Hexa hexaP = null;
            Hexa centerHexa = null;

            foreach (Point center in spellRange)
            {
                centerHexa = hexas.getHexa(center.x, center.y);
                if (centerHexa != null && centerHexa.type == HexaType.GROUND && centerHexa.charOn != null)
                    if ((centerHexa.charOn.team == currentTeam) && (centerHexa.charOn.HP <= centerHexa.charOn.HPmax - 3))
                    {
                        bestHexa[i]++;
                    }

                foreach (Point p in pointsAroundHexa(center, 1))
                {
                    hexaP = hexas.getHexa(p.x, p.y);
                    if (hexaP != null && hexaP.type == HexaType.GROUND && hexaP.charOn != null)
                    {
                        if ((hexaP.charOn.team == currentTeam) && (hexaP.charOn.HP <= hexaP.charOn.HPmax - 3))
                        {
                            bestHexa[i]++;
                        }
                    }
                }
                i++;
            }

            return spellRange[bestHexa.IndexOf(bestHexa.Max())];
        }

        /// <summary>
        /// Return true if the healer should use his spell
        /// </summary>
        /// <param name="toSort">The list to sort</param>
        /// <param name="charsInOrder">The sorting order</param>
        /// <returns>The sorted list</returns>
        public static bool canAOEHeal(int currentTeam, HexaGrid hexas, Character healer)
        {
            List<Point> spellRange = AIUtil.hexaGrid.findHexasInSight2(healer.x, healer.y, 5);

            List<int> bestHexa = new List<int>();
            for (int j = 0; j < spellRange.Count; j++)
                bestHexa.Add(0);

            int i = 0;
            Hexa hexaP = null;
            Hexa centerHexa = null;
            int value = 0;

            foreach (Point center in spellRange)
            {
                centerHexa = hexas.getHexa(center.x, center.y);
                if (centerHexa != null && centerHexa.type == HexaType.GROUND && centerHexa.charOn != null)
                    if ((centerHexa.charOn.team == currentTeam) && (centerHexa.charOn.HP <= centerHexa.charOn.HPmax - 3))
                    {
                        value++;
                    }

                foreach (Point p in pointsAroundHexa(center, 1))
                {
                    hexaP = hexas.getHexa(p.x, p.y);
                    if (hexaP != null && hexaP.type == HexaType.GROUND && hexaP.charOn != null)
                        if ((hexaP.charOn.team == currentTeam) && (hexaP.charOn.HP <= hexaP.charOn.HPmax - 3))
                        {
                            value++;
                        }

                }
                //Debug.Log("value:" + value);
                if (value > 1)
                {
                    //Debug.Log("AOE HEAL TRUE");
                    return true;
                }
                i++;
                value = 0;
            }



            //Debug.Log("AOE HEAL FALSE");
            return false;
        }

        /// <summary>
        /// Return the best Point for a mage to use his spell
        /// </summary>
        /// <param name="toSort">The list to sort</param>
        /// <param name="charsInOrder">The sorting order</param>
        /// <returns>The sorted list</returns>
        public static Point bestPointForAOEDamage(int currentTeam, HexaGrid hexas, int x, int y, int range, int aoeValue)
        {
            List<Point> spellRange = AIUtil.hexaGrid.findHexasInSight2(x, y, range);

            List<int> bestHexa = new List<int>();
            for (int j = 0; j < spellRange.Count; j++)
                bestHexa.Add(0);

            int i = 0;
            Hexa hexaP = null;
            Hexa centerHexa = null;

            foreach (Point center in spellRange)
            {
                centerHexa = hexas.getHexa(center.x, center.y);
                if (centerHexa != null && centerHexa.type == HexaType.GROUND && centerHexa.charOn != null)
                    if (centerHexa.charOn.team != currentTeam)
                    {
                        bestHexa[i]++;
                    }

                foreach (Point p in pointsAroundHexa(center, aoeValue))
                {
                    hexaP = hexas.getHexa(p.x, p.y);
                    if (hexaP != null && hexaP.type == HexaType.GROUND && hexaP.charOn != null)
                    {
                        if (hexaP.charOn.team != currentTeam)
                        {
                            bestHexa[i]++;
                        }
                    }
                }
                i++;
            }
            // Check if we get a better value with 3 step
            int value = betterAOEDamage(currentTeam, hexas, x, y, 3);


            return spellRange[bestHexa.IndexOf(bestHexa.Max())];
        }

        /// <summary>
        /// Return true if we can find a better point for AOEDamage within x step
        /// </summary>
        /// <param name="toSort">The list to sort</param>
        /// <param name="charsInOrder">The sorting order</param>
        /// <returns>The sorted list</returns>
        public static int betterAOEDamage(int currentTeam, HexaGrid hexas, int x, int y, int maxStep)
        {
            Point magePos = new Point(x, y);
            List<Point> posAroundCharacter = pointsAroundHexa(magePos, 3);
            List<int> bestHexa = new List<int>();

            foreach (Point pos in posAroundCharacter)
            {
                List<Point> spellRange = AIUtil.hexaGrid.findHexasInSight2(pos.x, pos.y, 5);

                for (int j = 0; j < spellRange.Count; j++)
                    bestHexa.Add(0);

                int i = 0;
                Hexa hexaP = null;
                Hexa centerHexa = null;

                foreach (Point center in spellRange)
                {
                    centerHexa = hexas.getHexa(center.x, center.y);
                    if (centerHexa != null && centerHexa.type == HexaType.GROUND && centerHexa.charOn != null)
                        if (centerHexa.charOn.team != currentTeam)
                        {
                            bestHexa[i]++;
                        }

                    foreach (Point p in pointsAroundHexa(center, 2))
                    {
                        hexaP = hexas.getHexa(p.x, p.y);
                        if (hexaP != null && hexaP.type == HexaType.GROUND && hexaP.charOn != null)
                        {
                            if (hexaP.charOn.team != currentTeam)
                            {
                                bestHexa[i]++;
                            }
                        }
                    }
                    i++;
                }
            }
            int betterAOE = bestHexa.IndexOf(bestHexa.Max());

            return bestHexa.IndexOf(bestHexa.Max());
        }

        /// <summary>
        /// Return true if the mage should use his spell
        /// </summary>
        /// <param name="toSort">The list to sort</param>
        /// <param name="charsInOrder">The sorting order</param>
        /// <returns>The sorted list</returns>
        public static bool canAOEDamage(int currentTeam, HexaGrid hexas, int x, int y, int range, int aoeValue)
        {
            List<Point> spellRange = AIUtil.hexaGrid.findHexasInSight2(x, y, range);

            List<int> bestHexa = new List<int>();
            for (int j = 0; j < spellRange.Count; j++)
                bestHexa.Add(0);

            int i = 0;
            Hexa hexaP = null;
            Hexa centerHexa = null;
            int value = 0;

            foreach (Point center in spellRange)
            {
                centerHexa = hexas.getHexa(center.x, center.y);
                if (centerHexa != null && (centerHexa.type == HexaType.GROUND || centerHexa.type == HexaType.BONUS) && centerHexa.charOn != null)
                    if (centerHexa.charOn.team != currentTeam)
                    {
                        value++;
                    }

                foreach (Point p in pointsAroundHexa(center, aoeValue))
                {
                    hexaP = hexas.getHexa(p.x, p.y);
                    if (hexaP != null && (hexaP.type == HexaType.GROUND || hexaP.type == HexaType.BONUS) && hexaP.charOn != null)
                        if (hexaP.charOn.team != currentTeam)
                        {
                            value++;
                        }

                }
                //Debug.Log("value:" + value);
                if (value > 1)
                {
                    //Debug.Log("AOE DAMAGE TRUE");
                    return true;
                }
                i++;
                value = 0;
            }



            //Debug.Log("AOE DAMAGE FALSE");
            return false;
        }

        /// <summary>
        /// Return the best Point for a envouteur to use his spell
        /// </summary>
        /// <param name="toSort">The list to sort</param>
        /// <param name="charsInOrder">The sorting order</param>
        /// <returns>The sorted list</returns>
        public static Point bestPointForAOEBoost(int currentTeam, HexaGrid hexas, Character envouteur)
        {
            List<Point> spellRange = AIUtil.hexaGrid.findHexasInSight2(envouteur.x, envouteur.y, 4);

            List<int> bestHexa = new List<int>();
            for (int j = 0; j < spellRange.Count; j++)
                bestHexa.Add(0);

            int i = 0;
            Hexa hexaP = null;
            Hexa centerHexa = null;

            foreach (Point center in spellRange)
            {
                centerHexa = hexas.getHexa(center.x, center.y);
                if (centerHexa != null && centerHexa.type == HexaType.GROUND && centerHexa.charOn != null)
                    if (centerHexa.charOn.team == currentTeam)
                    {
                        bestHexa[i]++;
                    }

                foreach (Point p in pointsAroundHexa(center, 2))
                {
                    hexaP = hexas.getHexa(p.x, p.y);
                    if (hexaP != null && hexaP.type == HexaType.GROUND && hexaP.charOn != null)
                    {
                        if (hexaP.charOn.team == currentTeam)
                        {
                            bestHexa[i]++;
                        }
                    }
                }
                i++;
            }

            return spellRange[bestHexa.IndexOf(bestHexa.Max())];
        }

        /// <summary>
        /// Return true if the healer can move to he's ally without puting himself at risk
        /// </summary>
        /// <param name="toSort">The list to sort</param>
        /// <param name="charsInOrder">The sorting order</param>
        /// <returns>The sorted list</returns>
        public static bool moveTowardsWithoutRisk(int currentTeam, HexaGrid hexas, Character ally)
        {
            List<Character> enemyTeam = getEnemyCharacterTeamList(currentTeam, hexas);
            int count = 0;

            foreach (Character enemy in enemyTeam)
                if (enemy.charClass != CharClass.ARCHER)
                    if (isInRangeAttack(ally, enemy))
                        count++;

            if (count <= 1)
                return true;

            return false;
        }

        /// <summary>
        /// Return the best point for the healer to go, if he want to get closer to an ally
        /// </summary>
        /// <param name="toSort">The list to sort</param>
        /// <param name="charsInOrder">The sorting order</param>
        /// <returns>The sorted list</returns>
        public static Point healerFollowWithoutRisk(int currentTeam, HexaGrid hexas, Character healer, Character ally)
        {
            Debug.Log("healerFollowWithoutRisk");
            Dictionary<Point, int> valueList = new Dictionary<Point, int>();
            Point healerPos = new Point(healer.x, healer.y);
            Hexa goTo = null;
            int min = 999;

            foreach (Point p in pointsAroundHexa(healerPos, 3))
            {
                goTo = hexas.getHexa(p.x, p.y);
                if (goTo != null && goTo.type == HexaType.GROUND && goTo.charOn == null)
                    valueList.Add(p, AIUtil.hexaGrid.getWalkingDistance(p.x, p.y, ally.x, ally.y));
            }

            valueList.OrderBy(x => x.Value).Select(x => x.Key);


            // Just return the closest point
            Point closest = null;
            foreach (Point p in pointsAroundHexa(healerPos, 3))
                if (AIUtil.hexaGrid.getWalkingDistance(p.x, p.y, ally.x, ally.y) < min)
                {
                    min = AIUtil.hexaGrid.getWalkingDistance(p.x, p.y, ally.x, ally.y);
                    closest = p;
                }
            foreach (KeyValuePair<Point, int> position in valueList)
            {
                if (howManyEnemyCanAttackPoint(position.Key, currentTeam, hexas) < 2)
                    return position.Key;
            }

            return closest;
        }

        /// <summary>
        /// Return true if the character isn't isolated from his team
        /// </summary>
        /// <param name="toSort">The list to sort</param>
        /// <param name="charsInOrder">The sorting order</param>
        /// <returns>The sorted list</returns>
        public static bool notToFarAway(int currentTeam, HexaGrid hexas, Character c)
        {
            List<Character> enemyTeam = getEnemyCharacterTeamList(currentTeam, hexas);
            List<Character> allyTeam = getCharacterTeamList(currentTeam, hexas);
            int enemyCount = 0;
            int allyCount = 0;


            foreach (Character enemy in enemyTeam)
                enemyCount += AIUtil.hexaGrid.getWalkingDistance(c.x, c.y, enemy.x, enemy.y);


            foreach (Character ally in allyTeam)
                allyCount += AIUtil.hexaGrid.getWalkingDistance(c.x, c.y, ally.x, ally.y);


            if (enemyCount < allyCount && allTeamFullLife(currentTeam, hexas) && getEnemyTargetsAvailable(c).Count == 0)
            {
                Debug.Log("To risky, i'll wait");
                return false;
            }

            return true;
        }


        //edited by GOUVEIA Klaus, group: L3Q1
        //Author : ?
        /// <summary>
        /// Help the Envouteur to choose a target to boost
        /// </summary>
        /// <param name="toSort">The list to sort</param>
        /// <param name="charsInOrder">The sorting order</param>
        /// <returns>The sorted list</returns>
        public static Character allyToBoost(int currentTeam, HexaGrid hexas)
        {
            List<Character> teamList = getCharacterTeamList(currentTeam, hexas);
            List<Character> possibleAlly = new List<Character>();

            foreach (Character ally in teamList)
            {
                if (getEnemyTargetsAvailable(ally).Count > 0 && ally.charClass == CharClass.VOLEUR)
                    return ally;
                else if (getEnemyTargetsAvailable(ally).Count > 0 && ally.charClass == CharClass.GUERRIER)
                    return ally;
                else if (getEnemyTargetsAvailable(ally).Count > 0 && ally.charClass == CharClass.VALKYRIE)
                    return ally;
                else if (getEnemyTargetsAvailable(ally).Count > 0 && ally.charClass != CharClass.SOIGNEUR && ally.charClass != CharClass.ENVOUTEUR)
                    possibleAlly.Add(ally);
            }


            return possibleAlly[0];
        }

        /// <summary>
        /// Returns true if atleast one member of currentTeam can hit an enemy 
        /// Takes into account whether <code>c</code> has his skill ready or not.
        /// </summary>
        /// <param name="c">The character the method uses</param>
        /// <returns>A list of all the characters <code>c</code> can target</returns>
        public static bool atLeastOnInRange(int currentTeam, HexaGrid hexas)
        {

            List<Character> teamList = getCharacterTeamList(currentTeam, hexas);

            foreach (Character ally in teamList)
                if (getEnemyTargetsAvailable(ally).Count > 0 && ally.charClass != CharClass.SOIGNEUR && ally.charClass != CharClass.ENVOUTEUR)
                    return true;

            //Debug.Log("no one in range atm");
            return false;
        }

        /// <summary>
        /// Return true if the character isn't isolated from his team
        /// </summary>
        /// <param name="toSort">The list to sort</param>
        /// <param name="charsInOrder">The sorting order</param>
        /// <returns>The sorted list</returns>
        public static bool moveWithoutRisk(int currentTeam, HexaGrid hexas, Character c)
        {
            List<Character> enemyTeam = getEnemyCharacterTeamList(currentTeam, hexas);

            if (allTeamFullLife(currentTeam, hexas) && !atLeastOnInRange(currentTeam, hexas)) //start of the game
                foreach (Character enemyC in enemyTeam)
                    if (enemyC.getClassData().basicAttack.range > AIUtil.hexaGrid.getWalkingDistance(c.x, c.y, enemyC.x, enemyC.y) - 7)
                        return false;


            Character enemy = closestEnemy(currentTeam, c, hexas);

            if (c.charClass == CharClass.ARCHER || c.charClass == CharClass.MAGE)
                if (enemy.getClassData().basicAttack.range > AIUtil.hexaGrid.getWalkingDistance(c.x, c.y, enemy.x, enemy.y) - 4)
                    return false;

                else if ((enemy.getClassData().basicAttack.range > AIUtil.hexaGrid.getWalkingDistance(c.x, c.y, enemy.x, enemy.y) - 4) && enemy.charClass != CharClass.ARCHER && enemy.charClass != CharClass.MAGE && enemy.charClass != CharClass.SOIGNEUR)
                    return false;

            return true;
        }

        /// <summary>
        /// Return true if someone on currentTeam is already fleeing
        /// </summary>
        /// <param name="toSort">The list to sort</param>
        /// <param name="charsInOrder">The sorting order</param>
        /// <returns>The sorted list</returns>
        public static bool alreadyAtRisk(int currentTeam, HexaGrid hexas, Character c)
        {
            List<Character> allyTeam = getCharacterTeamList(currentTeam, hexas);

            foreach (Character ally in allyTeam)
                if (isInDanger(ally, hexas) && ally != c)
                    return true;
            return false;
        }

        

    }
}