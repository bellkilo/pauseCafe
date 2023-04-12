using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Hexas;


// ##################################################################################################################################################
// Characters
// Author : ?
// Edited by L3C2, TALEB Rayane
// Commented by L3C2, TALEB Rayane
// ##################################################################################################################################################

namespace Characters
{


    public enum CharClass : byte { GUERRIER, VOLEUR, ARCHER, MAGE, SOIGNEUR, ENVOUTEUR, VALKYRIE, DRUIDE };

    //Author : ?
    //Edited by L3C2, TALEB Rayane
    public class CharsDB
    {

        public enum AttackEffect : byte { DAMAGE, HEAL, PA_BUFF, PM_BUFF, DMG_BUFF, STUN, POISON, ROOT }; // edited by Rayane TALEB L3C2

        // Attack 
        public class Attack
        {
            public int range;
            public int rangeAoE;
            public bool targetsEnemies;
            public bool targetsAllies;
            public bool targetsSelf;
            public AttackEffect attackEffect;
            public int effectValue;

            //Initialize an attack
            public Attack(int range, int rangeAoE, bool targetsEnemies, bool targetsAllies, bool targetsSelf, AttackEffect attackEffect, int effectValue)
            {
                this.range = range;
                this.rangeAoE = rangeAoE;
                this.targetsEnemies = targetsEnemies;
                this.targetsAllies = targetsAllies;
                this.targetsSelf = targetsSelf;
                this.attackEffect = attackEffect;
                this.effectValue = effectValue;
            }
        }
        // Char base stats per class and attacks
        public class CharacterDB
        {
            public int maxHP;
            public int basePA;
            public int basePM;
            public int basePriority;
            public Attack basicAttack;
            public Attack skill_1;
            public Attack skill_2; // edited by Rayane TALEB L3C2

            // base stats list
            public CharacterDB(int maxHP, int basePA, int basePM, int priority, Attack basicAttack, Attack skill_1, Attack skill_2) // edited by Rayane TALEB L3C2
            {
                this.maxHP = maxHP;
                this.basePA = basePA;
                this.basePM = basePM;
                this.basePriority = priority;
                this.basicAttack = basicAttack;
                this.skill_1 = skill_1;
                this.skill_2 = skill_2;
            }
        }
        public static List<CharacterDB> list;

        //Initialize the characters for the game
        //edited by L3C2 Rayane TALEB
        //Author : ?
        public static void initCharsDB()
        {
            list = new List<CharacterDB>();
            list.Add(new CharacterDB(17, 3, 3, 7, new Attack(1, 0, true, false, false, AttackEffect.DAMAGE, 3), new Attack(1, 0, true, false, false, AttackEffect.DAMAGE, 5), new Attack(1, 0, true, false, false, AttackEffect.DAMAGE, 3))); // GUERRIER 
            list.Add(new CharacterDB(11, 3, 3, 4, new Attack(1, 0, true, false, false, AttackEffect.DAMAGE, 1), new Attack(2, 0, true, false, false, AttackEffect.DAMAGE, 3), new Attack(2, 0, true, false, false, AttackEffect.DAMAGE, 2))); // VOLEUR 
            list.Add(new CharacterDB(13, 2, 3, 5, new Attack(7, 0, true, false, false, AttackEffect.DAMAGE, 2), new Attack(10, 0, true, false, false, AttackEffect.DAMAGE, 4), new Attack(12, 1, true, false, false, AttackEffect.DAMAGE, 1))); // ARCHER 
            list.Add(new CharacterDB(10, 2, 3, 5, new Attack(3, 1, true, false, false, AttackEffect.DAMAGE, 2), new Attack(5, 0, true, false, false, AttackEffect.DAMAGE, 3), new Attack(6, 0, true, false, false, AttackEffect.POISON, 1))); // MAGE 
            list.Add(new CharacterDB(13, 2, 3, 6, new Attack(4, 0, false, true, false, AttackEffect.HEAL, 3), new Attack(5, 1, false, true, false, AttackEffect.HEAL, 3), new Attack(3, 0, false, true, false, AttackEffect.HEAL, 9))); // SOIGNEUR 
            list.Add(new CharacterDB(11, 2, 3, 6, new Attack(4, 0, false, true, false, AttackEffect.DMG_BUFF, 1), new Attack(4, 2, false, true, false, AttackEffect.PA_BUFF, 1), new Attack(4, 2, false, true, false, AttackEffect.PM_BUFF, 1))); // ENVOUTEUR 
            list.Add(new CharacterDB(14, 2, 4, 4, new Attack(1, 1, true, false, false, AttackEffect.DAMAGE, 2), new Attack(4, 1, true, false, false, AttackEffect.DAMAGE, 3), new Attack(4, 0, true, false, false, AttackEffect.DAMAGE, 4))); // VALKYRIE 
            list.Add(new CharacterDB(11, 2, 3, 4, new Attack(2, 0, true, false, false, AttackEffect.DAMAGE, 1), new Attack(3, 0, true, false, false, AttackEffect.STUN, 1), new Attack(3, 0, true, false, false, AttackEffect.ROOT, 1))); // DRUIDE 
        }
    }

