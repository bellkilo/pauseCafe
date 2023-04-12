using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Characters;
using CharactersCPU;
using static UtilCPU.UtilCPU;
using static MainGame;
using AI_Class;
using AI_Util;
using Hexas;
using Misc;

namespace ScriptsCPU
{
    public class ScriptsCPU
    {
        /// <summary>
        /// Makes all the characters wait on their turn.
        /// </summary>
        /// <param name="currentTeam">The current team in play</param>
        /// <param name="hexas">The game board</param>
        /// <returns>A list of ActionAIPos turn skips</returns>
        public static List<(Character, ActionAIPos)> prone(int currentTeam, HexaGrid hexas)
        {
            Debug.Log("Script called : PRONE");

            List<CharacterCPU> teamList = getTeamList(currentTeam, hexas);
            List<(Character, ActionAIPos)> actions = new List<(Character, ActionAIPos)>();

            foreach (CharacterCPU c in teamList)
                actions.Add(c.wait());

            return actions;
        }

        /// <summary>
        /// Makes the CPU team gang up on one character.
        /// </summary>
        /// <param name="currentChar">The character currently in control</param>
        /// <param name="charList">The list of all the characters on the board</param>
        /// <returns>A (Character, ActionAIPos) list describing the actions to take</returns>
        public static List<(Character, ActionAIPos)> offense(int currentTeam, HexaGrid hexas, List<CharacterCPU> enemyList)
        {
            Debug.Log("Script called : OFFENSE");

            List<CharacterCPU> teamList = order(getTeamList(currentTeam, hexas),
                CharClass.SOIGNEUR, CharClass.ENVOUTEUR);

            List<(Character chr, ActionAIPos act)> actions = new List<(Character, ActionAIPos)>();
            CharacterCPU victim = null;

            foreach (CharacterCPU c in teamList)
                for (int countPA = 0; countPA < c.Character.PA; countPA++)
                {
                    // When all enemies are defeated
                    if (enemyList.Count < 1)
                        actions.Add(c.wait());

                    else
                    {
                        if (victim == null)
                            victim = enemyList[0];

                        switch (c.Character.charClass)
                        {
                            case CharClass.SOIGNEUR:
                                // Heals the ally with the least HP
                                HealerCPU h = (HealerCPU)c;
                                actions.Add(h.findHealingPriority(teamList));
                                break;
                            case CharClass.ENVOUTEUR:
                                // Targets allies in this order : archers > thieves > mages > warriors
                                SorcererCPU s = (SorcererCPU)c;
                                actions.Add(s.findBuffingPriority(teamList, actions));
                                break;
                            default:
                                // Plainly targets the current appointed unit to focus on
                                actions.Add(c.target(victim));

                                if (victim.HP <= 0)
                                {
                                    enemyList.Remove(victim);
                                    victim = null;
                                }

                                break;
                        }
                    }

                    // If the current character skipped this turn, close this for loop 
                    if (actions[actions.Count - 1].act.action == ActionType.SKIP)
                        countPA = c.Character.PA;
                }

            return actions;
        }

