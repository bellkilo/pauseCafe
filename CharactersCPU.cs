using System;
using System.Collections.Generic;
using UnityEngine;
using Misc;
using Characters;
using Hexas;
using AI_Util;
using AI_Class;
using static UtilCPU.UtilCPU;
using static MainGame;
using static ScriptsCPU.ScriptsCPU;

namespace CharactersCPU
{
    public abstract class CharacterCPU
    {
        // List of charCPUs created
        public static List<CharacterCPU> charCPUList;
        // Variable where the character and its relevant info is stored
        protected RelevantCharacterInfoWrapper infoWrapper;
        // The game board
        protected HexaGrid hexas;

        public CharacterCPU(Character character, HexaGrid hexas)
        {
            infoWrapper = new RelevantCharacterInfoWrapper(character);

            this.hexas = hexas;

            charCPUList.Add(this);
        }

        public Character Character { get => infoWrapper.Character; }

        public int HP { get => infoWrapper.HP; set => infoWrapper.HP = value; }
        public int TotalDamage { get => infoWrapper.TotalDamage; set => infoWrapper.TotalDamage = value; }
        public int TotalDamage2 { get => infoWrapper.TotalDamage2; set => infoWrapper.TotalDamage2 = value; }
        public int X { get => infoWrapper.X; set => infoWrapper.X = value; }
        public int Y { get => infoWrapper.Y; set => infoWrapper.Y = value; }

        public bool SkillAvailable { get => infoWrapper.SkillAvailable; set => infoWrapper.SkillAvailable = value; }
        public bool Skill2Available { get => infoWrapper.Skill2Available; set => infoWrapper.Skill2Available = value; }
        public bool Dmgbuff { get => infoWrapper.Dmgbuff; set => infoWrapper.Dmgbuff = value; }

        public CharsDB.CharacterDB ClassData { get => infoWrapper.Character.getClassData(); }

        public Character TempChar { get => new Character(Character.charClass, X, Y, Character.team, true); }

        /// <summary>
        /// This character will move up to <paramref name="target"/> and attack them once they are at range. 
        /// Uses skills or not depending on the parameter <paramref name="allowedToUseSkill"/>.
        /// </summary>
        /// <remarks>
        /// For coming up on other characters without attacking them, use <see cref="moveTowards(CharacterCPU)"/>.
        /// </remarks>
        /// <param name="target">The target character</param>
        /// <param name="allowedToUseSkill">If true, this character will use skills whenever they can, 
        /// and if false, they will not use any skills</param>
        /// <returns>A (character to use, action to do) tuple</returns>
        /// Edited by Rayane TALEB L3C2
        public virtual (Character, ActionAIPos) target(CharacterCPU target, bool allowedToUseSkill)
        {
            if (allowedToUseSkill && Skill2Available && target.HP > ClassData.basicAttack.effectValue + (Dmgbuff ? 1 : 0))
            // Skill2 use allowed, available & the target can't go down with just a basic attack
            {
                if (isInRangeToUseSkill2(this, target))
                {
                    /*
                    SkillAvailable = false;
                    TotalDamage -= 10;
                    
                    target.HP -= Character.getSkillDamage() + (Dmgbuff ? 1 : 0);
                    TotalDamage += Character.getSkillDamage() + (Dmgbuff ? 1 : 0);

                    return (Character, new ActionAIPos(ActionType.SKILL1, new Point(target.X, target.Y)));
                    //*/
                    return (Character, new ActionAIPos(ActionType.SKILL2, planForAttack(target, ActionType.SKILL2)));
                }

            }
            else if (allowedToUseSkill && SkillAvailable && target.HP > ClassData.basicAttack.effectValue + (Dmgbuff ? 1 : 0))
            // Skill use allowed, available & the target can't go down with just a basic attack
            {
                if (isInRangeToUseSkill(this, target))
                {
                    /*
                    SkillAvailable = false;
                    TotalDamage -= 10;
                    
                    target.HP -= Character.getSkillDamage() + (Dmgbuff ? 1 : 0);
                    TotalDamage += Character.getSkillDamage() + (Dmgbuff ? 1 : 0);

                    return (Character, new ActionAIPos(ActionType.SKILL1, new Point(target.X, target.Y)));
                    //*/
                    return (Character, new ActionAIPos(ActionType.SKILL1, planForAttack(target, ActionType.SKILL1)));
                }

            }
            else
            if (isInRangeToAttack(this, target))
            {
                /*
                target.HP -= Character.getDamage() + (Dmgbuff ? 1 : 0);
                TotalDamage += Character.getDamage() + (Dmgbuff ? 1 : 0);

                return (Character, new ActionAIPos(ActionType.ATK1, new Point(target.X, target.Y)));
                //*/
                return (Character, new ActionAIPos(ActionType.ATK1, planForAttack(target, ActionType.ATK1)));
            }

            return moveTowards(target);
        }