    //Class character, to instatiate characters
    //Author : ?
    //// Edited by L3C2, TALEB Rayane
    public class Character : System.IEquatable<Character>, System.IComparable<Character>
    {
        public static GameObject characterTemplate;
        public static List<GameObject> characterTemplateModels;
        public static GameObject charactersFolder;
        public static Color TEAM_1_COLOR = new Color(0.125f, 0.125f, 1);
        public static Color TEAM_2_COLOR = new Color(1, 0.125f, 0);
        public int totalDamage;
        public int totalDamage2; //edited by L3C2 Rayane TALEB
        public CharClass charClass;
        public int team;
        public int HPmax;
        public int HP;
        public int characterPA;
        public int characterPM;
        public int PA;
        public int PM;
        public int priority;
        public int basePriority;
        public int x;
        public int y;
        public int cptPoison; //edited by L3C2 Rayane TALEB
        public bool isStun;
        public bool isBuffPM;
        public bool isPoisoned;
        public bool isRooted;
        public bool isBuffPA;
        public bool skillAvailable;
        public bool skill2Available;
        public bool dmgbuff;
        public HexaDirection directionFacing;
        public GameObject go;

        //Constructor
        //Author : ?
        // Edited by L3C2, TALEB Rayane
        public Character(CharClass charClass, int x, int y, int team)
        {
            this.charClass = charClass;
            CharsDB.CharacterDB myCharClass = CharsDB.list[(int)charClass];
            HPmax = myCharClass.maxHP; HP = HPmax;
            PA = myCharClass.basePA;
            characterPA = myCharClass.basePA;
            PM = myCharClass.basePM;
            characterPM = myCharClass.basePM;
            priority = myCharClass.basePriority;
            basePriority = myCharClass.basePriority;
            totalDamage = 0;
            this.x = x;
            this.y = y;
            // edited by Rayane TALEB L3C2
            this.isBuffPM = false;
            this.isBuffPA = false;
            this.isPoisoned = false;
            this.isStun = false;
            this.isRooted = false;
            this.team = team;
            this.skillAvailable = true;
            this.skill2Available = true;
            this.dmgbuff = false;
            this.go = GameObject.Instantiate(characterTemplate, charactersFolder.transform);
            this.go.SetActive(true);
            this.go.transform.position = Hexa.hexaPosToReal(x, y, 0);
            this.go.GetComponent<CharacterGO>().character = this;
            this.setColorByTeam();
            this.setDirection(HexaDirection.DOWN);
        }

        // No GameObject (console mode)
        public Character(CharClass charClass, int x, int y, int team, bool a)
        {
            this.charClass = charClass;
            CharsDB.CharacterDB myCharClass = CharsDB.list[(int)charClass];
            HPmax = myCharClass.maxHP; HP = HPmax;
            PA = myCharClass.basePA;

            PM = myCharClass.basePM;
            priority = myCharClass.basePriority;
            basePriority = myCharClass.basePriority;

            this.x = x;
            this.y = y;
            this.isPoisoned = false;
            this.isStun = false; // edited by Rayane TALEB L3C2
            this.isBuffPM = false;
            this.isRooted = false;
            this.isBuffPA = false;
            this.team = team;
            this.skillAvailable = true;
            this.skill2Available = true;
            this.go = null;
        }

        //Override of the equals method
        // /!\ Needed to sort characters 
        //Author : VALAT Thibault
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            Character objAsCharacter = obj as Character;
            if (objAsCharacter == null) return false;
            else return Equals(objAsCharacter);
        }

        //Decide how to sort characters : according to the priority
        // /!\ Needed to sort characters
        //Author : VALAT Thibault
        public int CompareTo(Character compareChar)
        {
            if (compareChar == null)
                return 1;
            else
                return this.priority.CompareTo(compareChar.priority);
        }