        /// <summary>
        /// Gets the unit with the least HP to move towards the healer, then gets them healed.
        /// </summary>
        /// <param name="currentTeam">The team currently in control</param>
        /// <param name="hexas">The game board</param>
        /// <returns>A (Character, ActionAIPos) list describing the actions to take</returns>
        public static List<(Character, ActionAIPos)> patchUpUnit(int currentTeam, HexaGrid hexas, List<CharacterCPU> enemyList)
        {
            Debug.Log("Script called : PATCHUPUNIT");
            List<CharacterCPU> teamList = getHealingFocusedList(currentTeam, hexas);
            List<(Character chr, ActionAIPos act)> actions = new List<(Character, ActionAIPos)>();
            CharacterCPU victim = null;
            foreach (CharacterCPU c in teamList)
                for (int countPA = 0; countPA < c.Character.PA; countPA++)
                {
                    // When all enemies are defeated
                    if (enemyList.Count < 1)
                        actions.Add(c.wait());
                    else
                    {
                        if (victim == null)
                            victim = enemyList[0];
                        // Team member to focus healing on
                        if (teamList.IndexOf(c) == 0 && gotHealerInTeam(currentTeam))
                        {
                            // Can this unit get an attack in before needing to walk to the healer?
                            if (hexas.getWalkingDistance(c.X, c.Y, teamList[1].X, teamList[1].Y) <
                                (c.Character.PA - countPA - 1) * c.Character.getClassData().basePM
                                + teamList[1].Character.getClassData().basicAttack.range &&
                                (c.SkillAvailable ? isInRangeToUseSkill(c, victim) : isInRangeToAttack(c, victim)))
                            {
                                actions.Add(c.target(victim));
                                if (victim.HP <= 0)
                                {
                                    enemyList.Remove(victim);
                                    victim = null;
                                }
                            }
                            else
                                actions.Add(c.moveTowards(teamList[1]));
                        }
                        else switch (c.Character.charClass)
                            {
                                case CharClass.SOIGNEUR:
                                    // Heals the ally in the first slot of teamList
                                    actions.Add(c.target(teamList[0]));
                                    break;
                                case CharClass.ENVOUTEUR:
                                    // Targets allies in this order : archers > thieves > mages > warriors
                                    SorcererCPU s = (SorcererCPU)c;
                                    actions.Add(s.findBuffingPriority(teamList, actions));
                                    break;
                                default:
                                    // Plainly targets the current appointed unit to focus on
                                    actions.Add(c.target(victim));
                                    if (victim.HP <= 0)
                                    {
                                        enemyList.Remove(victim);
                                        victim = null;
                                    }
                                    break;
                            }
                    }
                    // If the current character skipped this turn, close this for loop 
                    if (actions[actions.Count - 1].act.action == ActionType.SKIP)
                        countPA = c.Character.PA;
                }
            return actions;
        }

        /// <summary>
        /// The team called moves as one towards the enemy.
        /// </summary>
        /// <param name="currentTeam">The team currently in control</param>
        /// <param name="hexas">The game board</param>
        /// <returns>A (Character, ActionAIPos) list describing the actions to take</returns>
        public static List<(Character, ActionAIPos)> formation(int currentTeam, HexaGrid hexas)
        {
            Debug.Log("Script called : FORMATION");

            List<CharacterCPU> teamList = order(getTeamList(currentTeam, hexas),
                CharClass.GUERRIER, CharClass.VALKYRIE, CharClass.VOLEUR, CharClass.MAGE, CharClass.DRUIDE, CharClass.ARCHER);
            List<(Character chr, ActionAIPos act)> actions = new List<(Character, ActionAIPos)>();

            CharacterCPU target = getTeamList(currentTeam == 0 ? 1 : 0, hexas)[0];
            CharacterCPU leader = teamList[0];

            foreach (CharacterCPU c in teamList)
                for (int countPA = 0; countPA < c.Character.PA; countPA++)
                {
                    // Leader call
                    if (c == leader)
                    {
                        // Limiting leader movement to 2 PA so leading thieves don't lose the other members
                        if (countPA >= 2)
                            actions.Add(c.wait());
                        else
                            actions.Add(c.moveTowards(target));
                    }
                    // Follower call
                    else
                        actions.Add(c.moveTowards(getTacticalFormation(target, leader, c.Character.getCharClass(), hexas)));

                    // If the current character skipped this turn, close this for loop 
                    if (actions[actions.Count - 1].act.action == ActionType.SKIP)
                        countPA = c.Character.PA;
                }


            return actions;
        }


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////
        //*//*//*//*//*//*//*//*//*//*//*//*//*//*//*//*//*//*//*//*//*//*//*//*//*//*//*//*//*//*//*//*//*//*//*//
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////


