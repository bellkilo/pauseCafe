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
using static UtilCPU.UtilCPU;
using AI_Class;
using Stats;
using Mono.Data.Sqlite;
using System.Data;
using System;
using static MainMenu;

// ##################################################################################################################################################
// MAIN
// Author : ?
// Edited by L3C2, Rayane TALEB
// Commented by L3C2, Rayane TALEB
// ##################################################################################################################################################

public class MainGame : MonoBehaviour
{
    // Init in character selection menu
    public static StartGameData startGameData;

    // Declaraion of variables
    public Mesh hexaFilledMesh;
    public Mesh hexaHollowMesh;
    public Mesh hexaWallMesh;
    public GameObject ruinsMap;
    public GameObject arrow;
    public GameObject hexaHighlight;
    public GameObject hexasFolder;
    public GameObject hexaTemplate;
    public GameObject charactersFolder;
    public GameObject characterTemplate;
    public List<GameObject> characterTemplateModels;
    public GameObject particleExplosion;
    public GameObject particleHeal;
    public GameObject damageValueDisplay;
    public GameObject camera_;
    public Transform cameraPos;
    public Vector3 cameraPosGoal;
    public Boolean cameraMoved;
    public GameObject UICurrentChar;
    public GameObject UIEnemyChar;
    public GameObject Initiative;
    public GameObject BonusCard;
    public List<Texture> charSquares;
    public GameObject UICharTurns;
    public GameObject UICharTurnTemplate;
    public GameObject UIAction;
    public GameObject UIPauseMenu;
    public GameObject UIVictoryScreen;
    public List<Texture> charCards;
    public int tileW;
    public int tileH;
    public bool lockedCamera;
    public Toggle toggleLockedCamera;
    public bool debugMode;
    public bool pauseMenu;

    public int frame;
    public bool updateUI;
    public bool updateMouseHover;
    public HexaGrid hexaGrid;
    public Vector3 mousePosOld;
    public Vector3 mousePos;
    public Hexa hexaHoveredOld;
    public Hexa hexaHovered;
    public static Character currentCharControlled;
    public int currentCharControlledID;

    public List<GameObject> UICharTurnsList;
    public List<GameObject> pathFinderDisplay;
    public List<GameObject> lineOfSightDisplay;
    public List<GameObject> dangerDisplay;
    public List<Point> pathWalk;
    public Character charHovered;
    public int attackUsed;
    public Point attackUsedTargetHexa;
    public CharsDB.Attack attackUsedAttack;
    public CharsDB.Attack skillHelp;
    public int pathWalkpos;
    public int newTurn;
    public int AIwaitingTime;
    public int winner;
    public static int bonusTeam;
    public StatsGame statsGame;

    public Color pathDisplayColor;
    public Color allPathsDisplayColor;
    public Color lineOfSightColor;
    public Color blockedSightColor;
    public Color AoEColor;
    public Color neutralBonusDisplayColor;
    public Color redBonusDisplayColor;

    public Color blueBonusDisplayColor;



    public enum ActionType { MOVE, ATK1, SKILL1, SKILL2, SKIP, WAIT }; // Edited by Rayane TALEB L3C2
    public ActionType actionType;
    public List<ActionAIPos> decisionSequence; // AI decisions
    public List<(Character chr, ActionAIPos act)> decisionSequenceCPU; // CPU decisions
    public Slider slider;
    public int countTurn;

    public Material pathDisplayMat;
    public Material bonusDisplayMat;

    public Material allPathsDisplayMat;
    public Material lineOfSightMat;
    public Material blockedSightMat;
    public Material aoeMat;
    List<Hexa> bonusPoints;
    Hexa caseBonus;

    //Awake is called before Start
    void Awake()
    {
        Application.targetFrameRate = 75;
        QualitySettings.vSyncCount = 0;
    }

    // Start is called before the first frame update
    // Initialisation of variables and game settings
    //Author : ??
    //Edited by 
    void Start()
    {

        //Initialisation of textures and colors
        pathDisplayMat = new Material(Shader.Find("Standard"));
        bonusDisplayMat = new Material(Shader.Find("Standard"));
        allPathsDisplayMat = new Material(Shader.Find("Standard"));
        lineOfSightMat = new Material(Shader.Find("Standard"));
        blockedSightMat = new Material(Shader.Find("Standard"));
        aoeMat = new Material(Shader.Find("Standard"));

        pathDisplayMat.color = pathDisplayColor;
        allPathsDisplayMat.color = allPathsDisplayColor;
        lineOfSightMat.color = lineOfSightColor;
        blockedSightMat.color = blockedSightColor;
        aoeMat.color = AoEColor;

        // Initialisation of Hexas and Characters global variables
        Hexa.hexasFolder = hexasFolder;
        Hexa.hexaFilledMesh = hexaFilledMesh;
        Hexa.hexaHollowMesh = hexaHollowMesh;
        Hexa.hexaWallMesh = hexaWallMesh;
        Hexa.hexaTemplate = hexaTemplate;
        Character.characterTemplate = characterTemplate;
        Character.characterTemplateModels = characterTemplateModels;
        Character.charactersFolder = charactersFolder;
        CharsDB.initCharsDB();
        bonusPoints = new List<Hexa>();
        Hexa.offsetX = -((tileW - 1) * 0.75f) / 2;
        Hexa.offsetY = -((tileH - 1) * -0.86f + ((tileW - 1) % 2) * 0.43f) / 2;
        List<Point> casesBonus = new List<Point>();
        hexaHighlight.GetComponent<Renderer>().material.color = new Color(1, 1, 1, 0.25f);
        frame = 0;
        updateUI = true;
        updateMouseHover = true;
        pauseMenu = false;
        cameraMoved = false;
        bonusTeam = -1;

        // Initialisation of game data if it's not (it should be in the main menu)
        if (startGameData == null)
        {
            startGameData = new StartGameData();
            startGameData.loadSave = false;
            startGameData.charsTeam1 = new List<CharClass>();
            startGameData.charsTeam1.Add(CharClass.VOLEUR);
            startGameData.charsTeam1.Add(CharClass.GUERRIER);
            startGameData.charsTeam2 = new List<CharClass>();
            startGameData.charsTeam2.Add(CharClass.VOLEUR);
            startGameData.charsTeam2.Add(CharClass.DRUIDE);
            startGameData.player1Type = PlayerType.AI_CPU_Defense;
            startGameData.player2Type = PlayerType.AI_CPU_Offense;
            startGameData.mapChosen = 1;
        }

        if (startGameData.loadSave)
        {
            loadGame();
        }
        else
        {
            // Initialisation of the map (hexa grid)
            slider.value = startGameData.slider;
            hexaGrid = new HexaGrid();
            if (startGameData.mapChosen == 0)
            {
                hexaGrid.createGridFromFile(Application.streamingAssetsPath + "/Data/Map/ruins");
                tileW = hexaGrid.w; tileH = hexaGrid.h;
                ruinsMap.SetActive(true);
                foreach (Hexa hexa in hexaGrid.hexaList)
                {
                    if (hexa.type != HexaType.GROUND && hexa.type != HexaType.BONUS)
                    {
                        hexa.go.GetComponent<Renderer>().enabled = false;
                    }
                }
            }
            else if (startGameData.mapChosen >= 1)
            {
                hexaGrid.createRandomRectGrid(tileW, tileH);
            }

            caseBonus = initBonus();
            Initiative.SetActive(true);

            // Put characters on the grid
            for (int i = 0; i < 5; i++)
            {
                if (i < startGameData.charsTeam1.Count) hexaGrid.addChar(startGameData.charsTeam1[i], tileW / 2 - 4 + 2 + i, tileH - 2, 0);
                if (i < startGameData.charsTeam2.Count) hexaGrid.addChar(startGameData.charsTeam2[i], tileW / 2 - 4 + 2 + i, 2, 1);
            }

            hexaGrid.charList.Sort();


            foreach (Character c in hexaGrid.charList)
            {
                hexaGrid.getHexa(c.x, c.y).changeType(HexaType.GROUND);
            }

            for (int i = hexaGrid.charList.Count; i <= 10; i++)
                Initiative.transform.GetChild(i).transform.position = new Vector3(10000, 10000, 0);

            // Initialisation of the current character cursor
            currentCharControlledID = 0;
            currentCharControlled = hexaGrid.charList[currentCharControlledID];

        }
        // Initialisation of the AI 
        decisionSequence = new List<ActionAIPos>();
        decisionSequenceCPU = new List<(Character chr, ActionAIPos act)>();
        AI.hexaGrid = hexaGrid;

        AIHard.learn = false;
        AIUtil.hexaGrid = hexaGrid;

        mousePos = Input.mousePosition;
        hexaHovered = null;
        hexaHoveredOld = null;
        charHovered = null;

        countTurn = 0;

        pathFinderDisplay = new List<GameObject>();
        lineOfSightDisplay = new List<GameObject>();
        pathWalk = null;
        attackUsed = 0;
        pathWalkpos = 0;
        newTurn = 0;
        winner = -1;
        statsGame = new StatsGame();
        actionType = ActionType.MOVE;
        UICharTurns.SetActive(true);
        UIAction.SetActive(true);
        { // Init character turn list UI
            int i = 0;
            foreach (Character c in hexaGrid.charList)
            {
                GameObject go = GameObject.Instantiate(UICharTurnTemplate, UICharTurns.transform);
                go.SetActive(false);
                go.transform.localPosition = new Vector3(200 + i, 0, 0);
                go.transform.GetChild(0).GetComponent<Image>().color = (c.team == 0) ? Character.TEAM_1_COLOR : Character.TEAM_2_COLOR;
                go.transform.GetChild(1).GetComponent<RawImage>().texture = charCards[(int)c.charClass];
                UICharTurnsList.Add(go);
                i += 80;
            }
        }
        displayInitiative();
        // Initialisation of the Camera
        cameraPosGoal = cameraPos.position;
        lockedCamera = (startGameData.player1Type != PlayerType.HUMAN && startGameData.player2Type != PlayerType.HUMAN);
        toggleLockedCamera.isOn = lockedCamera;
        AIwaitingTime = lockedCamera ? 0 : 20;
    }