        //Return the attribut to sort the characters
        // /!\ Needed to sort characters
        //Author : VALAT Thibault
        public override int GetHashCode()
        {
            return priority;
        }

        //To compare two characters
        // /!\ Needed to sort characters
        //Author : VALAT Thibault
        public bool Equals(Character other)
        {
            if (other == null) return false;
            return (this.priority.Equals(other.priority));
        }

        //Reset the priority to its basic value
        //Author : VALAT Thibault
        public void resetPriority()
        {
            if (basePriority == 4)
                this.priority = (basePriority + UnityEngine.Random.Range(0, 2));
            else if (basePriority >= 6)
                this.priority = (basePriority - UnityEngine.Random.Range(0, 2));
            else
                this.priority = basePriority;
        }

        //Update the position of the character on thr grid
        //Author : ?
        public void updatePos(int newX, int newY, HexaGrid hexaGrid)
        {
            hexaGrid.getHexa(x, y).charOn = null;
            x = newX;
            y = newY;
            hexaGrid.getHexa(x, y).charOn = this;
            this.go.transform.position = Hexa.hexaPosToReal(x, y, 0);
        }

        // Console mode
        public void updatePos2(int newX, int newY, HexaGrid hexaGrid)
        {
            hexaGrid.getHexa(x, y).charOn = null;
            x = newX;
            y = newY;
            hexaGrid.getHexa(x, y).charOn = this;
        }

        //Clear the position of the character on thr grid
        //Author : ?
        public void clearPos(HexaGrid hexaGrid)
        {
            hexaGrid.getHexa(x, y).charOn = null;
        }

        //x coordinate getter
        //Author : ?
        public int getX()
        {
            return x;
        }

        //y coordinate getter
        //Author : ?
        public int getY()
        {
            return y;
        }

        //total damage getter
        //Author : ?
        public int getTotalDamage()
        {
            return totalDamage;
        }

        //total damage2 getter
        //Author : Rayane TALEB L3C2
        public int getTotalDamage2()
        {
            return totalDamage2;
        }

        //total damage setter
        //Author : ?
        public void setTotalDamage(int totalDamage)
        {
            this.totalDamage = totalDamage;
        }

        //total damage setter
        //Author : Rayane TALEB L3C2
        public void setTotalDamage2(int totalDamage2)
        {
            this.totalDamage2 = totalDamage2;
        }

        //Pa getter
        //Author : ?
        public int getPA()
        {
            return PA;
        }

        //Pa setter
        //Author : ?
        public void setPA(int PA)
        {
            this.PA = PA;
        }

        //isBuffPA getter
        //Author : Rayane TALEB L3C2

        public bool getIsBuffPA()
        {
            return isBuffPA;
        }

        //isBuffPA getter
        //Author : Rayane TALEB L3C2

        public bool getIsBuffPM()
        {
            return isBuffPM;
        }
        //isPoisoned getter
        //Author : Rayane TALEB L3C2
        public bool getIsPoisoned()
        {
            return isPoisoned;
        }

        //isStun getter
        //Author : Rayane TALEB L3C2

        public bool getIsStun()
        {
            return isStun;
        }

        //isRooted getter
        //Author : Rayane TALEB L3C2

        public bool getIsRooted()
        {
            return isRooted;
        }

        //Use the poison effect
        //Author : Rayane TALEB L3C2
        public void usePoison()
        {
            if (cptPoison == 3) // 3 turn max
            {
                isPoisoned = false;
                cptPoison = 0;
            }
            else
            {
                HP -= 1;
                cptPoison++;
            }
        }

        //Poison method effect
        //Author : Rayane TALEB L3C2

        public void poisonMethod()
        {
            if (getIsPoisoned())
            {
                usePoison();
            }
        }

        //Stun method effect
        //Author : Rayane TALEB L3C2

        public void rootMethod()
        {
            if (getIsRooted())
            {
                isRooted = false;
                PM = 0;
            }
            else
            {       
                PM = CharsDB.list[(int)charClass].basePM;
            }
        }

        //BuffPM method effect
        //Author : Rayane TALEB L3C2

        public void buffPMMethod()
        {
            if (getIsBuffPM())
            {
                isBuffPM = false;
                PM = CharsDB.list[(int)charClass].basePM + CharsDB.list[5].skill_2.effectValue;
            }
            else
            {
                PM = CharsDB.list[(int)charClass].basePM;
            }
        }

        //BuffPA method effect
        //Author : Rayane TALEB L3C2