        /// <summary>
        /// Heal the closest ally wich health point aren't full
        /// </summary>
        /// <param name="currentChar">The character currently in control</param>
        /// <param name="charList">The list of all the characters on the board</param>
        /// <returns></returns>
        /// edited by TALEB Rayane, group: L3C2
        public static List<(Character, ActionAIPos)> heal(CharacterCPU healer, int currentTeam, HexaGrid hexas)
        {
            List<CharacterCPU> teamList = getTeamList(currentTeam, hexas);
            List<(Character, ActionAIPos)> actions = new List<(Character, ActionAIPos)>();
            CharacterCPU targetAlly;

            //Debug.Log("HEALER PLAYING");

            Character allyInPrio = identifyAllyClassPriority(currentTeam, hexas);

            if (allyInNeed(currentTeam, hexas))
            {
                // If an ally is in need
                if (healer.Character.skill2Available && canAOEHeal(currentTeam, hexas, healer.Character))
                {
                    // If spell is ready, and find a good way to use it
                    Point p = bestPointForAOEHeal(currentTeam, hexas, healer.Character);
                    ActionAIPos ciblage = new ActionAIPos(ActionType.SKILL2, new Point(p.x, p.y));
                    actions.Add((healer.Character, ciblage));
                }
                if (healer.Character.skillAvailable && canAOEHeal(currentTeam, hexas, healer.Character))
                {
                    // If spell is ready, and find a good way to use it
                    Point p = bestPointForAOEHeal(currentTeam, hexas, healer.Character);
                    ActionAIPos ciblage = new ActionAIPos(ActionType.SKILL1, new Point(p.x, p.y));
                    actions.Add((healer.Character, ciblage));
                }

                // Choosing target to heal/follow
                targetAlly = choosingAlly(allyRankedByMissingHP(currentTeam, hexas), currentTeam, hexas, healer.Character);
                Debug.Log("heal =>" + targetAlly.Character.charClass);
                actions.Add(healer.target(targetAlly));
            }

            else if (allTeamFullLife(currentTeam, hexas) && !atLeastOnInRange(currentTeam, hexas))
            {
                // If no one is in need, and no one in range, get closer to an ally
                actions.Add(healer.moveTowards(allyInPrio));
                return actions;
            }
            else if (!allyInNeed(currentTeam, hexas) && moveTowardsWithoutRisk(currentTeam, hexas, allyInPrio) && AIUtil.hexaGrid.getWalkingDistance(healer.Character.x, healer.Character.y, allyInPrio.x, allyInPrio.y) > 2)
            {
                actions.Add(healer.moveTowards(allyInPrio));
                return actions;
            }
            else actions.Add(healer.wait());

            return actions;
        }


        /// <summary>
        /// Boost the closest ally 
        /// </summary>
        /// <param name="currentChar">The character currently in control</param>
        /// <param name="charList">The list of all the characters on the board</param>
        /// <returns></returns>
        /// edited by TALEB Rayane, group: L3C2
        public static List<(Character, ActionAIPos)> envouteur(CharacterCPU envouteur, int currentTeam, HexaGrid hexas)
        {
            List<CharacterCPU> teamList = getTeamList(currentTeam, hexas);
            List<(Character, ActionAIPos)> actions = new List<(Character, ActionAIPos)>();
            //Character toBoost = null;


            if (envouteur.Character.skill2Available && atLeastOnInRange(currentTeam, hexas))
            {
                // If the spell is ready, find the best way to use it
                Debug.Log("PM BOOST ");
                Point p = bestPointForAOEBoost(currentTeam, hexas, envouteur.Character);
                ActionAIPos ciblage = new ActionAIPos(ActionType.SKILL2, new Point(p.x, p.y));
                actions.Add((envouteur.Character, ciblage));
                return actions;
            }

            if (envouteur.Character.skillAvailable && atLeastOnInRange(currentTeam, hexas))
            {
                // If the spell is ready, find the best way to use it
                Debug.Log("PA BOOST ");
                Point p = bestPointForAOEBoost(currentTeam, hexas, envouteur.Character);
                ActionAIPos ciblage = new ActionAIPos(ActionType.SKILL1, new Point(p.x, p.y));
                actions.Add((envouteur.Character, ciblage));
                return actions;
            }

            if (atLeastOnInRange(currentTeam, hexas))
            {
                // If an ally is in range, boost the best ally possible
                Debug.Log("BUG 1  ");
                actions.Add(envouteur.target(allyToBoost(currentTeam, hexas)));
                return actions;
            }
            else if (!atLeastOnInRange(currentTeam, hexas))
            {
                // If no ally is in range, get closer to an on
                Debug.Log("BUG 2 ");
                actions.Add(envouteur.moveTowards(identifyAllyClassPriority(currentTeam, hexas)));
                return actions;
            }
            return actions;
        }