    // Update is called once per frame
    //Author : ??
    //Edited by L3Q1 VALAT Thibault
    void Update()
    {

        //Change controlled character
        getHoveredHexa();
        if (Input.GetMouseButton(1) && charHovered != null && charHovered.PA > 0 && charHovered.team == currentCharControlled.team)
        {
            currentCharControlled = charHovered;
        }
        frame++;

        // Update mouse position
        mousePosOld = mousePos;
        mousePos = Input.mousePosition;

        hexaGrid.getHexa(16, 8).changeColor(new Color(0, 255, 0));

        // PAUSE MENU
        if (pauseMenu)
        {
            if (Input.GetMouseButtonDown(0))
            {
                int menuPosX = Screen.width / 2 - 80;
                int menuPosY = Screen.height / 2 + 100 - 100;
                // Save the current game
                if (mousePos.x >= menuPosX && mousePos.x < menuPosX + 160 && mousePos.y >= menuPosY - 15 && mousePos.y < menuPosY - 15 + 20)
                {
                    saveGame();
                }
                // To restart the game:
                else if (mousePos.x >= menuPosX && mousePos.x < menuPosX + 160 && mousePos.y >= menuPosY - 47 && mousePos.y < menuPosY - 47 + 20)
                {
                    Debug.Log("Restarting game");
                    SceneManager.LoadScene(1);


                }
                // to quit the game:
                else if (mousePos.x >= menuPosX && mousePos.x < menuPosX + 160 && mousePos.y >= menuPosY - 79 && mousePos.y < menuPosY - 79 + 20)
                {
                    MainMenu.startGameData = new StartGameData();
                    MainMenu.startGameData.slider = slider.value;
                    SceneManager.LoadScene(0);

                }
                // To go back:
                else if (mousePos.x >= Screen.width / 2 - 80 && mousePos.x < Screen.width / 2 + 80 && mousePos.y >= Screen.height - 30 && mousePos.y < Screen.height)
                {
                    pauseMenu = false;
                    UIPauseMenu.SetActive(false);
                }
                lockedCamera = toggleLockedCamera.isOn;
            }
        }
        //When there is still no winner
        else if (winner == -1)
        {
            float forceCamX = 0.0f;
            float forceCamY = 0.0f;
            float forceCamZ = 0.0f;
            float scrollWheelStrengh = 7.0f;

            // ZOOM (MOUSEWHEEL)
            if (Input.GetAxis("Mouse ScrollWheel") != 0.0f)
            {
                forceCamY -= Input.GetAxis("Mouse ScrollWheel") * scrollWheelStrengh;
            }

            //Camera
            if (!lockedCamera)
            {
                // MOVE CAMERA (MIDDLE CLICK OR ZQSD (AZERTY keyboard))
                if (Input.GetMouseButton(0))
                {
                    forceCamX += (mousePosOld.x - mousePos.x) * 0.003f * cameraPosGoal.y;
                    forceCamZ += (mousePosOld.y - mousePos.y) * 0.003f * cameraPosGoal.y;
                }
                if (Input.GetKey(KeyCode.Z))
                {
                    forceCamZ += 1;
                }
                if (Input.GetKey(KeyCode.Q))
                {
                    forceCamX -= 1;
                }
                if (Input.GetKey(KeyCode.S))
                {
                    forceCamZ -= 1;
                }
                if (Input.GetKey(KeyCode.D))
                {
                    forceCamX += 1;
                }
            }

            //Move the camera depending on the directions
            Vector3 newCameraPosGoal = new Vector3(cameraPosGoal.x + forceCamX, cameraPosGoal.y + forceCamY, cameraPosGoal.z + forceCamZ);
            cameraMoved = false;
            if (newCameraPosGoal.x > -21 && newCameraPosGoal.x < 23)
            {
                cameraMoved = true;
                cameraPosGoal.x = newCameraPosGoal.x;
            }

            if (newCameraPosGoal.y > 8.0f && newCameraPosGoal.y < 20.0f)
            {
                cameraMoved = true;
                cameraPosGoal.y = newCameraPosGoal.y;
                newCameraPosGoal.z += Input.GetAxis("Mouse ScrollWheel") * scrollWheelStrengh;
            }

            if (newCameraPosGoal.z > -27 && newCameraPosGoal.z < 10)
            {
                cameraMoved = true;
                cameraPosGoal.z = newCameraPosGoal.z;
            }
            if (forceCamX == 0.0f && forceCamY == 0.0f && forceCamZ == 0.0f)
            {
                cameraMoved = false;
            }

            // OPEN PAUSE MENU
            if (Input.GetMouseButtonDown(0))
            {
                if (mousePos.x >= Screen.width / 2 - 80 && mousePos.x < Screen.width / 2 + 80 && mousePos.y >= Screen.height - 30 && mousePos.y < Screen.height)
                {
                    pauseMenu = true;
                    UIPauseMenu.SetActive(true);
                }
            }
        }

        // SMOOTH CAMERA
        cameraPos.position = new Vector3(cameraPos.position.x * 0.85f + cameraPosGoal.x * 0.15f, cameraPos.position.y * 0.85f + cameraPosGoal.y * 0.15f, cameraPos.position.z * 0.85f + cameraPosGoal.z * 0.15f);


        // MAIN GAME LOOP a MOdifier
        if (winner == -1)
        {
            if (pathWalk != null)
            {
                // Walking animation when going from an hexa to another
                walkingAnimation();
            }
            else if (attackUsed > 0)
            {
                // Attack animation
                if (attackUsed == 1)
                {
                    useAttack();
                }
                attackUsed--;
                // Interaction with the game
            }
            else if (!pauseMenu)
            {
                PlayerType currentPlayerType = whoControlsThisChar(currentCharControlled);
                // ACTIONS FOR HUMAN PLAYERS
                if (currentPlayerType == PlayerType.HUMAN)
                {
                    // HOVER DETECTION : hovered hexa is stored in hexaHovered
                    getHoveredHexa();



                    // W Key : Move
                    if (Input.GetKeyDown(KeyCode.W))
                    {
                        actionType = ActionType.MOVE;
                        updateMouseHover = true; updateUI = true;
                    }

                    // X Key : Attack
                    if (Input.GetKeyDown(KeyCode.X))
                    {
                        actionType = ActionType.ATK1;
                        updateMouseHover = true; updateUI = true;
                    }

                    // C Key : Skill
                    if (Input.GetKeyDown(KeyCode.C))
                    {
                        if (currentCharControlled.skillAvailable)
                            actionType = ActionType.SKILL1;

                        updateMouseHover = true; updateUI = true;
                    }

                    // B Key : Skill 2 by Rayane TALEB L3C2
                    if (Input.GetKeyDown(KeyCode.B))
                    {
                        if (currentCharControlled.skill2Available)
                            actionType = ActionType.SKILL2;

                        updateMouseHover = true; updateUI = true;
                    }

                    // V Key : Skip
                    if (Input.GetKeyDown(KeyCode.V))
                    {
                        currentCharControlled.PA = 0;
                        nextTurn();
                    }


                    // Left Click : action (Move / Attack) or change action Type (UI) or skip turn
                    // Click on the left top buttons, under characters card
                    if (!cameraMoved && Input.GetMouseButtonUp(0) && (cameraPos.position - cameraPosGoal).magnitude <= 0.2f)
                    {
                        /*Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                        RaycastHit hit;
                        // Deplacement
                        if (Physics.Raycast(ray, out hit))
                        {
                            if (hit.collider.gameObject.name == "UIActionMove")
                            {
                                actionType = ActionType.MOVE;
                            }
                            // Attack
                            else if (hit.collider.gameObject.name == "UIActionAttack")
                            {
                                actionType = ActionType.ATK1;
                            }
                            // Special attack
                            else if (hit.collider.gameObject.name == "UIActionSkill1")
                            {
                                if (currentCharControlled.skillAvailable) actionType = ActionType.SKILL1;
                            }
                            else if (hit.collider.gameObject.name == "UIActionSkill2")
                            {
                                if (currentCharControlled.skillAvailable) actionType = ActionType.SKILL2;
                            }
                            // Next turn
                            else if (hit.collider.gameObject.name == "UIActionSkip")
                            {
                                currentCharControlled.PA = 0;
                                nextTurn();
                            }
                        }*/
                        if (mousePos.x >= 28 && mousePos.x < 28 + 250 && mousePos.y >= Screen.height - 470 && mousePos.y < Screen.height - 470 + 65)
                        {
                            actionType = ActionType.MOVE;
                        }
                        else if (mousePos.x >= 28 && mousePos.x < 28 + 250 && mousePos.y >= Screen.height - 540 && mousePos.y < Screen.height - 540 + 65)
                        {
                            actionType = ActionType.ATK1;
                        }
                        else if (mousePos.x >= 28 && mousePos.x < 28 + 250 && mousePos.y >= Screen.height - 610 && mousePos.y < Screen.height - 610 + 65)
                        {
                            if (currentCharControlled.skillAvailable) actionType = ActionType.SKILL1;
                        }
                        else if (mousePos.x >= 28 && mousePos.x < 28 + 250 && mousePos.y >= Screen.height -680 && mousePos.y < Screen.height - 680 + 65)
                        {
                            if (currentCharControlled.skill2Available) actionType = ActionType.SKILL2; // edited by L3C2 Rayane TALEB
                        }
                        else if (mousePos.x >= 28 && mousePos.x < 28 + 250 && mousePos.y >= Screen.height - 750 && mousePos.y < Screen.height - 750 + 65)
                        {
                            currentCharControlled.PA = 0;
                            nextTurn();
                        }
                        else
                        {
                            switch (actionType)
                            {
                                case ActionType.MOVE: actionMove(hexaHovered); break;
                                case ActionType.ATK1:
                                case ActionType.SKILL1: actionUseAttack(actionType, hexaHovered); break;
                                case ActionType.SKILL2: actionUseAttack(actionType, hexaHovered); break; // edited by L3C2 Rayane TALEB
                                case ActionType.SKIP: break;
                            }
                        }
                    }
                }
                //If the player isnot human
                else
                {
                    if (newTurn > AIwaitingTime)
                    {
                        if (decisionSequence.Count == 0 && decisionSequenceCPU.Count == 0)
                        {
                            switch (currentPlayerType)
                            {
                                case PlayerType.HUMAN: // failsafe
                                case PlayerType.AI_CPU_Defense: decisionSequenceCPU = CPU.decideDefense(currentCharControlled.team, hexaGrid); break;
                                case PlayerType.AI_CPU_Offense: decisionSequenceCPU = CPU.decideOffense(currentCharControlled.team, hexaGrid, caseBonus); break;
                                //case PlayerType.AI_CPU: decisionSequenceCPU = CPU.chooseStrategy(currentCharControlled.team, hexaGrid);  break;


                                /*case PlayerType.AI_CPU: if(CPUStrategy == 0) decisionSequenceCPU = CPU.decideDefense(currentCharControlled.team, hexaGrid);  break;
								 *					      else if(CPUStrategy == 1) decisionSequenceCPU = CPU.decideOffense(currentCharControlled.team, hexaGrid);  break;
								 *					      else if(CPUStrategy == 2) decisionSequenceCPU = CPU.chooseStrategy(currentCharControlled.team, hexaGrid);  break;	
								*/

                                /*case PlayerType.AI_EASY: decisionSequence = AIEasy.decide(currentCharControlled); break;
                                case PlayerType.AI_MEDIUM: decisionSequence = AIMedium.decide(currentCharControlled, statsGame); break; //change for the AI medium afterwards
                                case PlayerType.AI_HARD: decisionSequence = AIHard.decide(currentCharControlled, statsGame); break;*/
                            }
                        }
                        else
                        {
                            if (decisionSequence.Count > 0)
                            {
                                ActionAIPos actionAIPos = decisionSequence[0];
                                decisionSequence.RemoveAt(0);
                                //Debug.Log(currentPlayerType + " " + charToString(currentCharControlled) + " : " + actionAIPos.action + ((actionAIPos.pos != null) ? (" " + actionAIPos.pos.x + " " + actionAIPos.pos.y) : ""));
                                //Debug.Log("Targeted hexa : " + charToString(hexaGrid.getHexa(actionAIPos.pos.x, actionAIPos.pos.y).charOn));

                                switch (actionAIPos.action)
                                {
                                    case ActionType.MOVE: actionMove(hexaGrid.getHexa(actionAIPos.pos)); break;
                                    case ActionType.ATK1: actionUseAttack(actionAIPos.action, hexaGrid.getHexa(actionAIPos.pos)); break;
                                    case ActionType.SKILL1:
                                        actionUseAttack(actionAIPos.action, hexaGrid.getHexa(actionAIPos.pos));
                                        break;
                                    case ActionType.SKILL2:
                                        actionUseAttack(actionAIPos.action, hexaGrid.getHexa(actionAIPos.pos));
                                        break;
                                    case ActionType.SKIP: { currentCharControlled.PA = 0; nextTurn(); } break;
                                    default: break;
                                }
                            }
                            else // decisionSequenceCPU.Count > 0
                            {
                                setControlledCharacter(decisionSequenceCPU[0].chr);
                                ActionAIPos actionAIPos = decisionSequenceCPU[0].act;
                                decisionSequenceCPU.RemoveAt(0);


                                /*//Debug.Log(currentPlayerType + " " + charToString(currentCharControlled) + " : " + actionAIPos.action + ((actionAIPos.pos != null) ? (" " + actionAIPos.pos.x + " " + actionAIPos.pos.y) : ""));
                                if (actionAIPos.pos != null)
                                    Debug.Log(actionAIPos.action + " to hexa " + (actionAIPos.pos != null ? (actionAIPos.pos.x + " " + actionAIPos.pos.y) : "null")
                                        + " : " + charToString(hexaGrid.getHexa(actionAIPos.pos.x, actionAIPos.pos.y).charOn));*/

                                switch (actionAIPos.action)
                                {
                                    case ActionType.MOVE:
                                        actionMove(hexaGrid.getHexa(actionAIPos.pos));
                                        break;
                                    case ActionType.ATK1:
                                        actionUseAttack(actionAIPos.action, hexaGrid.getHexa(actionAIPos.pos));
                                        break;
                                    case ActionType.SKILL1:
                                        actionUseAttack(actionAIPos.action, hexaGrid.getHexa(actionAIPos.pos));
                                        break;
                                    case ActionType.SKILL2:
                                        actionUseAttack(actionAIPos.action, hexaGrid.getHexa(actionAIPos.pos));
                                        break;
                                    case ActionType.SKIP:
                                        currentCharControlled.PA = 0;
                                        nextTurn();
                                        break;

                                    default: break;
                                }
                            }
                        }
                    }
                }
            }
        }
        // DISPLAY WINNER if there is a winner
        else
        {
            UIPauseMenu.SetActive(false);
            UICharTurns.SetActive(false);
            UIAction.SetActive(false);
            UIVictoryScreen.SetActive(true);
            UIVictoryScreen.transform.GetChild(0).GetComponent<Text>().text = "VICTOIRE DE L'EQUIPE " + ((winner == 0) ? "BLEUE" : "ROUGE");
            UIVictoryScreen.transform.GetChild(0).GetComponent<Text>().color = ((winner == 0) ? Character.TEAM_1_COLOR : Character.TEAM_2_COLOR);

            // Back to menu
            if (Input.GetMouseButtonDown(0))
            {
                if (mousePos.x >= Screen.width / 2 - 150 && mousePos.x < Screen.width / 2 + 150 && mousePos.y >= Screen.height / 2 - 90 && mousePos.y < Screen.height / 2 - 90 + 60)
                {
                    Debug.Log("BINGO");
                    // EVALUATE AI HARD
                    // to delete
                    statsGame.endGame(winner, hexaGrid);
                    if (AIHard.learn) statsGame.evaluateGame();
                    statsGame.evaluateGame();
                    MainMenu.startGameData = new StartGameData();
                    MainMenu.startGameData.slider = slider.value;
                    SceneManager.LoadScene(0);
                }
            }
        }

        // - DISPLAYS -------------------------------------------------------------------------------


        // CENTER CHARACTER MODEL
        foreach (Character c in hexaGrid.charList)
        {
            if (c.go.transform.GetChild(1)) c.go.transform.GetChild(1).transform.position = c.go.transform.position;
        }
        if (winner == -1)
        {
            // Display arrow above the current character controlled
            {
                float currentHeight = (((newTurn % 60) < 30) ? (newTurn % 60) : (60 - (newTurn % 60))) / 60.0f * 0.2f;
                arrow.transform.position = new Vector3(currentCharControlled.go.transform.position.x, currentHeight + 1.5f, currentCharControlled.go.transform.position.z);
                if (newTurn == 0)
                {
                    hexaHighlight.GetComponent<MeshFilter>().mesh = hexaHollowMesh;
                }
                else if (newTurn < 10)
                {
                    hexaHighlight.transform.localScale = new Vector3(1 + (10 - newTurn) * 0.3f, 1 + (10 - newTurn) * 0.3f, 1);
                }
                else if (newTurn == 10)
                {
                    hexaHighlight.transform.localScale = new Vector3(1, 1, 1);
                    hexaHighlight.GetComponent<MeshFilter>().mesh = hexaFilledMesh;
                }
                hexaHighlight.transform.position = new Vector3(currentCharControlled.go.transform.position.x, -0.013f, currentCharControlled.go.transform.position.z);
                newTurn++;
            }

            if (updateMouseHover)
            {
                // Clear previous hexas displayed
                foreach (GameObject go in pathFinderDisplay) GameObject.Destroy(go);
                pathFinderDisplay = new List<GameObject>();
                foreach (GameObject go in lineOfSightDisplay) GameObject.Destroy(go);
                lineOfSightDisplay = new List<GameObject>();
                // Display hovered hexa
                displayHoveredHexa();
            }

            // Display path in green / line of sight in blue / AoE in red
            if (pathWalk == null && updateMouseHover && whoControlsThisChar(currentCharControlled) == PlayerType.HUMAN)
            {
                switch (actionType)
                {
                    case ActionType.MOVE: displayPossiblePaths(); displaySortestPath(); if (Input.GetKey(KeyCode.LeftControl)) displayLineOfSight(); break; //now displays the attack range of the controlled character if we press "Left Control". 
                    case ActionType.ATK1: case ActionType.SKILL1: case ActionType.SKILL2: displayLineOfSight(); break; // edited by L3C2 Rayane TALEB
                    case ActionType.SKIP: break;
                }
            }

            // Display UI
            if (updateUI)
            {
                displayNewCharTurnList();
                displayActionButtons();
                displaySkillHelp();
            }
        }

        updateMouseHover = false;
        updateUI = false;
    }