        /// <summary>
        /// Returns a point indicating where to attack to hit the highest amount of enemy units.
        /// </summary>
        /// <param name="target">The target</param>
        /// <param name="action">The attack (basic attack or skill)</param>
        /// <returns>A position</returns>
        /// Edited by Rayane TALEB L3C2
        public Point planForAttack(CharacterCPU target, ActionType action)
        {
            CharsDB.Attack attack = null;

            // Basic attack or skill
            switch (action)
            {
                case ActionType.ATK1:
                    attack = this.Character.getClassData().basicAttack;
                    break;
                case ActionType.SKILL1:
                    SkillAvailable = false;
                    TotalDamage -= 10;

                    attack = this.Character.getClassData().skill_1;
                    break;
                case ActionType.SKILL2:
                    Skill2Available = false;
                    TotalDamage2 -= 14;

                    attack = this.Character.getClassData().skill_2;
                    break;
                default:
                    Debug.Log("Default case called in planForAOE");

                    return new Point(target.X, target.Y);
            }

            // Has AOE
            if (attack.rangeAoE > 0)
            {
                Hexa toAttack;
                toAttack = takeAOEIntoAccount(this.TempChar, target.TempChar, attack, action);
                if (toAttack == null)
                {
                    Debug.Log("PlanForAttack: no enemy targets in range");
                    //return null;
                }
                Point toReturn = new Point(toAttack.x, toAttack.y);

                for (int i = 0; i < charCPUList.Count; i++)
                    if (hexas.getDistance(toReturn.x, toReturn.y, charCPUList[i].X, charCPUList[i].Y) <= attack.rangeAoE)
                        switch (attack.attackEffect)
                        {
                            case CharsDB.AttackEffect.DAMAGE:
                                target.HP -= attack.effectValue + (Dmgbuff ? 1 : 0);
                                TotalDamage += attack.effectValue + (Dmgbuff ? 1 : 0);
                                TotalDamage2 += attack.effectValue + (Dmgbuff ? 1 : 0);
                                break;
                            case CharsDB.AttackEffect.HEAL:
                                target.HP += Math.Min(attack.effectValue, target.ClassData.maxHP);
                                TotalDamage += attack.effectValue + (Dmgbuff ? 1 : 0);
                                TotalDamage2 += attack.effectValue + (Dmgbuff ? 1 : 0);
                                break;
                            case CharsDB.AttackEffect.DMG_BUFF:
                                target.Dmgbuff = true;
                                break;
                            default:
                                Debug.Log("Default case called in effectValue calculations");
                                break;
                        }

                if (TotalDamage >= 10)
                {
                    TotalDamage -= 10;
                    SkillAvailable = true;
                }
                if (TotalDamage2 >= 14)
                {
                    TotalDamage2 -= 14;
                    Skill2Available = true;
                }

                return toReturn;
            }
            // No AOE
            else
            {
                Debug.Log("PlanForAttack: noAOE");
                switch (attack.attackEffect)
                {
                    case CharsDB.AttackEffect.DAMAGE:
                        target.HP -= attack.effectValue + (Dmgbuff ? 1 : 0);
                        TotalDamage += attack.effectValue + (Dmgbuff ? 1 : 0);
                        TotalDamage2 += attack.effectValue + (Dmgbuff ? 1 : 0);
                        break;
                    case CharsDB.AttackEffect.HEAL:
                        target.HP += Math.Min(attack.effectValue, target.ClassData.maxHP);
                        TotalDamage += attack.effectValue + (Dmgbuff ? 1 : 0);
                        TotalDamage2 += attack.effectValue + (Dmgbuff ? 1 : 0);
                        break;
                    case CharsDB.AttackEffect.DMG_BUFF:
                        target.Dmgbuff = true;
                        break;
                    default:
                        Debug.Log("Default case called in effectValue calculations");
                        break;
                }

                if (TotalDamage >= 10)
                {
                    TotalDamage -= 10;
                    SkillAvailable = true;
                }
                if (TotalDamage2 >= 14)
                {
                    TotalDamage2 -= 14;
                    Skill2Available = true;
                }
                return new Point(target.X, target.Y);
            }
        }