        /// <summary>
        /// The currentChar will flee the enemy, and play around his healer
        /// </summary>
        /// <param name="currentChar">The character currently in control</param>
        /// <param name="charList">The list of all the characters on the board</param>
        /// <returns></returns>
        public static List<(Character, ActionAIPos)> fleeWithHealer(CharacterCPU currentChar, HexaGrid hexas)
        {
            // If the currentChar is a healer AND someone is in need, the healer won't flee
            if (currentChar.Character.charClass == CharClass.SOIGNEUR && allyInNeed(currentChar.Character.team, hexas))
                return heal(currentChar, currentChar.Character.team, hexas);

            // If the currentChar is a healer AND no one is in need, the healer will flee
            else if (currentChar.Character.charClass == CharClass.SOIGNEUR && !allyInNeed(currentChar.Character.team, hexas))
                return flee(currentChar, hexas);

            // If an ally is already fleing, the currentChar won't flee
            if (alreadyAtRisk(currentChar.Character.team, hexas, currentChar.Character))
                return attack(currentChar.Character.team, hexas);

            List<Character> teamList = getCharacterTeamList(currentChar.Character.team, hexas);
            List<ActionAIPos> mouvement = new List<ActionAIPos>();
            List<(Character, ActionAIPos)> actions = new List<(Character, ActionAIPos)>();
            Hexa goTo;
            Point healerPos = null;

            // Find healer position
            foreach (Character c in teamList)
                if (c.charClass == CharClass.SOIGNEUR)
                    healerPos = new Point(c.x, c.y);

            // For every point around the healer, we check if this point, is not on enemy range
            foreach (Point p in pointsAroundHexa(healerPos, 3))
                if (!isPointInEnemyTeamRangeAttack(p, currentChar.Character.team))
                {
                    goTo = hexas.getHexa(p.x, p.y);
                    if (goTo != null && (goTo.type == HexaType.GROUND || goTo.type == HexaType.BONUS) && goTo.charOn == null)
                    {
                        mouvement.AddRange(AIUtil.findSequencePathToHexa(currentChar.Character, goTo.x, goTo.y));
                        actions.Add((currentChar.Character, mouvement[0]));
                        return actions;
                    }
                }

            // The currentChar couldn't find a good way to flee, he will attack
            Debug.Log("Can't flee");
            return attack(currentChar.Character.team, hexas);
        }

        /// <summary>
        /// The currentChar will flee the enemy, 
        /// </summary>
        /// <param name="currentChar">The character currently in control</param>
        /// <param name="charList">The list of all the characters on the board</param>
        /// <returns></returns>
        public static List<(Character, ActionAIPos)> flee(CharacterCPU currentChar, HexaGrid hexas)
        {

            if (currentChar.Character.charClass == CharClass.SOIGNEUR)
                return heal(currentChar, currentChar.Character.team, hexas);

            List<ActionAIPos> mouvement = new List<ActionAIPos>();
            List<(Character, ActionAIPos)> actions = new List<(Character, ActionAIPos)>();
            Hexa goTo;

            // For every point around the currentChar, we check if this point, is not on enemy range
            foreach (Point p in hexas.findAllPaths(currentChar.Character.x, currentChar.Character.y, 3))
                if (!isPointInEnemyTeamRangeAttack(p, currentChar.Character.team))
                {
                    Debug.Log("hehe ici on flee 1");
                    Debug.Log("hehe ici on flee 1 qui bug");
                    goTo = hexas.getHexa(p.x, p.y);
                    if (goTo != null && (goTo.type == HexaType.GROUND || goTo.type == HexaType.BONUS) && goTo.charOn == null)
                    {
                        mouvement.AddRange(AIUtil.findSequencePathToHexa(currentChar.Character, goTo.x, goTo.y));
                        actions.Add((currentChar.Character, mouvement[0]));
                        Debug.Log("hehe ici on flee 2");
                        Debug.Log("hehe ici on flee 2 qui bug");
                        Debug.Log(actionListPrinter(actions));
                        Debug.Log("PA : " + currentChar.Character.PA);
                        return actions;
                    }
                }

            // The currentChar couldn't find a good way to flee, he will attack
            Debug.Log("Can't flee");
            return attack(currentChar.Character.team, hexas);
        }