    // ##################################################################################################################################################
    // Functions used in main
    // ##################################################################################################################################################

    //Author : VALAT Thibault L3Q1
    //Initialize a random bonus between : center-left of the map, center, or center right
    Hexa initBonus()
    {
        int bonusPlace = UnityEngine.Random.Range(1, 4);
        int x = 0;
        int y = 0;
        switch (bonusPlace)
        {
            case 1:
                displayBonus(hexaGrid.findAllPaths(5, 16, 2));
                x = 5;
                y = 16;
                break;
            case 2:
                displayBonus(hexaGrid.findAllPaths(17, 15, 2));
                x = 17;
                y = 15;
                break;
            case 3:
                displayBonus(hexaGrid.findAllPaths(29, 15, 2));
                x = 29;
                y = 15;
                break;
        }
        return hexaGrid.getHexa(x, y);
    }

    //Author : VALAT Thibault L3Q1
    //Display Bonus hexas in gray (neutral)
    void displayBonus(List<Point> points)
    {
        foreach (Point p in points)
        {
            Hexa point = hexaGrid.getHexa(p);
            point.changeType(HexaType.BONUS);
            bonusPoints.Add(point);

            GameObject go = GameObject.Instantiate(hexaTemplate, hexasFolder.transform);
            go.SetActive(true);
            go.GetComponent<Transform>().position = Hexa.hexaPosToReal(p.x, p.y, -0.015f);
            go.GetComponent<MeshFilter>().mesh = hexaFilledMesh;
            go.GetComponent<Renderer>().sharedMaterial = bonusDisplayMat;
            go.GetComponent<Collider>().enabled = false;
            bonusDisplayMat.color = neutralBonusDisplayColor;
        }
    }

    //Author : VALAT Thibault L3Q1
    //Display Bonus hexas in gray (neutral), red or blue and change during the game
    void checkAndUpdateBonusControll()
    {
        bool redInBonusZone = false;
        bool blueInBonusZone = false;

        //Check if there is zero or one or two teams in the bonus zone
        foreach (Character c in hexaGrid.charList)
        {
            if (hexaGrid.getHexa(c.x, c.y).type == HexaType.BONUS && c.team == 0)
                blueInBonusZone = true;
            if (hexaGrid.getHexa(c.x, c.y).type == HexaType.BONUS && c.team == 1)
                redInBonusZone = true;
        }
        //Reset the bonus zone
        if ((!redInBonusZone && !blueInBonusZone) || (redInBonusZone && blueInBonusZone))
        {
            changeBonusColor(neutralBonusDisplayColor);
            giveBonusValue(-1);
        }
        //Give the bonus to the red team
        else if (redInBonusZone && !blueInBonusZone)
        {
            changeBonusColor(redBonusDisplayColor);
            giveBonusValue(1);
        }
        //Give the bonus to the blue team
        else if (!redInBonusZone && blueInBonusZone)
        {
            changeBonusColor(blueBonusDisplayColor);
            giveBonusValue(0);
        }

    }

    //Change the display of the bonus hexas and the bonus card
    //Author : VALAT Thibault L3Q1
    void changeBonusColor(Color teamColor)
    {
        //Change the color of the hexas
        foreach (Hexa h in bonusPoints)
        {
            GameObject go = GameObject.Instantiate(hexaTemplate, hexasFolder.transform);
            go.SetActive(true);
            go.GetComponent<Transform>().position = Hexa.hexaPosToReal(h.x, h.y, -0.015f);
            go.GetComponent<MeshFilter>().mesh = hexaFilledMesh;
            go.GetComponent<Renderer>().sharedMaterial = bonusDisplayMat;
            go.GetComponent<Collider>().enabled = false;
            bonusDisplayMat.color = teamColor;
        }

        //Change the color of the bonus card
        switch (bonusTeam)
        {
            case -1:
                BonusCard.transform.GetChild(0).GetComponent<Image>().color = new Color(0.5f, 0.5f, 0.5f);
                BonusCard.transform.GetChild(2).GetComponent<Text>().text = "Bonus"; break;
            case 1:
                BonusCard.transform.GetChild(0).GetComponent<Image>().color = new Color(1, 0.2f, 0.2f);
                BonusCard.transform.GetChild(2).GetComponent<Text>().text = "+1 HP"; break;
            case 0:
                BonusCard.transform.GetChild(0).GetComponent<Image>().color = new Color(0.2f, 0.2f, 1);
                BonusCard.transform.GetChild(2).GetComponent<Text>().text = "+1 HP"; break;
        }

    }