        /// <summary>
        /// This character will move up to <paramref name="target"/> and attack them once they are at range.
        /// </summary>
        /// <remarks>
        /// For coming up on other characters without attacking them, use <see cref="moveTowards(CharacterCPU)"/>.
        /// </remarks>
        /// <param name="target">The target character</param>
        /// <returns>A (character to use, action to do) tuple</returns>
        public virtual (Character, ActionAIPos) target(CharacterCPU target)
        {
            return this.target(target, true);
        }

        /// <summary>
        /// The character defined in this class will move towards the point passed in the parameter.
        /// </summary>
        /// <remarks>
        /// If the hexa space located at point is freed up, the character will just go to it.
        /// Otherwise, they will attempt to position themselves at a nearby hexa.
        /// </remarks>
        /// <param name="point">The location this unit will move towards</param>
        /// <returns>A (character to use, action to do) tuple</returns>
        public (Character, ActionAIPos) moveTowards(Point point)
        {
            // Indicated point is the character's position
            if (X == point.x && Y == point.y)
                return (Character, new ActionAIPos(ActionType.SKIP, null));

            List<ActionAIPos> actions = new List<ActionAIPos>();
            Hexa pointHexa = hexas.getHexa(point.x, point.y);
            bool willBeOccupied = false;

            // Check to see if another CharacterCPU is planning to move on that point
            foreach (CharacterCPU cpu in charCPUList)
                willBeOccupied = willBeOccupied || (cpu.X == point.x && cpu.Y == point.y);

            // Can just walk on the target hexa
            if (pointHexa != null && hexas.canWalk(pointHexa) && pointHexa.charOn == null && !willBeOccupied)
                actions.AddRange(findSequencePathToHexaCPU(TempChar, point.x, point.y));
            // Target hexa is occupied or a wall
            else
            {
                List<Point> targetArea = pointsFromClosestToFarthest(point, this);
                int i = targetArea.Count - 1; // Going through targetArea in reverse (inner rings first)

                while (actions.Count == 0 && i >= 0)
                {
                    Point p = targetArea[i];
                    Hexa goTo = hexas.getHexa(p.x, p.y);
                    willBeOccupied = false;

                    foreach (CharacterCPU cpu in charCPUList)
                        willBeOccupied = willBeOccupied || (cpu.X == p.x && cpu.Y == p.y);

                    if (goTo != null && hexas.canWalk(goTo) && goTo.charOn == null && !willBeOccupied)
                        actions.AddRange(findSequencePathToHexaCPU(TempChar, goTo.x, goTo.y));

                    i--;
                }
            }

            if (actions.Count == 0)
                return (Character, new ActionAIPos(ActionType.SKIP, null));

            X = actions[0].pos.x;
            Y = actions[0].pos.y;
            return (Character, actions[0]);
        }
        //Author VALAT Thibault L3Q1


        /// <summary>
        /// The character defined in this class will move towards the character passed in the parameter.
        /// </summary>
        /// <remarks>
        /// This method will attempt to position the character in such a way that
        /// the target's distance to them is equal to their attack range.
        /// </remarks>
        /// <param name="target">The character this unit will move towards</param>
        /// <returns>A (character to use, action to do) tuple</returns>
        public virtual (Character, ActionAIPos) moveTowards(CharacterCPU target)
        {
            List<ActionAIPos> actions = new List<ActionAIPos>();
            List<Point> targetArea = pointsFromClosestToFarthest(target, this,
                (Skill2Available) ?
                (Character.getSkill2Range()) :
                ((SkillAvailable) ?
                (Character.getSkillRange()) :
                (Character.getRange())));
            bool willBeOccupied;

            int j = 0;

            while (actions.Count == 0 && j < targetArea.Count)
            {
                Point p = targetArea[j];
                Hexa goTo = hexas.getHexa(p.x, p.y);
                willBeOccupied = false;

                // Check to see if another CharacterCPU is planning to move on that point
                foreach (CharacterCPU cpu in charCPUList)
                    willBeOccupied = willBeOccupied || (cpu.X == p.x && cpu.Y == p.y);

                //Debug.Log("To : " + p.x + " " + p.y + ", " + goTo.type + ", " + charToString(goTo.charOn));

                if (goTo != null && hexas.canWalk(goTo) && goTo.charOn == null && !willBeOccupied)
                    actions.AddRange(findSequencePathToHexaCPU(TempChar, goTo.x, goTo.y));

                j++;
            }

            if (actions.Count == 0)
                return (Character, new ActionAIPos(ActionType.SKIP, null));

            X = actions[0].pos.x;
            Y = actions[0].pos.y;
            return (Character, actions[0]);
        }