        //edited by GOUVEIA Klaus, group: L3Q1
        //Author : ?
        /// <summary>
        /// Makes the CPU team gang up on one character.
        /// </summary>
        /// <param name="currentChar">The character currently in control</param>
        /// <param name="charList">The list of all the characters on the board</param>
        /// <returns></returns>
        public static List<(Character, ActionAIPos)> attack(int currentTeam, HexaGrid hexas)
        {
            List<CharacterCPU> teamList = getTeamList(currentTeam, hexas);
            List<(Character, ActionAIPos)> actions = new List<(Character, ActionAIPos)>();
            Character victim = null;

            foreach (CharacterCPU c in teamList)
                for (int countPA = 0; countPA < c.Character.PA; countPA++)
                {
                    //victim = enemyInMostAllyRange(currentTeam, c.Character, hexas);
                    victim = easierTargetToKill(currentTeam, c.Character, hexas);

                    // If the character canno't attack this turn, he won't put himself at risk
                    if (c.Character.getPA() == 1 && getEnemyTargetsAvailable(c.Character).Count == 0 && !moveWithoutRisk(currentTeam, hexas, c.Character))
                    {
                        actions.Add(c.wait());
                    }
                    else
                        actions.Add(c.target(victim));

                }


            return actions;
        }

        /// currentChar will focus the enemyFocused if he can
        /// <returns></returns>
        /// author : Rayane TALEB L3C2

        public static Character focusCharacter(CharacterCPU currentChar, CharClass enemyFocused)
        {
            List<Character> enemysList = getEnemyTargetsAvailable(currentChar.Character);
            Character victim = null;

            foreach (Character c in enemysList)
            {
                if (c.getCharClass() == enemyFocused)
                {
                    if (isInTotalRange(currentChar.Character, c))
                    {
                    victim = c;
                    }
                }
            }
            return victim;
        }

        /// <summary>
        /// Guerrier decision
        /// </summary>
        /// <param name="currentChar">The character currently in control</param>
        /// <param name="charList">The list of all the characters on the board</param>
        /// <returns></returns>
        /// edited by Rayane TALEB L3C2
        public static List<(Character, ActionAIPos)> guerrier(CharacterCPU guerrier, int currentTeam, HexaGrid hexas)
        {
            List<(Character, ActionAIPos)> actions = new List<(Character, ActionAIPos)>();
            List<CharacterCPU> teamList = getTeamList(currentTeam, hexas);
            List<Character> enemysList = getEnemyTargetsAvailable(guerrier.Character);
            Character victim = null;

            victim = null;

            //edited by Rayane TALEB L3C2, if druide is on range, guerrier will focus him
            for (int countPA = 0; countPA < guerrier.Character.PA; countPA++)
                {
                    Debug.Log("ATRTAQQUE" + countPA);
                    victim = focusCharacter(guerrier, CharClass.DRUIDE);
                    if (victim != null)
                    {
                        Debug.Log("guerrier Target Druide");
                        actions.Add(guerrier.target(victim));
                    }
                    //victim = enemyInMostAllyRange(currentTeam, c.Character, hexas);
                    else
                    {
                        victim = easierTargetToKill(currentTeam, guerrier.Character, hexas);

                        // If the character canno't attack this turn, he won't put himself at risk
                        if (guerrier.Character.getPA() == 1 && getEnemyTargetsAvailable(guerrier.Character).Count == 0 && !moveWithoutRisk(currentTeam, hexas, guerrier.Character))
                            actions.Add(guerrier.wait());
                        else
                            actions.Add(guerrier.target(victim));
                    }

                }
            return actions;
        }