    //Give the +1HP bonus to the team controlling the bonus
    //Author : VALAT Thibault L3Q1  
    void giveBonusValue(int bonusControlledTeam)
    {
        //Reset the bonus value if the team which had it lost it
        if (bonusControlledTeam == -1 && bonusTeam != -1)
        {
            foreach (Character c in hexaGrid.charList)
                if (c.team == bonusTeam)
                    c.HP--;
            bonusTeam = -1;
        }
        //Give the bonus to the blue team
        else if (bonusControlledTeam == 0 && bonusTeam != 0)
        {
            foreach (Character c in hexaGrid.charList)
            {
                if (c.team == bonusTeam)
                    c.HP--;
                if (c.team == bonusControlledTeam)
                    c.HP++;
            }
            bonusTeam = 0;
        }
        //Give the bonus to the red team
        else if (bonusControlledTeam == 1 && bonusTeam != 1)
        {
            foreach (Character c in hexaGrid.charList)
            {
                if (c.team == bonusTeam)
                    c.HP--;
                if (c.team == bonusControlledTeam)
                    c.HP++;
            }
            bonusTeam = 1;
        }
    }

    // Return the team with the active bonus
    //Author : GOUVEIA Klaus L3Q1
    public static int getBonusTeam()
    {
        return bonusTeam;
    }


    // Return the playerType of character (humaun ...)
    //Author : ??
    PlayerType whoControlsThisChar(Character c)
    {
        return (c.team == 0) ? startGameData.player1Type : startGameData.player2Type;
    }

    //Stock the current hoverred hexa
    //Author : ??
    void getHoveredHexa()
    {
        if ((mousePos.x >= 35 && mousePos.x < 35 + 140 && mousePos.y >= Screen.height - 318 && mousePos.y < Screen.height - 318 + 24) ||
        (mousePos.x >= 35 && mousePos.x < 35 + 140 && mousePos.y >= Screen.height - 348 && mousePos.y < Screen.height - 348 + 24) ||
        (mousePos.x >= 35 && mousePos.x < 35 + 140 && mousePos.y >= Screen.height - 377 && mousePos.y < Screen.height - 377 + 24) ||
        (mousePos.x >= 35 && mousePos.x < 35 + 140 && mousePos.y >= Screen.height - 406 && mousePos.y < Screen.height - 406 + 24) ||
        (mousePos.x >= Screen.width / 2 - 80 && mousePos.x < Screen.width / 2 + 80 && mousePos.y >= Screen.height - 30 && mousePos.y < Screen.height))
        {
            if (hexaHovered != null)
            {
                hexaHoveredOld = hexaHovered;
                hexaHovered = null;
                charHovered = null;
            }
            updateMouseHover = true;
            updateUI = true;
        }
        else
        {
            RaycastHit raycastHit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Debug.DrawRay(ray.origin, ray.direction * 100, Color.red);
            bool success = false;
            if (Physics.Raycast(ray.origin, ray.direction, out raycastHit, 100)) success = (raycastHit.transform.gameObject.tag == "Hexa");
            if (success)
            {
                Hexa hexaHit = raycastHit.transform.gameObject.GetComponent<HexaGO>().hexa;
                if (hexaHit != hexaHovered)
                {
                    hexaHoveredOld = hexaHovered;
                    hexaHovered = hexaHit;
                    charHovered = hexaHovered.charOn;
                    updateMouseHover = true;
                    updateUI = true;
                }
            }
            else
            {
                if (hexaHovered != null)
                {
                    hexaHoveredOld = hexaHovered;
                    hexaHovered = null;
                    charHovered = null;
                }
                updateMouseHover = true;
                updateUI = true;
            }
        }
    }

    //get the controlled character
    //Author : ??
    void getControlledCharacter()
    {

        //stocks the hovered hexa
        getHoveredHexa();

        if (Input.GetMouseButton(0) && hexaHovered != null)
        {
            foreach (Character c in hexaGrid.charList)
            {
                if (c.team == currentCharControlled.team)
                {
                    if (hexaHovered.x == c.x && hexaHovered.y == c.y)
                    {
                        currentCharControlled = c;
                    }
                }
            }
        }
    }

    //Set the controlled character
    //Author : ??
    public void setControlledCharacter(Character c)
    {
        currentCharControlled = c;
    }

    //Move the character 
    //Author : ??
    void actionMove(Hexa hexaDestination)
    {
        //Move the character when its possible
        if (hexaDestination != null && (hexaDestination.type == HexaType.GROUND || (hexaDestination.type == HexaType.BONUS)))
        {
            List<Point> path = hexaGrid.findShortestPath(currentCharControlled.x, currentCharControlled.y, hexaDestination.x, hexaDestination.y, currentCharControlled.PM);
            if (path != null && path.Count > 1)
            {
                pathWalk = path;
                pathWalkpos = 0;
            }
            else
            {
                Debug.LogWarning("ActionMove Error: " + hexaDestination.x + " " + hexaDestination.y +
                    "\nHexa type : \t" + hexaDestination.type +
                    "\nOn hexa : \t" + charToString(hexaDestination.charOn));
            }
        }
        else Debug.LogWarning("ActionMove Error: No Hexa found.");
    }

    //Attack with the current character, the hexa clicked
    //Author : ?? a MOdifier
    // edited by L3C2 Rayane TALEB
    void actionUseAttack(ActionType attack, Hexa hexaDestination)
    {
        //Set the attack used
        CharsDB.Attack attackUsed_;
        if (attack == ActionType.ATK1)
        {
            attackUsed_ = CharsDB.list[(int)currentCharControlled.charClass].basicAttack;
        }
        else if (attack == ActionType.SKILL1)
        {
            attackUsed_ = CharsDB.list[(int)currentCharControlled.charClass].skill_1;
        }
        else
        {
            attackUsed_ = CharsDB.list[(int)currentCharControlled.charClass].skill_2; // edited by L3C2 Rayane TALEB
        }
        //Use the attack if it's possible
        if (hexaDestination != null && hexaGrid.hexaInSight(currentCharControlled.x, currentCharControlled.y, hexaDestination.x, hexaDestination.y, attackUsed_.range))
        {


            if (attack == ActionType.SKILL1)
            {
                currentCharControlled.skillAvailable = false;
                actionType = ActionType.ATK1;
            }
            if (attack == ActionType.SKILL2) // edited by L3C2 Rayane TALEB
            {
                currentCharControlled.skill2Available = false;
                actionType = ActionType.SKILL1;
            }
            // Attack animation
            Animator animator = currentCharControlled.go.transform.GetChild(1).GetComponent<Animator>();
            if (animator)
            {
                animator.SetTrigger("Attack1Trigger");
            }
            attackUsedAttack = attackUsed_;
            attackUsedTargetHexa = new Point(hexaDestination.x, hexaDestination.y);
            attackUsed = 30; // Delay attack

            // Particles for soigneur
            if (currentCharControlled.charClass == CharClass.SOIGNEUR) 
            {
                GameObject go = GameObject.Instantiate(particleHeal);
                go.transform.position = Hexa.hexaPosToReal(hexaDestination.x, hexaDestination.y, 0);
                go.transform.localScale *= 0.1f;
                Destroy(go, 5);
            }
            //Particles for mage
            else if (currentCharControlled.charClass == CharClass.MAGE)
            {
                GameObject go = GameObject.Instantiate(particleExplosion);
                go.transform.position = Hexa.hexaPosToReal(hexaDestination.x, hexaDestination.y, 0);
                go.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
                Destroy(go, 5);
            }

            else if (currentCharControlled.charClass == CharClass.DRUIDE) // edited by Rayane TALEB L3C2
            {
                GameObject go = GameObject.Instantiate(particleHeal);
                go.transform.position = Hexa.hexaPosToReal(hexaDestination.x, hexaDestination.y, 0);
                go.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
                Destroy(go, 5);
            }

            // calculate the angle the character will be facing
            Vector3 v1 = Hexa.hexaPosToReal(hexaDestination.x, hexaDestination.y, 0);
            Vector3 v2 = Hexa.hexaPosToReal(currentCharControlled.x, currentCharControlled.y, 0);
            float x = v1.x - v2.x;
            float y = v1.z - v2.z;
            float d = Mathf.Sqrt(x * x + y * y);
            float cos_ = (x == 0) ? 0 : x / d;
            float angle = Mathf.Acos(cos_);
            if (y < 0) angle = -angle;
            int angleDegrees = (int)((angle * 180) / (Mathf.PI));
            if (angleDegrees < 0) angleDegrees = 360 + angleDegrees;
            angleDegrees = 360 - angleDegrees + 90;
            angleDegrees = (angleDegrees + 5) / 10 * 10;
            //Debug.Log(x + " " + y + " " + cos_ + " " + " " + angle + " " + angleDegrees);
            Transform charModel = currentCharControlled.go.transform.GetChild(1);
            if (charModel) charModel.eulerAngles = new Vector3(0, angleDegrees, 0);
        }
        //When it's not possible
        else
        {
            if (hexaDestination == null) Debug.Log("hexaDest null");
            else if (!hexaGrid.hexaInSight(currentCharControlled.x, currentCharControlled.y, hexaDestination.x, hexaDestination.y, attackUsed_.range))
                Debug.Log("hexa not in sight: from " + currentCharControlled.x + "," + currentCharControlled.y + " to " + hexaDestination.x + "," + hexaDestination.y);
        }
        updateMouseHover = true;
        updateUI = true;
    }