        /// <summary>
        /// The character defined in this class will move towards the character passed in the parameter.
        /// </summary>
        /// <remarks>
        /// This method will attempt to position the character in such a way that
        /// the target's distance to them is equal to their attack range.
        /// </remarks>
        /// <param name="target">The character this unit will move towards</param>
        /// <returns>A (character to use, action to do) tuple</returns>
        public virtual (Character, ActionAIPos) moveTowards(Character target)
        {
            List<ActionAIPos> actions = new List<ActionAIPos>();
            List<Point> targetArea = pointsFromClosestToFarthest(target, this.TempChar,
                (Character.isSkill2Up()) ?
                (Character.getSkill2Range()) :
                ((Character.isSkillUp()) ?
                (Character.getSkillRange()) :
                (Character.getRange())));

            int j = 0;

            while (actions.Count == 0 && j < targetArea.Count)
            {
                Point p = targetArea[j];
                Hexa goTo = hexas.getHexa(p.x, p.y);

                //Debug.Log("To : " + p.x + " " + p.y + ", " + goTo.type + ", " + charToString(goTo.charOn));

                if (goTo != null && hexas.canWalk(goTo) && goTo.charOn == null)
                    actions.AddRange(findSequencePathToHexaCPU(Character, goTo.x, goTo.y));

                j++;
            }

            if (actions.Count == 0)
                return (Character, new ActionAIPos(ActionType.SKIP, null));
            return (Character, actions[0]);
        }

        /// <summary>
        /// Targets a character given as a parameter.
        /// </summary>
        /// <param name="target">The target character</param>
        /// <returns>A (character to use, action to do) tuple</returns>
        /// Edited by Rayane TALEB L3C2
        public virtual (Character, ActionAIPos) target(Character target)
        {
            if (this.Skill2Available && target.HP > this.ClassData.basicAttack.effectValue)
            // Skill2 possible et cible avec plus de PV que les dégâts d'une attaque simple
            {  // AIUtil.isCharWithinRangeSkill(hexas.charList.IndexOf(character), hexas.charList.IndexOf(target))
                if (isInRangeToUseSkill2(Character, target))
                {
                    this.Skill2Available = false;
                    return (Character, new ActionAIPos(ActionType.SKILL2, new Point(target.x, target.y)));
                }
            }

            if (this.SkillAvailable && target.HP > this.ClassData.basicAttack.effectValue)
            // Skill possible et cible avec plus de PV que les dégâts d'une attaque simple
            {  // AIUtil.isCharWithinRangeSkill(hexas.charList.IndexOf(character), hexas.charList.IndexOf(movetoward))
                if (isInRangeToUseSkill(Character, target)){
                    this.SkillAvailable = false;
                    return (Character, new ActionAIPos(ActionType.SKILL1, new Point(target.x, target.y)));
                }
            }
            // AIUtil.isCharWithinRangeAttack(hexas.charList.IndexOf(character), hexas.charList.IndexOf(target))
            if (isInRangeToAttack(Character, target))
                return (Character, new ActionAIPos(ActionType.ATK1, new Point(target.x, target.y)));

            return moveTowards(target);
        }

        /// <summary>
        /// Returns a turn skip.
        /// </summary>
        /// <returns>A turn skip</returns>
        public (Character, ActionAIPos) wait()
        {
            return (Character, new ActionAIPos(ActionType.SKIP, null));
        }
    }

    public class WarriorCPU : CharacterCPU
    {
        public WarriorCPU(Character character, HexaGrid hexas)
            : base(character, hexas) { }
    }

    public class ThiefCPU : CharacterCPU
    {
        public ThiefCPU(Character character, HexaGrid hexas)
            : base(character, hexas) { }
    }

    public class MageCPU : CharacterCPU
    {
        public MageCPU(Character character, HexaGrid hexas)
            : base(character, hexas) { }
    }