        /// <summary>
        /// Mage decision
        /// </summary>
        /// <param name="currentChar">The character currently in control</param>
        /// <param name="charList">The list of all the characters on the board</param>
        /// <returns></returns>
        /// edited by TALEB Rayane, group: L3C2
        public static List<(Character, ActionAIPos)> mage(CharacterCPU mage, int currentTeam, HexaGrid hexas)
        {
            List<(Character, ActionAIPos)> actions = new List<(Character, ActionAIPos)>();
            List<CharacterCPU> teamList = getTeamList(currentTeam, hexas);
            Character victim = null;

            Debug.Log("MAGE PLAYING");
            // Rayane TALEB L3C2, If the mage spell is ready, he check if can find  agood way to use it, if yes he'll use it
            if (mage.Character.skill2Available && getEnemyTargetsAvailable(mage.Character).Count != 0)
            {
                Debug.Log("mage Attack2");
                //Mage will focus the guerrier if he can
                victim = focusCharacter(mage, CharClass.GUERRIER);
                if (victim == null)
                {
                    victim = easierTargetToKill(currentTeam, mage.Character, hexas);
                }
                if (isInRangeToUseSkill2(mage.Character, victim))
                {
                    actions.Add(mage.target(victim));
                    return actions;
                }

            }
            
            if (mage.Character.skillAvailable && canAOEDamage(currentTeam, hexas, mage.Character.x, mage.Character.y, 5, 2) /*&& !betterAOEDamage(currentTeam, hexas, mage.Character.x, mage.Character.y, 3)*/)
            {

                //Debug.Log("AOE DAMAGE ");
                Point p = bestPointForAOEDamage(currentTeam, hexas, mage.Character.x, mage.Character.y, 5, 2);
                ActionAIPos ciblage = new ActionAIPos(ActionType.SKILL1, new Point(p.x, p.y));
                actions.Add((mage.Character, ciblage));
                return actions;
            }

            for (int countPA = 0; countPA < mage.Character.PA; countPA++)
            {
                    //victim = enemyInMostAllyRange(currentTeam, c.Character, hexas);
                    victim = easierTargetToKill(currentTeam, mage.Character, hexas);

                    // If the character canno't attack this turn, he won't put himself at risk
                    if (mage.Character.getPA() == 1 && getEnemyTargetsAvailable(mage.Character).Count == 0 && !moveWithoutRisk(currentTeam, hexas, mage.Character))
                        actions.Add(mage.wait());
                    else
                        actions.Add(mage.target(victim));

            }
            return actions;
        }