    //Use attack
    //// edited by L3C2 Rayane TALEB
    //Author : ?
    void useAttack()
    {
        //Get the characters hitted 
        List<Character> hits = hexaGrid.getCharWithinRange(attackUsedTargetHexa.x, attackUsedTargetHexa.y, attackUsedAttack.rangeAoE);
        // Filter target(s)
        if (attackUsedAttack.targetsEnemies == false)
        {
            for (int i = 0; i < hits.Count; i++)
            {
                if (hits[i].team != currentCharControlled.team)
                {
                    hits.RemoveAt(i); i--;
                }
            }
        }
        if (attackUsedAttack.targetsAllies == false)
        {
            for (int i = 0; i < hits.Count; i++)
            {
                if (hits[i].team == currentCharControlled.team)
                {
                    hits.RemoveAt(i); i--;
                }
            }
        }
        if (attackUsedAttack.targetsSelf == false)
        {
            for (int i = 0; i < hits.Count; i++)
            {
                if (hits[i] == currentCharControlled)
                {
                    hits.RemoveAt(i); i--;
                }
            }
        }

        //Give attack effects
        foreach (Character c in hits)
        {
            //If the attack effect is damage, give damages
            switch (attackUsedAttack.attackEffect)
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

    //Display the walking animation
    //Author : ??
    void walkingAnimation()
    {
        int speed = 6;
        if (pathWalkpos == 0)
        {
            Animator animator = currentCharControlled.go.transform.GetChild(1).GetComponent<Animator>();
            if (animator)
            {
                animator.SetBool("Moving", true);
                animator.SetBool("Running", true);
            }
        }
        if (pathWalkpos < (pathWalk.Count - 1) * speed)
        {
            for (int i = 0; i < 6; i++)
            {
                Point p = HexaGrid.findPos(pathWalk[pathWalkpos / speed].x, pathWalk[pathWalkpos / speed].y, (HexaDirection)i);
                if (p.x == pathWalk[pathWalkpos / speed + 1].x && p.y == pathWalk[pathWalkpos / speed + 1].y)
                {
                    currentCharControlled.setDirection((HexaDirection)i);
                    i = 6;
                }
            }

            float multiplier = (pathWalkpos % speed) / (float)speed;

            float x1 = pathWalk[pathWalkpos / speed].x * 0.75f;
            float x2 = pathWalk[pathWalkpos / speed + 1].x * 0.75f;
            float x = x1 * (1.0f - multiplier) + x2 * multiplier;

            float y1 = pathWalk[pathWalkpos / speed].y * -0.86f + (pathWalk[pathWalkpos / speed].x % 2) * 0.43f;
            float y2 = pathWalk[pathWalkpos / speed + 1].y * -0.86f + (pathWalk[pathWalkpos / speed + 1].x % 2) * 0.43f;
            float y = y1 * (1.0f - multiplier) + y2 * multiplier;
            currentCharControlled.go.transform.position = new Vector3(x + Hexa.offsetX, 0, y + Hexa.offsetY);
            pathWalkpos++;
        }
        else
        {
            currentCharControlled.updatePos(pathWalk[pathWalk.Count - 1].x, pathWalk[pathWalk.Count - 1].y, hexaGrid);
            Animator animator = currentCharControlled.go.transform.GetChild(1).GetComponent<Animator>();
            if (animator)
            {
                animator.SetBool("Moving", false);
                animator.SetBool("Running", false);
            }
            nextTurn();
        }
    }

    //Proceed to the next turn
    //Author : ?
    // Edited by Rayane TALEB L3C2
    void nextTurn()
    {
        checkAndUpdateBonusControll();
        displayInitiative();
        currentCharControlled.PA--;

        //Check if the controlled character can use special attack
        if (currentCharControlled.totalDamage >= 10)
        {
            if (currentCharControlled.charClass == CharClass.GUERRIER)
                currentCharControlled.totalDamage = 0;
            else
                currentCharControlled.totalDamage -= 10;
            currentCharControlled.skillAvailable = true;
        }

        //Check if the controlled character can use the second special attack by Rayane TALEB L3C2
        if (currentCharControlled.totalDamage2 >= 14)
        {
            if (currentCharControlled.charClass == CharClass.GUERRIER)
                currentCharControlled.totalDamage2 = 0;
            else
                currentCharControlled.totalDamage2 -= 14;
            currentCharControlled.skill2Available = true;
        }

        //When the controlled character has no more PA, his turn is over
        if (currentCharControlled.PA <= 0)
        {
            //Reset priority
            do
            {
                hexaGrid.charList.Sort();
                foreach (Character c in hexaGrid.charList)
                {
                    if (c.priority >= 5)
                        c.priority = c.priority - UnityEngine.Random.Range(1, 3);
                    else
                        c.priority--;
                }

                currentCharControlled.resetPriority();
                hexaGrid.charList.Sort();
                currentCharControlled = hexaGrid.charList[0];
                //Check if the char controlled has been affected
                currentCharControlled.isEffectMethod();// edited by L3C2 Rayane TALEB
                if (currentCharControlled.getIsStun())// edited by L3C2 Rayane TALEB
                {   currentCharControlled.isStun = false;
                    nextTurn();
                }
            } while (currentCharControlled.HP <= 0);

            PlayerType currentPlayerType = (currentCharControlled.team == 0) ? startGameData.player1Type : startGameData.player2Type;
            //Stats to remove
            /*if (currentPlayerType == PlayerType.AI_HARD || currentPlayerType == PlayerType.AI_MEDIUM)
            {
                statsGame.nextTurn(currentCharControlled);
            }*/
            actionType = ActionType.MOVE;
            newTurn = 0;
            decisionSequence = new List<ActionAIPos>();
            if (!lockedCamera) cameraPosGoal = new Vector3(currentCharControlled.go.transform.position.x, cameraPosGoal.y, ((hexaGrid.h * -0.43f) * 0.0f + currentCharControlled.go.transform.position.z * 1.0f) - cameraPosGoal.y * 0.75f);
        }
        updateUI = true;
        updateMouseHover = true;
        pathWalk = null;
        checkAndUpdateBonusControll();
        displayInitiative();
    }


    //Load a game
    //Author : ?
    //Edited by L3Q1, VALAT Thibault
    public void loadGame()
    {
        hexaGrid = new HexaGrid();
        int nbChar = 0;
        string conn = "URI=file:" + Application.streamingAssetsPath + "/Save/saveGame.db"; //Path to database.
        IDbConnection dbconn = (IDbConnection)new SqliteConnection(conn);
        dbconn.Open(); //Open connection to the database
        IDbCommand dbcmd = dbconn.CreateCommand();
        IDataReader reader;

        //If the map is ruins
        if (startGameData.mapChosen == 0)
        {
            hexaGrid.createGridFromFile(Application.streamingAssetsPath + "/Data/Map/ruins");
            tileW = hexaGrid.w; tileH = hexaGrid.h;
            ruinsMap.SetActive(true);
            foreach (Hexa hexa in hexaGrid.hexaList)
            {
                if (hexa.type != HexaType.GROUND && hexa.type != HexaType.BONUS)
                {
                    hexa.go.GetComponent<Renderer>().enabled = false;
                }
            }
        }
        //If it's a random map
        else
        {
            Hexa.offsetX = -((hexaGrid.w - 1) * 0.75f) / 2;
            Hexa.offsetY = -((hexaGrid.h - 1) * -0.86f + ((hexaGrid.w - 1) % 2) * 0.43f) / 2;

            dbcmd.CommandText = "SELECT type FROM board";
            reader = dbcmd.ExecuteReader();

            for (int j = 0; j < hexaGrid.h; j++)
            {
                for (int i = 0; i < hexaGrid.w && reader.Read(); i++)
                {
                    hexaGrid.hexaList.Add(new Hexa((HexaType)reader.GetInt32(1), i, j));
                }
            }
        }

        //Load the bonus zone
        dbcmd.CommandText = "SELECT bonusCenterX, bonusCenterY, bonusColor FROM board";
        reader = dbcmd.ExecuteReader();
        reader.Read();


        caseBonus = hexaGrid.getHexa((int)reader.GetInt32(0), (int)reader.GetInt32(1));
        bonusTeam = (int)reader.GetInt32(2);
        displayBonus(hexaGrid.findAllPaths(caseBonus.getx(), caseBonus.gety(), 2));
        reader.Close(); //close the reader to execute a new command
        reader = null;

        dbcmd.CommandText = "SELECT player1, player2, map, w, h, current, NBchar FROM game";
        reader = dbcmd.ExecuteReader();
        reader.Read();
        startGameData.player1Type = (PlayerType)reader.GetInt32(0);
        startGameData.player2Type = (PlayerType)reader.GetInt32(1);
        startGameData.mapChosen = (int)reader.GetInt32(2);
        hexaGrid.w = (int)reader.GetInt32(3);
        hexaGrid.h = (int)reader.GetInt32(4);
        currentCharControlledID = reader.GetInt32(5);
        nbChar = reader.GetInt32(6);


        reader.Close(); //close the reader to execute a new command
        reader = null;


        dbcmd.CommandText = "SELECT class, team, x, y, pa, hp, skillA, directionF, priority FROM characters";
        reader = dbcmd.ExecuteReader();
        for (int i = 0; i < nbChar && reader.Read(); i++)
        {
            CharClass cCharClass = (CharClass)reader.GetInt32(0);
            int cTeam = (int)reader.GetInt32(1);
            int cX = reader.GetInt32(2);
            int cY = reader.GetInt32(3);
            Character c = new Character(cCharClass, cX, cY, cTeam);
            c.PA = (int)reader.GetInt32(4);
            c.HP = (int)reader.GetInt32(5);
            c.skillAvailable = (reader.GetInt32(6) == 0) ? false : true;
            c.directionFacing = (HexaDirection)reader.GetInt32(7);
            c.priority = (int)reader.GetInt32(8);
            hexaGrid.addChar(c);
        }
        reader.Close(); //close the reader to execute a new command
        reader = null;
        dbcmd.CommandText = "DELETE FROM game";
        dbcmd.ExecuteNonQuery();
        dbcmd.CommandText = "INSERT INTO game (player1) VALUES (" + (-1) + ")";
        dbcmd.ExecuteNonQuery();

        currentCharControlled = hexaGrid.charList[currentCharControlledID];
        dbcmd.Dispose();
        dbcmd = null;
        dbconn.Close();
        dbconn = null;

        hexaGrid.charList.Sort();
        for (int i = hexaGrid.charList.Count; i <= 10; i++)
            Initiative.transform.GetChild(i).transform.position = new Vector3(10000, 10000, 0);

        checkAndUpdateBonusControll();
        displayInitiative();
    }

    //Save a game
    //Author : ?
    //Edited by L3Q1, VALAT Thibault
    public void saveGame()
    {
        if (!(File.Exists(Application.streamingAssetsPath + "/Save/saveGame.db")))
        {
            Mono.Data.Sqlite.SqliteConnection.CreateFile(Application.streamingAssetsPath + "/Save/saveGame.db");
            string conna = "URI=file:" + Application.streamingAssetsPath + "/Save/saveGame.db"; //Path to database.
            IDbConnection dbconna;
            dbconna = (IDbConnection)new SqliteConnection(conna);
            IDbCommand dbcmda = dbconna.CreateCommand();
            dbconna.Open(); //Open connection to the database.
            dbcmda.CommandText = "BEGIN TRANSACTION";
            dbcmda.ExecuteNonQuery();
            dbcmda.CommandText = "CREATE TABLE IF NOT EXISTS 'game' ('player1'INTEGER,'player2'INTEGER,'map'INTEGER,'w'INTEGER,'h'INTEGER,'current'INTEGER,'NBchar'INTEGER)";
            dbcmda.ExecuteNonQuery();
            dbcmda.CommandText = "CREATE TABLE IF NOT EXISTS 'characters' ('class'	INTEGER,'team'	INTEGER,'x'	INTEGER,'y'	INTEGER,'pa'	INTEGER,'hp'	INTEGER,'skillA'INTEGER,'directionF'	INTEGER, 'totalDamage'	INTEGER, 'priority'	INTEGER)";
            dbcmda.ExecuteNonQuery();
            dbcmda.CommandText = "CREATE TABLE IF NOT EXISTS 'board' ('IDhexa'	INTEGER UNIQUE,'type'  INTEGER,'bonusCenterX'  INTEGER,'bonusCenterY'  INTEGER,'bonusColor' INTEGER,PRIMARY KEY('IDhexa'))";
            dbcmda.ExecuteNonQuery();
            dbcmda.CommandText = "end";
            dbcmda.ExecuteNonQuery();
            dbconna.Close();
        }
        string conn = "URI=file:" + Application.streamingAssetsPath + "/Save/saveGame.db"; //Path to database.

        IDbConnection dbconn = (IDbConnection)new SqliteConnection(conn);

        IDbCommand dbcmd = dbconn.CreateCommand();
        dbconn.Open(); //Open connection to the database.


        dbcmd.CommandText = "begin";
        dbcmd.ExecuteNonQuery();
        dbcmd.CommandText = "DELETE FROM game";
        dbcmd.ExecuteNonQuery();
        dbcmd.CommandText = "DELETE FROM board";
        dbcmd.ExecuteNonQuery();
        dbcmd.CommandText = "DELETE FROM characters";
        dbcmd.ExecuteNonQuery();
        dbcmd.CommandText = "DELETE FROM game";
        dbcmd.ExecuteNonQuery();
        dbcmd.CommandText = "DELETE FROM board";
        dbcmd.ExecuteNonQuery();
        dbcmd.CommandText = "INSERT INTO game (player1, player2, map, w, h, current, NBchar) VALUES (" + (byte)startGameData.player1Type + ", " + (byte)startGameData.player2Type + ", " + (byte)startGameData.mapChosen + ", " + hexaGrid.w + ", " + hexaGrid.h + ", " + currentCharControlledID + ", " + hexaGrid.charList.Count + ")";
        dbcmd.ExecuteNonQuery();

        foreach (Character c in hexaGrid.charList)
        {
            dbcmd.CommandText = "INSERT INTO characters (class, team, x, y, pa, hp, skillA, directionF, totalDamage, priority) VALUES (" + (byte)c.charClass + ", " + (byte)c.team + ", " + c.x + ", " + c.y + ", " + (byte)c.PA + ", " + (byte)c.HP + ", " + (byte)(c.skillAvailable ? 1 : 0) + ", " + (byte)c.directionFacing + ", " + (byte)c.totalDamage + ", " + (byte)c.priority + ")";
            dbcmd.ExecuteNonQuery();
        }

        if (startGameData.mapChosen == 1)
        { // If we chose a random map, write it.
            int k = 0;
            for (int j = 0; j < hexaGrid.h; j++)
            {
                for (int i = 0; i < hexaGrid.w; i++)
                {
                    dbcmd.CommandText = "INSERT INTO board (IDhexa, type) VALUES (" + k + ", " + (byte)(hexaGrid.hexaList[k].type) + ")";
                    dbcmd.ExecuteNonQuery();
                    k++;
                }
            }
        }

        dbcmd.CommandText = "INSERT INTO board (bonusCenterX, bonusCenterY, bonusColor) VALUES (" + (byte)caseBonus.getx() + ", " + (byte)caseBonus.gety() + ", " + (byte)bonusTeam + ")";
        dbcmd.ExecuteNonQuery();


        dbcmd.CommandText = "end";
        dbcmd.ExecuteNonQuery();
        dbconn.Close();
        dbconn = null;

    }

    //Give the damage effect
    //Author : Rayane TALEB, L3C2
    void useAttackDamageCase(Character c)
    {
        GameObject dmgValueDisp = GameObject.Instantiate(damageValueDisplay);
        if (currentCharControlled.dmgbuff == true)
        {
            dmgValueDisp.GetComponent<DamageValueDisplay>().camera_ = cameraPos;
            dmgValueDisp.GetComponent<DamageValueDisplay>().setValue(c.x, c.y, "-" + (attackUsedAttack.effectValue + 1), Color.red, 60);
        }
        else
        {
            dmgValueDisp.GetComponent<DamageValueDisplay>().camera_ = cameraPos;
            dmgValueDisp.GetComponent<DamageValueDisplay>().setValue(c.x, c.y, "-" + attackUsedAttack.effectValue, Color.red, 60);
        }
        /*if (whoControlsThisChar(c) == PlayerType.AI_HARD) statsGame.addToDamageTaken(c, attackUsedAttack.effectValue);
        if (whoControlsThisChar(currentCharControlled) == PlayerType.AI_HARD) statsGame.addToDamageDealt(currentCharControlled, attackUsedAttack.effectValue);*/

        if (currentCharControlled.dmgbuff == true)
        {
            //Debug.Log("Action Type = " + actionType);
            // Check if VOLEUR steal HP Edited by Rayane TALEB L3C2
            if (currentCharControlled.charClass == CharClass.VOLEUR && actionType == ActionType.SKILL1) //edited by rayane
            {
                GameObject dmgValueDisp1 = GameObject.Instantiate(damageValueDisplay);
                dmgValueDisp1.GetComponent<DamageValueDisplay>().camera_ = cameraPos;
                dmgValueDisp1.GetComponent<DamageValueDisplay>().setValue(c.x, c.y - 1, "+ 1 HP", Color.green, 55);
                currentCharControlled.HP += 1;
            }

            // Check if GUERRIER steal PA Edited by Rayane TALEB L3C2
            if (currentCharControlled.charClass == CharClass.GUERRIER && actionType == ActionType.SKILL1) //edited by rayane
            {
                GameObject dmgValueDisp1 = GameObject.Instantiate(damageValueDisplay);
                dmgValueDisp1.GetComponent<DamageValueDisplay>().camera_ = cameraPos;
                dmgValueDisp1.GetComponent<DamageValueDisplay>().setValue(c.x, c.y - 1, "+ 1 PA", Color.blue, 55);
                currentCharControlled.PA += 1;
            }
            currentCharControlled.totalDamage += (attackUsedAttack.effectValue + 1);
            currentCharControlled.totalDamage2 += (attackUsedAttack.effectValue + 1);// edited by L3C2 Rayane TALEB
            c.HP -= (attackUsedAttack.effectValue + 1);
        }
        else
        {
            if (currentCharControlled.charClass == CharClass.VOLEUR && actionType == ActionType.SKILL1) // edited by L3C2 Rayane TALEB
            {
                GameObject dmgValueDisp1 = GameObject.Instantiate(damageValueDisplay);
                dmgValueDisp1.GetComponent<DamageValueDisplay>().camera_ = cameraPos;
                dmgValueDisp1.GetComponent<DamageValueDisplay>().setValue(c.x, c.y - 1, "+ 1 HP", Color.green, 55);
                currentCharControlled.HP += 1;
            }
            if (currentCharControlled.charClass == CharClass.GUERRIER && actionType == ActionType.SKILL1) // edited by L3C2 Rayane TALEB
            {
                GameObject dmgValueDisp1 = GameObject.Instantiate(damageValueDisplay);
                dmgValueDisp1.GetComponent<DamageValueDisplay>().camera_ = cameraPos;
                dmgValueDisp1.GetComponent<DamageValueDisplay>().setValue(c.x, c.y - 1, "+ 1 PA", Color.blue, 55);
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
        GameObject dmgValueDisp = GameObject.Instantiate(damageValueDisplay);
        dmgValueDisp.GetComponent<DamageValueDisplay>().camera_ = cameraPos;
        int heal = attackUsedAttack.effectValue;
        if (heal > c.HPmax - c.HP) heal = c.HPmax - c.HP;
        dmgValueDisp.GetComponent<DamageValueDisplay>().setValue(c.x, c.y, "+" + heal, Color.green, 60);

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
            // Create object that shows pa buff
            GameObject dmgValueDisp = GameObject.Instantiate(damageValueDisplay);
            dmgValueDisp.GetComponent<DamageValueDisplay>().camera_ = cameraPos;
            dmgValueDisp.GetComponent<DamageValueDisplay>().setValue(c.x, c.y, "+" + attackUsedAttack.effectValue + " PA", Color.blue, 60);

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
            // Create object that shows pm buff
            GameObject dmgValueDisp = GameObject.Instantiate(damageValueDisplay);
            dmgValueDisp.GetComponent<DamageValueDisplay>().camera_ = cameraPos;
            dmgValueDisp.GetComponent<DamageValueDisplay>().setValue(c.x, c.y, "+ " + attackUsedAttack.effectValue + " PM", Color.green, 60);
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
            GameObject dmgValueDisp = GameObject.Instantiate(damageValueDisplay);
            dmgValueDisp.GetComponent<DamageValueDisplay>().camera_ = cameraPos;
            dmgValueDisp.GetComponent<DamageValueDisplay>().setValue(c.x, c.y, "+" + attackUsedAttack.effectValue, Color.yellow, 60);
        }
    }

    //Give the stun effect
    //Author : Rayane TALEB, L3C2

    void useAttackStunCase(Character c)
    {
        GameObject dmgValueDisp = GameObject.Instantiate(damageValueDisplay);
        dmgValueDisp.GetComponent<DamageValueDisplay>().camera_ = cameraPos;
        dmgValueDisp.GetComponent<DamageValueDisplay>().setValue(c.x, c.y, "PARALYSATION", Color.red, 60);
        currentCharControlled.totalDamage += attackUsedAttack.effectValue*2;
        currentCharControlled.totalDamage2 += attackUsedAttack.effectValue*2;
        c.HP -= attackUsedAttack.effectValue;
        c.isStun = true;
    }

    //Give the poison effect
    //Author : Rayane TALEB, L3C2

    void useAttackPoisonCase(Character c)
    {
        GameObject dmgValueDisp = GameObject.Instantiate(damageValueDisplay);
        dmgValueDisp.GetComponent<DamageValueDisplay>().camera_ = cameraPos;
        dmgValueDisp.GetComponent<DamageValueDisplay>().setValue(c.x, c.y, "POISONED", Color.magenta, 60);
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
        GameObject dmgValueDisp = GameObject.Instantiate(damageValueDisplay);
        dmgValueDisp.GetComponent<DamageValueDisplay>().camera_ = cameraPos;
        dmgValueDisp.GetComponent<DamageValueDisplay>().setValue(c.x, c.y, "ROOTED", Color.gray, 60);
        c.isRooted = true;
        currentCharControlled.totalDamage += attackUsedAttack.effectValue*2;
        currentCharControlled.totalDamage2 += attackUsedAttack.effectValue*2;
        c.HP -= attackUsedAttack.effectValue;
    }


    // ##################################################################################################################################################
    // Display Functions used in main
    // ##################################################################################################################################################


    //Display the skill helper in the bottom left
    //Author : Rayane TALEB L3C2

    void displaySkillHelp()
    {
        UIAction.transform.GetChild(5).gameObject.SetActive(false);
        UIAction.transform.GetChild(6).gameObject.SetActive(false);
        if (mousePos.x >= 28 && mousePos.x < 28 + 250 && mousePos.y >= Screen.height - 540 && mousePos.y < Screen.height - 540 + 65)
        {
            skillHelp = CharsDB.list[(int)currentCharControlled.charClass].basicAttack;
            switch (currentCharControlled.charClass)
            {
                case CharClass.GUERRIER:
                    {
                        UIAction.transform.GetChild(5).gameObject.SetActive(true);
                        UIAction.transform.GetChild(6).gameObject.SetActive(true);
                        UIAction.transform.GetChild(5).transform.GetChild(0).GetComponent<Text>().text = "ATK : " + skillHelp.effectValue;
                        UIAction.transform.GetChild(6).transform.GetChild(0).GetComponent<Text>().text = "L'attaque basique du guerrier";
                        break;
                    }
                case CharClass.VOLEUR:
                    {
                        UIAction.transform.GetChild(5).gameObject.SetActive(true);
                        UIAction.transform.GetChild(6).gameObject.SetActive(true);
                        UIAction.transform.GetChild(5).transform.GetChild(0).GetComponent<Text>().text = "ATK : " + skillHelp.effectValue;
                        UIAction.transform.GetChild(6).transform.GetChild(0).GetComponent<Text>().text = "L'attaque basique du voleur";
                        break;
                    }
                case CharClass.ARCHER:
                    {
                        UIAction.transform.GetChild(5).gameObject.SetActive(true);
                        UIAction.transform.GetChild(6).gameObject.SetActive(true);
                        UIAction.transform.GetChild(5).transform.GetChild(0).GetComponent<Text>().text = "ATK : " + skillHelp.effectValue;
                        UIAction.transform.GetChild(6).transform.GetChild(0).GetComponent<Text>().text = "L'attaque basique de l'archer";
                        break;
                    }
                case CharClass.MAGE:
                    {
                        UIAction.transform.GetChild(5).gameObject.SetActive(true);
                        UIAction.transform.GetChild(6).gameObject.SetActive(true);
                        UIAction.transform.GetChild(5).transform.GetChild(0).GetComponent<Text>().text = "ATK : " + skillHelp.effectValue;
                        UIAction.transform.GetChild(6).transform.GetChild(0).GetComponent<Text>().text = "L'attaque basique du mage";
                        break;
                    }
                case CharClass.SOIGNEUR:
                    {
                        UIAction.transform.GetChild(5).gameObject.SetActive(true);
                        UIAction.transform.GetChild(6).gameObject.SetActive(true);
                        UIAction.transform.GetChild(5).transform.GetChild(0).GetComponent<Text>().text = "RANGE :  " + skillHelp.range;
                        UIAction.transform.GetChild(6).transform.GetChild(0).GetComponent<Text>().text = "Le soigneur heal de "+skillHelp.effectValue+" hp";
                        break;
                    }
                case CharClass.ENVOUTEUR:
                    {
                        UIAction.transform.GetChild(5).gameObject.SetActive(true);
                        UIAction.transform.GetChild(6).gameObject.SetActive(true);
                        UIAction.transform.GetChild(5).transform.GetChild(0).GetComponent<Text>().text = "BUFF DE : " + skillHelp.effectValue;
                        UIAction.transform.GetChild(6).transform.GetChild(0).GetComponent<Text>().text = "L'envouteur augmente les dégats";
                        break;

                    }
                case CharClass.VALKYRIE:
                    {
                        UIAction.transform.GetChild(5).gameObject.SetActive(true);
                        UIAction.transform.GetChild(6).gameObject.SetActive(true);
                        UIAction.transform.GetChild(5).transform.GetChild(0).GetComponent<Text>().text = "ATK : " + skillHelp.effectValue;
                        UIAction.transform.GetChild(6).transform.GetChild(0).GetComponent<Text>().text = "L'attaque basique de la valkyrie";
                        break;
                    }
                case CharClass.DRUIDE:
                    {
                        UIAction.transform.GetChild(5).gameObject.SetActive(true);
                        UIAction.transform.GetChild(6).gameObject.SetActive(true);
                        UIAction.transform.GetChild(5).transform.GetChild(0).GetComponent<Text>().text = "ATK : " + skillHelp.effectValue;
                        UIAction.transform.GetChild(6).transform.GetChild(0).GetComponent<Text>().text = "L'attaque basique du druide";
                        break;
                    }
            }
        }
        else if (mousePos.x >= 28 && mousePos.x < 28 + 250 && mousePos.y >= Screen.height - 610 && mousePos.y < Screen.height - 610 + 65)
        {
            skillHelp = CharsDB.list[(int)currentCharControlled.charClass].skill_1;
            switch (currentCharControlled.charClass)
            {
                case CharClass.GUERRIER:
                    {
                        UIAction.transform.GetChild(5).gameObject.SetActive(true);
                        UIAction.transform.GetChild(6).gameObject.SetActive(true);
                        UIAction.transform.GetChild(5).transform.GetChild(0).GetComponent<Text>().text = "ATK : " + skillHelp.effectValue;
                        UIAction.transform.GetChild(6).transform.GetChild(0).GetComponent<Text>().text = "Le guerrier donne un coup d'épée à son adversaire";
                        break;
                    }
                case CharClass.VOLEUR:
                    {
                        UIAction.transform.GetChild(5).gameObject.SetActive(true);
                        UIAction.transform.GetChild(6).gameObject.SetActive(true);
                        UIAction.transform.GetChild(5).transform.GetChild(0).GetComponent<Text>().text = "ATK : " + skillHelp.effectValue;
                        UIAction.transform.GetChild(6).transform.GetChild(0).GetComponent<Text>().text = "Le voleur inflige des dégats avec son couteau";
                        break;
                    }
                case CharClass.ARCHER:
                    {
                        UIAction.transform.GetChild(5).gameObject.SetActive(true);
                        UIAction.transform.GetChild(6).gameObject.SetActive(true);
                        UIAction.transform.GetChild(5).transform.GetChild(0).GetComponent<Text>().text = "ATK : " + skillHelp.effectValue;
                        UIAction.transform.GetChild(6).transform.GetChild(0).GetComponent<Text>().text = "L'archer touche sa cible en plein coeur";
                        break;
                    }
                case CharClass.MAGE:
                    {
                        UIAction.transform.GetChild(5).gameObject.SetActive(true);
                        UIAction.transform.GetChild(6).gameObject.SetActive(true);
                        UIAction.transform.GetChild(5).transform.GetChild(0).GetComponent<Text>().text = "ATK : " + skillHelp.effectValue;
                        UIAction.transform.GetChild(6).transform.GetChild(0).GetComponent<Text>().text = "Le mage lance un sort à son adversaire";
                        break;
                    }
                case CharClass.SOIGNEUR:
                    {
                        UIAction.transform.GetChild(5).gameObject.SetActive(true);
                        UIAction.transform.GetChild(6).gameObject.SetActive(true);
                        UIAction.transform.GetChild(5).transform.GetChild(0).GetComponent<Text>().text = "RANGE :  " + skillHelp.range;
                        UIAction.transform.GetChild(6).transform.GetChild(0).GetComponent<Text>().text = "Le soigneur heal de " + skillHelp.effectValue + " hp dans un rayon de 1 case";
                        break;
                    }
                case CharClass.ENVOUTEUR:
                    {
                        UIAction.transform.GetChild(5).gameObject.SetActive(true);
                        UIAction.transform.GetChild(6).gameObject.SetActive(true);
                        UIAction.transform.GetChild(5).transform.GetChild(0).GetComponent<Text>().text = "BUFF DE : " + skillHelp.effectValue;
                        UIAction.transform.GetChild(6).transform.GetChild(0).GetComponent<Text>().text = "L'envouteur donne un bonus de PA";
                        break;

                    }
                case CharClass.VALKYRIE:
                    {
                        UIAction.transform.GetChild(5).gameObject.SetActive(true);
                        UIAction.transform.GetChild(6).gameObject.SetActive(true);
                        UIAction.transform.GetChild(5).transform.GetChild(0).GetComponent<Text>().text = "ATK : " + skillHelp.effectValue;
                        UIAction.transform.GetChild(6).transform.GetChild(0).GetComponent<Text>().text = "La valkyrie attaque l'ennemi avec sa hache";
                        break;
                    }
                case CharClass.DRUIDE:
                    {
                        UIAction.transform.GetChild(5).gameObject.SetActive(true);
                        UIAction.transform.GetChild(6).gameObject.SetActive(true);
                        UIAction.transform.GetChild(5).transform.GetChild(0).GetComponent<Text>().text = "PA ennemi réduit à 0 ";
                        UIAction.transform.GetChild(6).transform.GetChild(0).GetComponent<Text>().text = "Le druide étourdit l'ennemi au prochain tour";
                        break;
                    }
            }
        }
        else if (mousePos.x >= 28 && mousePos.x < 28 + 250 && mousePos.y >= Screen.height - 680 && mousePos.y < Screen.height - 680 + 65)
        {
            skillHelp = CharsDB.list[(int)currentCharControlled.charClass].skill_2;
            switch (currentCharControlled.charClass)
            {
                case CharClass.GUERRIER:
                    {
                        UIAction.transform.GetChild(5).gameObject.SetActive(true);
                        UIAction.transform.GetChild(6).gameObject.SetActive(true);
                        UIAction.transform.GetChild(5).transform.GetChild(0).GetComponent<Text>().text = "ATK : " + skillHelp.effectValue;
                        UIAction.transform.GetChild(6).transform.GetChild(0).GetComponent<Text>().text = "Le guerrier gagne 1 PA avec son attaque";
                        break;
                    }
                case CharClass.VOLEUR:
                    {
                        UIAction.transform.GetChild(5).gameObject.SetActive(true);
                        UIAction.transform.GetChild(6).gameObject.SetActive(true);
                        UIAction.transform.GetChild(5).transform.GetChild(0).GetComponent<Text>().text = "ATK : " + skillHelp.effectValue;
                        UIAction.transform.GetChild(6).transform.GetChild(0).GetComponent<Text>().text = "Le voleur se guérit d'un HP";
                        break;
                    }
                case CharClass.ARCHER:
                    {
                        UIAction.transform.GetChild(5).gameObject.SetActive(true);
                        UIAction.transform.GetChild(6).gameObject.SetActive(true);
                        UIAction.transform.GetChild(5).transform.GetChild(0).GetComponent<Text>().text = "ATK : " + skillHelp.effectValue;
                        UIAction.transform.GetChild(6).transform.GetChild(0).GetComponent<Text>().text = "L'archer tire un déluge de flèches sur les adversaires";
                        break;
                    }
                case CharClass.MAGE:
                    {
                        UIAction.transform.GetChild(5).gameObject.SetActive(true);
                        UIAction.transform.GetChild(6).gameObject.SetActive(true);
                        UIAction.transform.GetChild(5).transform.GetChild(0).GetComponent<Text>().text = "- " + skillHelp.effectValue + " HP PAR TOUR";
                        UIAction.transform.GetChild(6).transform.GetChild(0).GetComponent<Text>().text = "Le mage empoisonne un ennemi pendant 3 tours";
                        break;
                    }
                case CharClass.SOIGNEUR:
                    {
                        UIAction.transform.GetChild(5).gameObject.SetActive(true);
                        UIAction.transform.GetChild(6).gameObject.SetActive(true);
                        UIAction.transform.GetChild(5).transform.GetChild(0).GetComponent<Text>().text = "RANGE :  " + skillHelp.range;
                        UIAction.transform.GetChild(6).transform.GetChild(0).GetComponent<Text>().text = "Le soigneur heal un allié d'au maximum " + skillHelp.effectValue + " hp";
                        break;
                    }
                case CharClass.ENVOUTEUR:
                    {
                        UIAction.transform.GetChild(5).gameObject.SetActive(true);
                        UIAction.transform.GetChild(6).gameObject.SetActive(true);
                        UIAction.transform.GetChild(5).transform.GetChild(0).GetComponent<Text>().text = "BUFF DE : " + skillHelp.effectValue;
                        UIAction.transform.GetChild(6).transform.GetChild(0).GetComponent<Text>().text = "L'envouteur donne un bonus de PM";
                        break;
                    }
                case CharClass.VALKYRIE:
                    {
                        UIAction.transform.GetChild(5).gameObject.SetActive(true);
                        UIAction.transform.GetChild(6).gameObject.SetActive(true);
                        UIAction.transform.GetChild(5).transform.GetChild(0).GetComponent<Text>().text = "ATK : " + skillHelp.effectValue;
                        UIAction.transform.GetChild(6).transform.GetChild(0).GetComponent<Text>().text = "La valkyrie lance sa lance sur un adversaire";
                        break;
                    }
                case CharClass.DRUIDE:
                    {
                        UIAction.transform.GetChild(5).gameObject.SetActive(true);
                        UIAction.transform.GetChild(6).gameObject.SetActive(true);
                        UIAction.transform.GetChild(5).transform.GetChild(0).GetComponent<Text>().text = "PM ennemi réduit à 0 ";
                        UIAction.transform.GetChild(6).transform.GetChild(0).GetComponent<Text>().text = "Le druide immobilise l'ennemi pour le tour suivant";
                        break;
                    }
            }
        }
    }

    //Display the right character in the top left character card
    //Author : ?
    void displayNewCharTurnList()
    {
        //GameObject go = UICharTurnsList[0];
        UIEnemyChar.SetActive(false);

        UICurrentChar.transform.GetChild(3).GetComponent<Text>().text = currentCharControlled.PA + "";
        UICurrentChar.transform.GetChild(6).GetComponent<Text>().text = currentCharControlled.HP + "/" + currentCharControlled.HPmax;
        UICurrentChar.transform.GetChild(1).GetComponent<RawImage>().texture = charCards[(int)currentCharControlled.charClass];
        UICurrentChar.transform.GetChild(0).GetComponent<Image>().color = (currentCharControlled.team == 0) ? Character.TEAM_1_COLOR : Character.TEAM_2_COLOR;
        if (charHovered != null && charHovered.team == currentCharControlled.team)
        {
            UICurrentChar.transform.GetChild(0).GetComponent<Image>().color = new Color(1, 1, 1);
            UICurrentChar.transform.GetChild(3).GetComponent<Text>().text = charHovered.PA + "";
            UICurrentChar.transform.GetChild(6).GetComponent<Text>().text = charHovered.HP + "/" + charHovered.HPmax;
            UICurrentChar.transform.GetChild(1).GetComponent<RawImage>().texture = charCards[(int)charHovered.charClass];
        }
        else if (charHovered != null && charHovered.team != currentCharControlled.team)
        {
            UIEnemyChar.SetActive(true);
            UIEnemyChar.transform.GetChild(0).GetComponent<Image>().color = new Color(1, 1, 1);
            UIEnemyChar.transform.GetChild(3).GetComponent<Text>().text = charHovered.PA + "";
            UIEnemyChar.transform.GetChild(6).GetComponent<Text>().text = charHovered.HP + "/" + charHovered.HPmax;
            UIEnemyChar.transform.GetChild(1).GetComponent<RawImage>().texture = charCards[(int)charHovered.charClass];
        }
    }

    //Display Initiative at the bottom on the screen
    //Author : VALAT Thibault, L3Q1
    void displayInitiative()
    {
        Initiative.transform.GetChild(hexaGrid.charList.Count).transform.position = new Vector3(10000, 10000, 0);
        //Display every Initiative case
        for (int i = 0; i < hexaGrid.charList.Count; i++)
            if (hexaGrid.charList[i].HP > 0)
                displayOneInitiativeCase(i, hexaGrid.charList[i]);
    }

    //Display on Initiative case
    //Author : VALAT Thibault, L3Q1
    void displayOneInitiativeCase(int i, Character c)
    {
        Initiative.transform.GetChild(i).transform.GetChild(1).GetComponent<RawImage>().texture = charSquares[(int)c.charClass];
        Initiative.transform.GetChild(i).transform.GetChild(0).GetComponent<Image>().color = (c.team == 0) ? Character.TEAM_1_COLOR : Character.TEAM_2_COLOR;
    }

    //Display the action buttons on the top left
    //Author : ?
    // edited by Rayane TALEB L3C2
    void displayActionButtons()
    {
        for (int i = 0; i < 4; i++)
        {
            if (((int)actionType) == i) UIAction.transform.GetChild(i).GetChild(0).gameObject.GetComponent<Text>().fontStyle = FontStyle.Bold;
            else UIAction.transform.GetChild(i).GetChild(0).gameObject.GetComponent<Text>().fontStyle = FontStyle.Normal;
        }
        if (currentCharControlled.skillAvailable == false)
        {
            UIAction.transform.GetChild(2).transform.GetChild(0).GetComponent<Text>().text = currentCharControlled.totalDamage + " / 10";
        }
        else
        {
            UIAction.transform.GetChild(2).transform.GetChild(0).GetComponent<Text>().text = "Compétence (C)";
        }
        if (currentCharControlled.skill2Available == false)
        {
            UIAction.transform.GetChild(3).transform.GetChild(0).GetComponent<Text>().text = currentCharControlled.totalDamage2 + " / 14";// edited by L3C2 Rayane TALEB
        }
        else
        {
            UIAction.transform.GetChild(3).transform.GetChild(0).GetComponent<Text>().text = "Compétence 2 (B)";// edited by L3C2 Rayane TALEB
        }
    }

    //Display the current hovered hexa
    //Author : ?
    void displayHoveredHexa()
    {
        if (hexaHovered != null) hexaHovered.hoveredColor();
        if (hexaHoveredOld != null) hexaHoveredOld.defaultColor();
    }

    //Display the possibles hexas to go to
    //Author : ?
    void displayPossiblePaths()
    {
        List<Point> path = hexaGrid.findAllPaths(currentCharControlled.x, currentCharControlled.y, currentCharControlled.PM);
        foreach (Point p in path)
        {
            GameObject go = GameObject.Instantiate(hexaTemplate, hexasFolder.transform);
            go.SetActive(true);
            go.GetComponent<Transform>().position = Hexa.hexaPosToReal(p.x, p.y, -0.015f);
            go.GetComponent<MeshFilter>().mesh = hexaFilledMesh;
            go.GetComponent<Renderer>().sharedMaterial = allPathsDisplayMat;
            go.GetComponent<Collider>().enabled = false;
            pathFinderDisplay.Add(go);
        }
    }

    //Display the shortest path
    //Author : ?
    void displaySortestPath()
    {
        // Display path (create the green hexas)
        if (hexaHovered != null && (hexaHovered.type == HexaType.GROUND || hexaHovered.type == HexaType.BONUS))
        {
            List<Point> path = hexaGrid.findShortestPath(currentCharControlled.x, currentCharControlled.y, hexaHovered.x, hexaHovered.y, currentCharControlled.PM);
            if (path != null)
            {
                path.RemoveAt(0);
                foreach (Point p in path)
                {
                    GameObject go = GameObject.Instantiate(hexaTemplate, hexasFolder.transform);
                    go.SetActive(true);
                    go.GetComponent<Transform>().position = Hexa.hexaPosToReal(p.x, p.y, -0.014f);
                    go.GetComponent<MeshFilter>().mesh = hexaFilledMesh;
                    go.GetComponent<Renderer>().sharedMaterial = pathDisplayMat;
                    go.GetComponent<Collider>().enabled = false;
                    pathFinderDisplay.Add(go);
                }
            }
        }
    }

    //Display the sighted hexas
    //Author : ?
    //Edited by L3C2 Rayane TALEB 
    void displayLineOfSight()
    {
        List<Point> hexasBlocked = null;
        List<Point> pointList = hexaGrid.findHexasInSight(currentCharControlled.x, currentCharControlled.y, (actionType == ActionType.ATK1) ? (currentCharControlled.getClassData().basicAttack.range) : ((actionType == ActionType.SKILL1) ? (currentCharControlled.getClassData().skill_1.range) : (currentCharControlled.getClassData().skill_2.range)), out hexasBlocked, currentCharControlled);// edited by L3C2 Rayane TALEB

        bool hexaHoveredTargetable = false;
        // Display line of sight (Blue hexas)
        foreach (Point p in pointList)
        {
            if (hexaHovered != null && p.x == hexaHovered.x && p.y == hexaHovered.y) hexaHoveredTargetable = true;
            GameObject go = GameObject.Instantiate(hexaTemplate, hexasFolder.transform);
            go.SetActive(true);
            go.GetComponent<Transform>().position = Hexa.hexaPosToReal(p.x, p.y, -0.015f);
            go.GetComponent<MeshFilter>().mesh = hexaFilledMesh;
            go.GetComponent<Renderer>().sharedMaterial = lineOfSightMat;
            go.GetComponent<Collider>().enabled = false;
            lineOfSightDisplay.Add(go);
        }
        // Display blocked hexas (transparent blue hexas)
        foreach (Point p in hexasBlocked)
        {
            GameObject go = GameObject.Instantiate(hexaTemplate, hexasFolder.transform);
            go.SetActive(true);
            go.GetComponent<Transform>().position = Hexa.hexaPosToReal(p.x, p.y, -0.015f);
            go.GetComponent<MeshFilter>().mesh = hexaFilledMesh;
            go.GetComponent<Renderer>().sharedMaterial = blockedSightMat;
            go.GetComponent<Collider>().enabled = false;
            lineOfSightDisplay.Add(go);
        }
        if (hexaHoveredTargetable)
        {
            List<Point> hexaPos = hexaGrid.getHexasWithinRange(hexaHovered.x, hexaHovered.y, (actionType == ActionType.ATK1) ? (currentCharControlled.getClassData().basicAttack.rangeAoE) : ((actionType == ActionType.SKILL1) ? (currentCharControlled.getClassData().skill_1.rangeAoE) : (currentCharControlled.getClassData().skill_2.rangeAoE)));// edited by L3C2 Rayane TALEB
            // Display AoE (red hexas)
            foreach (Point p in hexaPos)
            {
                GameObject go = GameObject.Instantiate(hexaTemplate, hexasFolder.transform);
                go.SetActive(true);
                go.GetComponent<Transform>().position = Hexa.hexaPosToReal(p.x, p.y, -0.014f);
                go.GetComponent<MeshFilter>().mesh = hexaFilledMesh;
                go.GetComponent<Renderer>().sharedMaterial = aoeMat;
                go.GetComponent<Collider>().enabled = false;
                lineOfSightDisplay.Add(go);
            }
        }
    }


}