    public class ValkyrieCPU : CharacterCPU
    {
        public ValkyrieCPU(Character character, HexaGrid hexas)
            : base(character, hexas) { }
    }

    /// Edited by Rayane TALEB L3C2
    public class DruideCPU : CharacterCPU
    {
        public DruideCPU(Character character, HexaGrid hexas)
            : base(character, hexas) { }
    }

    public class ArcherCPU : CharacterCPU
    {
        public ArcherCPU(Character character, HexaGrid hexas)
            : base(character, hexas) { }

        /// <summary>
        /// The character defined in this class will move towards the character passed in the parameter.
        /// </summary>
        /// <remarks>
        /// This method will attempt to position the character in such a way that
        /// the target's distance to them is equal to their attack range.
        /// </remarks>
        /// <param name="target">The character this unit will move towards</param>
        /// <returns>A (character to use, action to do) tuple</returns>
        public override (Character, ActionAIPos) moveTowards(CharacterCPU target)
        {
            List<ActionAIPos> actions = new List<ActionAIPos>();
            List<Point> targetArea = pointsFromClosestToFarthest(target, this,
                (Skill2Available) ?
                (Character.getSkill2Range()) :
                ((SkillAvailable) ?
                (Character.getSkillRange()) :
                (Character.getRange())));
            bool willBeOccupied;

            int j = 0;

            while (actions.Count == 0 && j < targetArea.Count)
            {
                Point p = targetArea[j];
                Hexa goTo = hexas.getHexa(p.x, p.y);
                willBeOccupied = false;

                // Check to see if another CharacterCPU is planning to move on that point
                foreach (CharacterCPU cpu in charCPUList)
                    willBeOccupied = willBeOccupied || (cpu.X == p.x && cpu.Y == p.y);

                //Debug.Log("To : " + p.x + " " + p.y + ", " + goTo.type + ", " + charToString(goTo.charOn));

                if (goTo != null && hexas.canWalk(goTo) && goTo.charOn == null && !willBeOccupied)
                    actions.AddRange(findSequencePathToHexaCPU(TempChar, goTo.x, goTo.y));

                j++;
            }

            if (actions.Count == 0)
                return (Character, new ActionAIPos(ActionType.SKIP, null));

            X = actions[0].pos.x;
            Y = actions[0].pos.y;
            return (Character, actions[0]);
        }
    }

    public class HealerCPU : CharacterCPU
    {
        public HealerCPU(Character character, HexaGrid hexas)
            : base(character, hexas) { }

        /*/// <summary>
        /// This character will move up to <paramref name="target"/> and heal them once they are at range. 
        /// Uses skills or not depending on the parameter <paramref name="allowedToUseSkill"/>.
        /// </summary>
        /// <remarks>
        /// For coming up on other characters without attacking them, use <see cref="moveTowards(CharacterCPU)"/>.
        /// </remarks>
        /// <param name="target">The target character</param>
        /// <param name="allowedToUseSkill">If true, this character will use skills whenever they can, 
        /// and if false, they will not use any skills</param>
        /// <returns>A (character to use, action to do) tuple</returns>
        public override (Character, ActionAIPos) target(CharacterCPU target, bool allowedToUseSkill)
        {
            if (allowedToUseSkill && SkillAvailable && target.HP > ClassData.basicAttack.effectValue + (Dmgbuff ? 1 : 0))
            // Skill use allowed, available & the target can't go down with just a basic attack
            {
                if (isInRangeToUseSkill(this, target))
                {
                    SkillAvailable = false;
                    TotalDamage -= 10;

                    target.HP += Character.getSkillDamage();
                    TotalDamage += Character.getSkillDamage();

                    return (Character, new ActionAIPos(ActionType.SKILL1, new Point(target.X, target.Y)));
                }

            }
            else
            if (isInRangeToAttack(this, target))
            {
                target.HP += Character.getDamage();
                TotalDamage += Character.getDamage();

                return (Character, new ActionAIPos(ActionType.ATK1, new Point(target.X, target.Y)));
            }

            return moveTowards(target);
        }*/