        //Edited by Rayane TALEB L3C2
        //Author : ?
        /// <summary>
        /// Rogue decision
        /// </summary>
        /// <param name="currentChar">The character currently in control</param>
        /// <param name="charList">The list of all the characters on the board</param>
        /// <returns></returns>
        /// edited by Rayane TALEB L3C2
        public static List<(Character, ActionAIPos)> voleur(CharacterCPU voleur, int currentTeam, HexaGrid hexas, Hexa caseBonus)
        {
            List<CharacterCPU> teamList = getTeamList(currentTeam, hexas);
            List<(Character, ActionAIPos)> actions = new List<(Character, ActionAIPos)>();
            Character victim = null;
            List<Character> enemysList = getEnemyTargetsAvailable(voleur.Character);
            Debug.Log("Enemy Tragets Available :" + enemysList.Count);
            List<Character> currentCharacter = new List<Character>();
            currentCharacter.Add(voleur.Character);

            Point centreBonus = new Point(caseBonus.x, caseBonus.y);
            Debug.Log("PA du voleur" + enemysList.Count);
            Debug.Log("JE RENTRE ICI 1");
            for (int countPA = 0; countPA < voleur.Character.PA; countPA++)
            {
                victim = focusCharacter(voleur, CharClass.SOIGNEUR); // edited by Rayane TALEB L3C2
                if(victim != null) {
                    Debug.Log("Voleur Target Soigneur");
                    actions.Add(voleur.target(victim));
                }


                else if (enemysList.Count != 0 && countPA == 0 && moveWithoutRisk(currentTeam, hexas, voleur.Character) && actions.Count == 0)
                {
                    Debug.Log("Voleur PA = 3");
                    Debug.Log("JE RENTRE ICI 3");
                    if (canGroupKill(currentCharacter, enemysList[0]))
                    {
                        Debug.Log("JE TUE 3 FOIS");
                        Debug.Log("Voleur Kill Ennemy");
                        actions.Add(voleur.target(enemysList[0]));
                        actions.Add(voleur.target(enemysList[0]));
                        actions.Add(voleur.target(enemysList[0]));
                        return actions;
                    }
                    else if (isInTotalRange(voleur.Character, enemysList[0]))
                    {
                        Debug.Log("JE TUE 2 FOIS ET JE FUIS");
                        Debug.Log("Voleur Hit and Flee 1");
                        actions.Add(voleur.target(enemysList[0]));
                        actions.Add(voleur.target(enemysList[0]));
                        actions.Add(flee(voleur, hexas)[0]);
                        actions.Add(voleur.wait());
                        return actions;
                    }
                    else
                    {
                        Debug.Log("JE TUE 1 FOIS ET JE FUIS 2 FOIS");
                        Debug.Log("Voleur Hit and Flee 2");
                        actions.Add(voleur.target(enemysList[0]));
                        actions.Add(flee(voleur, hexas)[0]);
                        actions.Add(flee(voleur, hexas)[0]);
                        actions.Add(voleur.wait());
                        return actions;
                    }
                }

                else if (enemysList.Count != 0 && countPA == 1 && moveWithoutRisk(currentTeam, hexas, voleur.Character) && actions.Count == 1)
                {
                    Debug.Log("Voleur PA = 2");
                    if (canGroupKill(currentCharacter, enemysList[0]))
                    {
                        Debug.Log("Voleur Kill Ennemy");
                        actions.Add(voleur.target(enemysList[0]));
                        actions.Add(voleur.target(enemysList[0]));
                        return actions;
                    }
                    else
                    {
                        Debug.Log("Voleur Hit and Flee3");
                        actions.Add(voleur.target(enemysList[0]));
                        actions.Add(flee(voleur, hexas)[0]);
                        actions.Add(voleur.wait());
                        return actions;
                    }
                }

                else if(getBonusTeam() != currentTeam && hexas.getHexa(voleur.X, voleur.Y).type == HexaType.BONUS){
                    Debug.Log("Voleur Attack Zone");
                    Debug.Log("JE RENTRE ICI 4");
                    victim = closestEnemy(currentTeam, voleur.Character, hexas);
                    actions.Add(voleur.target(victim)); 
                }

                else if(getBonusTeam() != currentTeam){
                    Debug.Log("Voleur Move Towards Zone");
                    Debug.Log("JE RENTRE ICI 5");
                    Point bonus = new Point(caseBonus.x, caseBonus.y);
                    actions.Add(voleur.moveTowards(bonus)); 
                }
                

                else {
                    //if (notToFarAway(currentTeam, hexas, voleur.Character) && moveWithoutRisk(currentTeam, hexas, voleur.Character)){
                    victim = easierTargetToKill(currentTeam, voleur.Character, hexas);
                    
                    if (getEnemyTargetsAvailable(voleur.Character).Count == 0 && !moveWithoutRisk(currentTeam, hexas, voleur.Character)){
                        actions.Add(voleur.wait());
                        Debug.Log("Voleur Wait");
                        Debug.Log("JE RENTRE ICI 6");
                    }
                    else {
                        Debug.Log("Voleur Target Enemy");
                        Debug.Log("JE RENTRE ICI 7");
                        actions.Add(voleur.target(victim));
                    }
                }
                //if (notToFarAway(currentTeam, hexas, voleur.Character) && moveWithoutRisk(currentTeam, hexas, voleur.Character))
                //{
                //    //victim = enemyInMostAllyRange(currentTeam, voleur.Character, hexas);
                //    victim = easierTargetToKill(currentTeam, voleur.Character, hexas);
                //    actions.Add(voleur.target(victim));
                //}
                //else
                //    actions.Add(voleur.wait());
                //***
            }
            Debug.Log("actions count" + actions.Count);
            Debug.Log("JE RENTRE ICI 8");
            return actions;
        }


        //edited by GOUVEIA Klaus, group: L3Q1
        //Author : ?
        /// <summary>
        /// Archer behaviour
        /// </summary>
        /// <param name="currentChar">The character currently in control</param>
        /// <param name="charList">The list of all the characters on the board</param>
        /// <returns></returns>
        public static List<(Character, ActionAIPos)> archer(CharacterCPU archer, int currentTeam, HexaGrid hexas)
        {
            List<CharacterCPU> teamList = getTeamList(currentTeam, hexas);
            List<(Character, ActionAIPos)> actions = new List<(Character, ActionAIPos)>();
            Character victim = null;
            List<Character> enemysList = getEnemyTargetsAvailable(archer.Character);
            Debug.Log("Enemy Tragets Available :" + enemysList.Count);

            for (int countPA = 0; countPA < archer.Character.PA; countPA++)
            {

                victim = easierTargetToKill(currentTeam, archer.Character, hexas);
                actions.Add(archer.target(victim));
                Debug.Log("Archer Attack2");
                
            }

            Debug.Log("Archer Actions => " + actions.Count);
            return actions;
        }