        public void buffPAMethod()
        {
            if (getIsBuffPA())
            {
                PA = CharsDB.list[(int)charClass].basePA + CharsDB.list[5].skill_1.effectValue;
                isBuffPA = false;
            }
            else
            {
                PA = CharsDB.list[(int)charClass].basePA;
            }
        }

        //Method effect
        //Author : Rayane TALEB L3C2

        public void isEffectMethod()
        {
            poisonMethod();
            if (!getIsRooted())
            {
                buffPMMethod();
            }
            else
            {
                rootMethod();
            }
            if (!getIsStun())
            {
                buffPAMethod();
            }
        }

        //PM getter
        //Author : Rayane TALEB L3C2

        public int getPM()
        {
            return PM;
        }

        //PM setter
        //Author : Rayane TALEB L3C2

        public void setPM(int PM)
        {
            this.PM = PM;
        }

        //Hp getter
        //Author : ?
        public int getHP()
        {
            return HP;
        }

        //Pa setter
        //Author : ?
        public void setHP(int HP)
        {
            this.HP = HP;
        }

        //Char class getter
        //Author : ?
        public CharClass getCharClass()
        {
            return charClass;
        }

        //DamageBuff getter
        //Author : ?
        public bool getDamageBuff()
        {
            return dmgbuff;
        }

        //DamageBuff setter
        //Author : ?
        public void setDamageBuff(bool dmgbuff)
        {
            this.dmgbuff = dmgbuff;
        }

        //Damage getter
        //Author : ?
        public int getDamage()
        {
            if (dmgbuff == true) return CharsDB.list[(int)charClass].basicAttack.effectValue + 1;
            else return CharsDB.list[(int)charClass].basicAttack.effectValue;
        }

        //Range getter
        //Author : ?
        public int getRange()
        {
            return this.getClassData().basicAttack.range;
        }

        //Skill damage getter
        //Author : ?
        public int getSkillDamage()
        {
            if (dmgbuff == true) return CharsDB.list[(int)charClass].skill_1.effectValue + 1;
            else return CharsDB.list[(int)charClass].skill_1.effectValue;
        }

        //Skill2 damage getter
        //Author : L3C2, Rayane TALEB
        public int getSkill2Damage()
        {
            if (dmgbuff == true) return CharsDB.list[(int)charClass].skill_2.effectValue + 1;
            else return CharsDB.list[(int)charClass].skill_2.effectValue;
        }


        //Skill range getter
        //Author : ?
        public int getSkillRange()
        {
            return this.getClassData().skill_1.range;
        }

        //Skill2 range getter
        //Author : L3C2, Rayane TALEB
        public int getSkill2Range()
        {
            return this.getClassData().skill_2.range;
        }

        //Returns if the skill is ready or not
        //Author : ?
        public bool isSkillUp()
        {
            return skillAvailable;
        }

        // Author : L3C2, Rayane TALEB
        public bool isSkill2Up()
        {
            return skill2Available;
        }

        //Set the color of the characters depending on the team
        //Author : ?
        public void setColorByTeam()
        {

            switch (team)
            {
                case 0: this.go.transform.GetChild(0).GetComponent<Renderer>().material.color = TEAM_1_COLOR; break;
                case 1: this.go.transform.GetChild(0).GetComponent<Renderer>().material.color = TEAM_2_COLOR; break;
                default: break;
            }
            GameObject.Instantiate(characterTemplateModels[(int)this.charClass], go.transform);
        }

        //Set the direction of the character
        //Author : ?
        public void setDirection(HexaDirection newDirection)
        {
            this.directionFacing = newDirection;
            Transform charModel = this.go.transform.GetChild(1);
            if (charModel) charModel.eulerAngles = new Vector3(0, (int)newDirection * 60, 0);
        }

        //Returns the name of the class of the character
        //Author : ?
        // Edited by L3C2, TALEB Rayane
        public string getName()
        {
            switch (this.charClass)
            {
                case CharClass.GUERRIER: return "Guerrier";
                case CharClass.VOLEUR: return "Voleur";
                case CharClass.ARCHER: return "Archer";
                case CharClass.MAGE: return "Mage";
                case CharClass.SOIGNEUR: return "Soigneur";
                case CharClass.ENVOUTEUR: return "Envouteur";
                case CharClass.VALKYRIE: return "Valkyrie";
                case CharClass.DRUIDE: return "Druide";
                default: return "None";
            }
        }

        //Returns the data of the class of the character
        //Author : ?
        public CharsDB.CharacterDB getClassData()
        {
            return CharsDB.list[(int)charClass];
        }
    }

}