        /// <summary>
        /// Finds which team member to heal.
        /// </summary>
        /// <param name="teamList">The list of all the team members</param>
        /// <returns>A (character to use, action to do) tuple</returns>
        public (Character, ActionAIPos) findHealingPriority(List<CharacterCPU> teamList)
        {
            CharacterCPU toHeal = null;
            int toHealScore = 0;

            foreach (CharacterCPU healerTarget in teamList)
                if (healerTarget.Character != this.Character)
                {
                    if (toHeal == null)
                    {
                        toHeal = healerTarget;
                        toHealScore = toHeal.Character.getClassData().maxHP - toHeal.HP;
                    }
                    else
                    {
                        int healerTargetScore = healerTarget.Character.getClassData().maxHP - healerTarget.HP;

                        if (healerTargetScore > toHealScore && isInTotalRange(this, toHeal))
                        {
                            toHeal = healerTarget;
                            toHealScore = healerTargetScore;
                        }
                    }
                }

            // toHeal has health to heal
            if (toHealScore > 0)
                return this.target(toHeal);
            else return this.wait();
        }
    }


    public class SorcererCPU : CharacterCPU
    {
        public SorcererCPU(Character character, HexaGrid hexas)
            : base(character, hexas) { }

        /*/// <summary>
        /// This character will move up to <paramref name="target"/> and heal them once they are at range. 
        /// Uses skills or not depending on the parameter <paramref name="allowedToUseSkill"/>.
        /// </summary>
        /// <remarks>
        /// For coming up on other characters without attacking them, use <see cref="moveTowards(CharacterCPU)"/>.
        /// </remarks>
        /// <param name="target">The target character</param>
        /// <param name="allowedToUseSkill">If true, this character will use skills whenever they can, 
        /// and if false, they will not use any skills</param>
        /// <returns>A (character to use, action to do) tuple</returns>
        public override (Character, ActionAIPos) target(CharacterCPU target, bool allowedToUseSkill)
        {
            if (allowedToUseSkill && SkillAvailable && target.HP > ClassData.basicAttack.effectValue + (Dmgbuff ? 1 : 0))
            // Skill use allowed, available & the target can't go down with just a basic attack
            {
                if (isInRangeToUseSkill(this, target))
                {
                    SkillAvailable = false;
                    TotalDamage -= 10;

                    return (Character, new ActionAIPos(ActionType.SKILL1, new Point(target.X, target.Y)));
                }

            }
            else
            if (isInRangeToAttack(this, target))
            {
                target.Dmgbuff = true;

                return (Character, new ActionAIPos(ActionType.ATK1, new Point(target.X, target.Y)));
            }

            return moveTowards(target);
        }*/

        /// <summary>
        /// Finds which team member to buff. Waits if there's no need for it.
        /// </summary>
        /// <param name="teamList">The list of all the team members</param>
        /// <returns>A (character to use, action to do) tuple</returns>
        public (Character, ActionAIPos) findBuffingPriority(List<CharacterCPU> teamList, List<(Character, ActionAIPos)> actions)
        {
            List<CharacterCPU> sorcererFocusOrder = order(remaining(teamList, actions),
                                    CharClass.ARCHER, CharClass.VOLEUR, CharClass.MAGE, CharClass.GUERRIER);

            foreach (CharacterCPU toBuff in sorcererFocusOrder)
                if (toBuff != this && !toBuff.Dmgbuff)
                    return this.target(toBuff, false);

            return this.wait();
        }
    }


    ///Edited by Rayane TALEB L3C2
    public class RelevantCharacterInfoWrapper
    {
        private Character character;
        private int hp,
            totalDamage,
            totalDamage2,
            x,
            y;
        private bool skillAvailable, skill2Available,
            dmgbuff;

        public RelevantCharacterInfoWrapper(Character character)
        {
            this.character = character;

            this.HP = character.HP;
            this.TotalDamage = character.totalDamage;
            this.TotalDamage2 = character.totalDamage2;
            this.X = character.x;
            this.Y = character.y;

            this.SkillAvailable = character.skillAvailable;
            this.Skill2Available = character.skill2Available;
            this.Dmgbuff = character.dmgbuff;
        }

        public Character Character { get => character; }

        public int HP { get => hp; set => hp = value; }
        public int TotalDamage { get => totalDamage; set => totalDamage = value; }
        public int TotalDamage2 { get => totalDamage2; set => totalDamage2 = value; }
        public int X { get => x; set => x = value; }
        public int Y { get => y; set => y = value; }

        public bool SkillAvailable { get => skillAvailable; set => skillAvailable = value; }
        public bool Skill2Available { get => skill2Available; set => skill2Available = value; }

        public bool Dmgbuff { get => dmgbuff; set => dmgbuff = value; }
    }
}