        //edited by TALEB Rayane, group: L3C2
        //Author : Klaus GOUVEIA
        public static List<(Character, ActionAIPos)> valkyrie(CharacterCPU valkyrie, int currentTeam, HexaGrid hexas, Hexa caseBonus)
        {
            List<CharacterCPU> teamList = getTeamList(currentTeam, hexas);
            List<(Character, ActionAIPos)> actions = new List<(Character, ActionAIPos)>();
            Point bonus = new Point(caseBonus.x, caseBonus.y);
            Character victim = null;
            List<Character> currentCharacter = new List<Character>();
            currentCharacter.Add(valkyrie.Character);

            List<Character> enemysList = getEnemyTargetsAvailable(valkyrie.Character);
            Debug.Log("Enemy Tragets Available :" + enemysList.Count);

            if (valkyrie.Character.skill2Available && getEnemyTargetsAvailable(valkyrie.Character).Count != 0)
            {

                //Debug.Log("AOE DAMAGE ");
                Debug.Log("valkyrie Attack2");
                victim = easierTargetToKill(currentTeam, valkyrie.Character, hexas);
                if (isInRangeToUseSkill2(valkyrie.Character, victim))
                {
                    actions.Add(valkyrie.target(victim));
                    return actions;
                }
            }

            if (valkyrie.Character.skillAvailable && canAOEDamage(currentTeam, hexas, valkyrie.Character.x, valkyrie.Character.y, 5, 2))
            {

                //Debug.Log("AOE DAMAGE ");
                Point p = bestPointForAOEDamage(currentTeam, hexas, valkyrie.Character.x, valkyrie.Character.y, 2, 1);
                ActionAIPos ciblage = new ActionAIPos(ActionType.SKILL1, new Point(p.x, p.y));
                actions.Add((valkyrie.Character, ciblage));
                return actions;
            }

            for (int countPA = 0; countPA < valkyrie.Character.PA; countPA++)
                {
                    //victim = enemyInMostAllyRange(currentTeam, c.Character, hexas);
                    victim = easierTargetToKill(currentTeam, valkyrie.Character, hexas);

                    // If the character canno't attack this turn, he won't put himself at risk
                    if (valkyrie.Character.getPA() == 1 && getEnemyTargetsAvailable(valkyrie.Character).Count == 0 && !moveWithoutRisk(currentTeam, hexas, valkyrie.Character))
                        actions.Add(valkyrie.moveTowards(bonus));
                    else
                        actions.Add(valkyrie.target(victim));

                }
            return actions;

        }

        // edited by Rayane TALEB L3C2
        public static List<(Character, ActionAIPos)> druide(CharacterCPU druide, int currentTeam, HexaGrid hexas)
        {
            List<CharacterCPU> teamList = getTeamList(currentTeam, hexas);
            List<(Character, ActionAIPos)> actions = new List<(Character, ActionAIPos)>();
            Character victim = null;

            for (int countPA = 0; countPA < druide.Character.PA; countPA++)
            {
                // edited by Rayane TALEB L3C2, druide will focus guerrier if he can
                victim = focusCharacter(druide, CharClass.GUERRIER);
                if (victim == null)
                {
                    //victim = enemyInMostAllyRange(currentTeam, archer.Character, hexas);
                    victim = easierTargetToKill(currentTeam, druide.Character, hexas);
                }

                if (druide.Character.getPA() == 1 && getEnemyTargetsAvailable(druide.Character).Count == 0 && !moveWithoutRisk(currentTeam, hexas, druide.Character))
                    actions.Add(druide.wait());
                else
                    actions.Add(druide.target(victim));

                //Debug.Log("target possible => " + getEnemyTargetsAvailable(druide.Character).Count);
                
            }


            return actions;
        }

    }
}

