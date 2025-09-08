using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Pun.UtilityScripts;
using Photon.Realtime;
using Photon.Voice.PUN.UtilityScripts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Reflection;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;


public enum GameState
{
    RiskAnalysis,
    PlanningPoker,
    Procurement,
    RiskManagement,
    CardReading,
    Premiation
}
public class GameMechanics : MonoBehaviourPunCallbacks, IPunTurnManagerCallbacks, IOnEventCallback
{
    private readonly byte ChooseIndexEvent = 198;
    //private readonly byte KeepAlive = 255;
    private readonly byte spawnCardFromTempRiskDeckEvent = 197;
    private readonly byte showPairOfCardsEvent = 196;
    private readonly byte spawnCardFromBoardEvent = 195;
    private readonly byte printPlayerTurnEvent = 194;
    private readonly byte incrementPlayerTurnCompletitionEvent = 193;
    private readonly byte updatePointsForEachTeamEvent = 192;
    private readonly byte waitForSecondsEvent = 191;
    private readonly byte throwFirstDiceEvent = 190;
    private readonly byte throwSecondDiceEvent = 189;
    private readonly byte showCardFromTempRiskDeckEvent = 188;
    private readonly byte spawnCardEvent = 187;
    private readonly byte terminateGameEvent = 184;
    private readonly byte SetPremiationState = 171;
    private readonly byte setFirstSprintEvent = 179;
    private readonly byte setSecondSprintEvent = 178;
    private readonly byte setThirdSprintEvent = 177;
    private readonly byte resourcesSubmitted = 181;
    private readonly byte throwTheDice = 173;
    private readonly byte updateBoardEvent = 169;
    private readonly byte playerLeftEvent = 166;
    private readonly byte foundPlayerToStart = 164;
    private int lastThrow = default;
    private int lastIndex = default;
    public static GameState gameState = GameState.RiskAnalysis;
    private PunTurnManager turnManager = default;
    private List<IEventCard> eventCards = new List<IEventCard>();
    private List<RiskCard> riskDeck = new List<RiskCard>();
    private List<RiskCard> temp_riskDeck = new List<RiskCard>();
    private Dictionary<byte, List<RiskCard>> Board = new Dictionary<byte, List<RiskCard>>();
    private DiceBehaviour diceBehaviour;
    private int[] probabilities = new int[6];
    private int[] impacts = new int[3];
    public static int sprintNumber = 0;
    private GameObject actualCard = null;
    private GameObject actualEventCard = null;
    public static int playersTurnCompletition = 0; //quanti utenti hanno completato il turno
    private int card_index;
    public static event Action OnMyPlayerLeftTheRoom, resetTeamVariables, OnProcurementStateSet, OnGoingToLeave, OnGameFinished, OnRiskManagementStateSetCloseUGS, LeaveTheRoom, OnTerminetingGameClearUGS, showUGSThenClear;
    public static event Action<int,int,int> OnRiskManagementStateSet, OnGameStartSetUpTokens;
    public static event Action<int> OnFirstThrow, OnDestroyGameObject;
    public static event Action<byte, string, int> OnRiskMitigationOnSprint/*, OnRiskMitigationOnSprint2, OnRiskMitigationOnSprint3*/;
    public static event Action<int, string> OnLeave;
    public static byte completedUserGameSheets = 0;
    public static bool areCardsSelectable = false;
    public static bool isLocalPlayerTurn = false;
    public static bool isGameStarted = false;
    public GameObject[] decks_in_scene;
    public float distance = 0f;
    public float height = 0f;
    public static int[] pointsForEachTeam = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
    public byte[] lastVote = new byte[2];
    private GameObject pairOfCards;
    public static bool isMyTurnToTalk = false;
    private byte objectingVotes = 0;
    private int numberOfVotes = 0;
    private int numberOfNonTeamMates = 0;
    public static bool isStartingPlayer = false;
    public static List<int> ActorNumbers = new List<int>();
    public GameObject premiazione;
    public static int nextPlayer = 0;
    public static event Action autoFillUserGameSheet;
    private bool hasPlayersTurnCompletitionIncreased = false;
    //public GameObject premiBackslash;
    public GameObject smokeParticle;
    //public GameObject explosionParticle;
    public GameObject cardControls;
    private float TurnRiskManagementTimer = 0f; //determina dopo quanto tempo mostrare la scritta "Premere la freccetta destra ..."
    public static bool finishedReading = false;
    private int numberOfActorsThatFinishedReading = 0;
    private int eventsCount = 0;
    private bool hasmodified = false;
    private bool isRiskCardInstantiated = false;
    private bool mostraVotoRifiutato = false;
    private bool hasThrown = false;
    public GameObject premibackslash;
    private int nextCardTransformIndex = 0;
    private bool HasUpdatedUGS = false;
    private bool enteredCardReading = false;
    private bool secondDiceThrown = false;
    public GameObject fase;
    private bool hasVoted = true;
    private bool timer = false; //is timer started
    private float time = 61f; //la durata del timer
    private bool leftShiftPressed = false;
    private TextChat textChat;
    private int PlayersMaxCount = 0; //It's the number of players at the beginning of the match. It's used to change the nextPlayerIndex 
    private Player lastPlayer = null;
    private Player lastPlayerToThrowDice = null;
    private Player lastMasterClient = null;
    private bool HasnumberOfActorsThatFinishedReadingIcreased = false;
    private bool isFirstTurnInRiskManagement = true; //determina se è il primo turno durante la fase di Risk Management
    private bool isRankingCleared = true;
    private void setUpGame()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        TeamSelection.OnTeamsReady += StartGame;
        UserGameSheet.OnUGScompleted += setUserGameSheetCompleted;
        CardController.onCardChoosen += chooseCards;
        PlayerController.stopTalking += FinishTalking;
        PlayerController.finishReading += finishReading;
        subscribeToEveryEventCard();
        PlayerController.throws += throwFirstDice;
        TeamSelection.OnSelectTeamInsertInList += addPlayerToActorNumbers;
        TeamSelection.OnExitTeamRemoveFromList += removePlayerFromActorNumbers;
        ConnectToServer.OnLeftTheRoom += terminateGame;
        this.turnManager = this.gameObject.AddComponent<PunTurnManager>();
        this.turnManager.TurnManagerListener = this;
        this.turnManager.TurnDuration = float.MaxValue;
        isFirstTurnInRiskManagement = true;
        premiazione.SetActive(false);
    }

    private void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
        setUpGame();
        textChat = GameObject.Find("TextChat").GetComponent<TextChat>();
    }

    private void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {

    }

    public int countNonTeamMates()
    {
        int result = 0;
        for (int i = 0; i <= 12; i++)
        {
            if (TeamSelection.Teams[i].Count >= 1)
            {
                result++;
            }
        }
        return result;
    }

    public void choosePlayerToStart()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.LogError("Inizio io");
        }
    }

    public void addPlayerToActorNumbers(int actorNumber)
    {
        Debug.LogError("InseriscoIlgiocatore");
        if (!ActorNumbers.Contains(actorNumber))
        {
            ActorNumbers.Add(actorNumber);
        }
        Debug.LogError("actor added: "+ActorNumbers[ActorNumbers.IndexOf(actorNumber)]);
    }

    public void removePlayerFromActorNumbers(int actorNumber)
    {
        Debug.LogError("RimuovoIlgiocatore");
        if (ActorNumbers.Contains(actorNumber))
        {
            ActorNumbers.Remove(actorNumber);
        }
    }

    public void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    public void StartGame()
    {
        this.turnManager = this.gameObject.AddComponent<PunTurnManager>();
        this.turnManager.TurnManagerListener = this;
        this.turnManager.TurnDuration = float.MaxValue;
        lastMasterClient = PhotonNetwork.CurrentRoom.GetPlayer(PhotonNetwork.MasterClient.ActorNumber);
        lastPlayerToThrowDice = PhotonNetwork.CurrentRoom.GetPlayer(PhotonNetwork.MasterClient.ActorNumber);
        fase.SetActive(true);
        if (PhotonNetwork.IsMasterClient)
        {
            choosePlayerToStart();
        }
        populateEventDeck();
        populateBoard();
        populateRiskDeck();
        for (int m = 0; m <= 8; m++)
        {
            if (!decks_in_scene[0].transform.GetChild(m).gameObject.activeSelf)
            {
                decks_in_scene[0].transform.GetChild(m).gameObject.SetActive(true);
            }
        }
        OnGameStartSetUpTokens?.Invoke(3, 3, 4);
        numberOfNonTeamMates = this.countNonTeamMates();
        Debug.LogError("Non teammates: " + numberOfNonTeamMates);
        if (this.turnManager.Turn == 0)
        {
            //nextPlayer = (nextPlayer % PlayersMaxCount) + 1;
            lastPlayer = PhotonNetwork.MasterClient;
            lastPlayerToThrowDice = PhotonNetwork.MasterClient;
            if (PhotonNetwork.IsMasterClient)
            {
                chooseIndex(UnityEngine.Random.Range(0, riskDeck.Count));
            }
        }
        isGameStarted = true;
        PhotonNetwork.CurrentRoom.IsOpen = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (this.timer == true)
        {
            if (time > 0f)
            {
                time -= Time.deltaTime;
            }
            else
            {
                this.timer = false;
                FinishTalking();
            }
        }
        if (Input.GetKey(KeyCode.LeftShift) && !textChat.isSelected && PhotonNetwork.CurrentRoom != null && gameState >= GameState.RiskManagement)
        {
            showRanking();
        }
        else if (!Input.GetKey(KeyCode.LeftShift))
        {
            hideRanking();
        }
        if (actualEventCard != null)
        {
            actualEventCard.transform.rotation = GameObject.FindGameObjectWithTag("MainCamera").transform.rotation;
        }
        if (actualCard != null)
        {
            actualCard.transform.rotation = GameObject.FindGameObjectWithTag("MainCamera").transform.rotation;
        }
        if (pairOfCards != null)
        {
            pairOfCards.transform.LookAt(GameObject.FindGameObjectWithTag("MainCamera").transform);
        }
        if (gameState.Equals(GameState.RiskAnalysis) && isGameStarted)
        {
            cardControls.SetActive(true);
            GameObject.Find("Fase (testo)").GetComponent<TMP_Text>().text = "Risk Analysis";
        }
        else if (gameState.Equals(GameState.Procurement) && isGameStarted)
        {
            GameObject.Find("Fase (testo)").GetComponent<TMP_Text>().text = "Procurement";
            if (UserGameSheet.compilationTerminated)
            {
                GameObject.Find("Fase (sottotesto)").GetComponent<TMP_Text>().text = "Attendi che gli altri utenti compilino lo User Game Sheet...";
            }
            else
            {
                GameObject.Find("Fase (sottotesto)").GetComponent<TMP_Text>().text = "Preparati a compilare lo User Game Sheet!";
            }
        }
        else if (gameState.Equals(GameState.RiskManagement) && isGameStarted)
        {
            GameObject.Find("Fase (testo)").GetComponent<TMP_Text>().text = "Risk Management";
        }
        else if (gameState.Equals(GameState.Premiation) && isGameStarted)
        {
            GameObject.Find("Fase (testo)").GetComponent<TMP_Text>().text = "Finito!";
            GameObject.Find("Fase (sottotesto)").GetComponent<TMP_Text>().text = "";
        }
        else if (gameState.Equals(GameState.PlanningPoker) && isGameStarted) 
        {
            cardControls.SetActive(false);
            if (objectingVotes == 1)
            {
                GameObject.Find("Fase (testo)").GetComponent<TMP_Text>().text = "Votazione rifiutata!";
            }
            else
            {
                GameObject.Find("Fase (testo)").GetComponent<TMP_Text>().text = "Discuss!";
            }
            if (isMyTurnToTalk)
            {
                string timeToShow;
                if (time > 60f)
                {
                    timeToShow = "60";
                }
                else
                {
                    timeToShow = Mathf.CeilToInt(time).ToString();
                }
                GameObject.Find("Fase (sottotesto)").GetComponent<TMP_Text>().text = "Hai "+ timeToShow + " secondi per spiegare il razionale dietro la scelta delle tue carte.";
            }
        }
        else if (gameState.Equals(GameState.CardReading) && isGameStarted && !finishedReading)
        {
            GameObject.Find("Fase (testo)").GetComponent<TMP_Text>().text = "Card Reading";
            GameObject.Find("Fase (sottotesto)").GetComponent<TMP_Text>().text = "Cosa c'è scritto sulla carta?";
        }
        else if (gameState.Equals(GameState.CardReading) && isGameStarted && finishedReading)
        {
            GameObject.Find("Fase (testo)").GetComponent<TMP_Text>().text = "Card Reading";
            GameObject.Find("Fase (sottotesto)").GetComponent<TMP_Text>().text = "Attendi che gli altri utenti terminino la lettura...";
        }
        if (isGameStarted) 
        {
            if (riskDeck.Count != 0)
            {
                decks_in_scene[0].SetActive(true);
            }
            else if (riskDeck.Count == 0)
            {
                decks_in_scene[0].SetActive(false);
            }
            if (Board[0].Count != 0)
            {
                decks_in_scene[1].SetActive(true);
                for (int k = 0; k <= Board[0].Count - 1; k++)
                {
                    decks_in_scene[1].gameObject.transform.GetChild(k).gameObject.SetActive(true);
                }
            }
            else if (Board[0].Count == 0)
            {
                decks_in_scene[1].SetActive(false);
            }
            if (Board[1].Count != 0)
            {
                decks_in_scene[2].SetActive(true);
                for (int k = 0; k <= Board[1].Count - 1; k++)
                {
                    decks_in_scene[2].gameObject.transform.GetChild(k).gameObject.SetActive(true);
                }
            }
            else if (Board[1].Count == 0)
            {
                decks_in_scene[2].SetActive(false);
            }
            if (Board[2].Count != 0)
            {
                decks_in_scene[3].SetActive(true);
                for (int k = 0; k <= Board[2].Count - 1; k++)
                {
                    decks_in_scene[3].gameObject.transform.GetChild(k).gameObject.SetActive(true);
                }
            }
            else if (Board[2].Count == 0)
            {   
                decks_in_scene[3].SetActive(false);
            }
            if (eventCards.Count != 0)
            {
                decks_in_scene[4].SetActive(true);
            }
        }
        if (!isGameStarted)
        {
            fase.SetActive(false);
        }
        if (isLocalPlayerTurn && gameState.Equals(GameState.RiskManagement))
        {
            TurnRiskManagementTimer += Time.deltaTime;
            if (TurnRiskManagementTimer >= 2f && TurnRiskManagementTimer < 4f)
            {
                GameObject.Find("Fase (sottotesto)").GetComponent<TMP_Text>().text = "Premi la freccia destra per lanciare il dado";
            }
            else if (TurnRiskManagementTimer >= 4f)
            {
                GameObject.Find("Fase (sottotesto)").GetComponent<TMP_Text>().text = "È il tuo turno";
                TurnRiskManagementTimer = 0f;
            }
        }
    }
    void IPunTurnManagerCallbacks.OnTurnBegins(int turn)
    {
        if (gameState.Equals(GameState.RiskManagement) || gameState.Equals(GameState.CardReading))
        {
            if (lastPlayerToThrowDice.Equals(PhotonNetwork.LocalPlayer))
            {
                this.BeginMyTurn();
            }
        }
        else 
        {
            if (PhotonNetwork.IsMasterClient)
            {
                this.BeginMyTurn();
            }
        }
    }

    //Player moved and did not finish the move
    void IPunTurnManagerCallbacks.OnPlayerMove(Photon.Realtime.Player player, int turn, object move)
    {
        lastPlayer = player;
        isFirstTurnInRiskManagement = false;
        isLocalPlayerTurn = false;
        HasnumberOfActorsThatFinishedReadingIcreased = false;
        //TMP_Text g = GameObject.Find("INFO").GetComponent<TMP_Text>();
        //g.text = "Result is : " + actual_move[0];
        if (move != null) 
        {
            if ((gameState.Equals(GameState.RiskManagement)))
            {
                int[] actual_move = (int[])move;
                Debug.LogError("actual_move[2]: " + actual_move[2]);
                if (actual_move[2] == 1 && !secondDiceThrown) // carta rischio 
                {
                    secondDiceThrown = true;
                    Debug.LogError("nextPlayer:  " + nextPlayer);
                    if (PhotonNetwork.LocalPlayer.ActorNumber == player.ActorNumber)
                    {
                        throwSecondDice();
                        return;
                    }
                    return;
                }
                else
                {
                    secondDiceThrown = false;
                    Debug.LogError("È la fase di Risk Management");
                    Debug.LogError("ma ora passo alla fase CardReading");
                    gameState = GameState.CardReading;
                    finishedReading = false;
                    enteredCardReading = true;
                    if (actual_move[2] == 0) // carta evento
                    {
                        //g.text = "è una carta evento";
                        //this.photonView.RPC("spawnCard", RpcTarget.All, true, actual_move[1]);
                        //evento spawn card
                        Debug.LogError("È una carta evento");
                        if (PhotonNetwork.LocalPlayer.ActorNumber == player.ActorNumber)
                        {
                            byte evCode = throwTheDice;
                            int content = 2;
                            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All }; // You would have to set the Receivers to All in order to receive this event on the local client as well
                            PhotonNetwork.RaiseEvent(evCode, content, raiseEventOptions, SendOptions.SendReliable);
                        }
                        lastIndex = actual_move[1];
                        int playerTeamIndex = actual_move[3];
                        Debug.LogError("OnFirstThrow?. Dice result is: " + actual_move[0] + " actual_move[2] is: " + actual_move[2] + " last Throw is " + lastThrow + " last index is " + lastIndex);

                        if (PhotonNetwork.LocalPlayer.ActorNumber == player.ActorNumber)
                        {
                            byte evCode1 = spawnCardEvent;
                            int content1 = (int)actual_move[1];
                            RaiseEventOptions raiseEventOptions1 = new RaiseEventOptions { Receivers = ReceiverGroup.All }; // You would have to set the Receivers to All in order to receive this event on the local client as well
                            PhotonNetwork.RaiseEvent(evCode1, content1, raiseEventOptions1, SendOptions.SendReliable);
                        }
                    }
                    else if (actual_move[2] == 1) // carta rischio
                    {
                        if (!hasPlayersTurnCompletitionIncreased)
                        {
                            hasPlayersTurnCompletitionIncreased = true;
                            playersTurnCompletition++;
                            Debug.LogError("Oh No! È una carta rischio");
                        }
                        eventsCount = 0;
                        Debug.LogError("È una carta rischio");
                        if (TeamSelection.myTeamIndex == actual_move[3])
                        {
                            assignPoints(diceToPile(actual_move[0]), actual_move[1]);
                            Debug.LogError("ASSIGNED POINTS");
                        }
                        //lancia evento
                        if (PhotonNetwork.LocalPlayer.ActorNumber == player.ActorNumber)
                        {
                            byte evCode = throwTheDice;
                            int content = 1;
                            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All }; // You would have to set the Receivers to All in order to receive this event on the local client as well
                            PhotonNetwork.RaiseEvent(evCode, content, raiseEventOptions, SendOptions.SendReliable);
                        }
                        lastIndex = actual_move[1];
                        int playerTeamIndex = actual_move[3];
                        Debug.LogError("OnFirstThrow?. Dice result is: " + actual_move[0] + " actual_move[2] is: " + actual_move[2] + " last Throw is " + lastThrow + " last index is " + lastIndex);
                        if (PhotonNetwork.LocalPlayer.ActorNumber == player.ActorNumber)
                        {
                            byte evCode2 = spawnCardFromBoardEvent;
                            object[] content2 = new object[3];
                            content2[0] = diceToPile(actual_move[0]);
                            content2[1] = actual_move[1];
                            content2[2] = actual_move[3];
                            RaiseEventOptions raiseEventOptions2 = new RaiseEventOptions { Receivers = ReceiverGroup.All }; // You would have to set the Receivers to All in order to receive this event on the local client as well
                            PhotonNetwork.RaiseEvent(evCode2, content2, raiseEventOptions2, SendOptions.SendReliable);
                        }
                    }
                    lastPlayerToThrowDice = player;
                    Debug.LogError("lastPlayerToThrowDice set");
                    if (PhotonNetwork.IsMasterClient)
                    {
                        this.turnManager.BeginTurn();
                    }
                }
            }
        }
        else
        {
            Debug.LogError("OnPlayerMove: ricevuta mossa nulla");
            if (gameState.Equals(GameState.RiskManagement))
            {
                startNextTurn(player, false);
            }
        }
    }

    //Player moved and completed the move
    void IPunTurnManagerCallbacks.OnPlayerFinished(Photon.Realtime.Player player, int turn, object move)
    {
        lastPlayer = player;
        isLocalPlayerTurn = false;
        hasThrown = false;
        isRiskCardInstantiated = false;
        HasnumberOfActorsThatFinishedReadingIcreased = false;
        //TMP_Text g = GameObject.Find("Logger").GetComponent<TMP_Text>();
        //g.text = "Ha finito\t";
        Debug.LogError("GameState:  "+gameState+" move: "+move);
        Debug.LogError("OnPlayerFinished");
        if (move != null)
        {
            Debug.LogError("Mossa NON nulla di "+player.ActorNumber+" (actorNumber)");
            if (gameState.Equals(GameState.RiskAnalysis))
            {
                Debug.LogError("Voting...");
                try
                {
                    if (!hasVoted)
                    {
                        hasVoted = true;
                        byte[] result = move as byte[];
                        vote(result);
                    }
                }
                catch(InvalidCastException e)
                {
                    Debug.LogError(e.StackTrace);
                }
                finally
                {
                    startNextTurn(player, false);
                }
            }
            else if (gameState.Equals(GameState.RiskManagement))
            {
                Debug.LogError("OnPlayerFinished during RiskManagement keep reading");
                if (PhotonNetwork.IsMasterClient)
                {
                    this.turnManager.BeginTurn();
                }
                Debug.LogError("Started master client turn");
            }
            else if (gameState.Equals(GameState.CardReading))
            {
                numberOfActorsThatFinishedReading++;
                Debug.LogError("numberOfActorsThatFinishedReading: " + numberOfActorsThatFinishedReading);
            }
            else if (gameState.Equals(GameState.Procurement))
            {
                completedUserGameSheets++;
                Debug.LogError("Player Index " + (int)player.CustomProperties["myTeamIndex"] + "\nMy team index "+TeamSelection.myTeamIndex);
                Debug.LogError(completedUserGameSheets + " players have compleated the User Game Sheet");
                return;

            }
            else if (gameState.Equals(GameState.PlanningPoker))
            {
                GameObject.Find("Fase (sottotesto)").GetComponent<TMP_Text>().text = "Tempo scaduto!";
                startNextTurn(player, false);
            }
        }
        else
        {
            Debug.LogError("Ricevuta mossa nulla");
            //firstRoundInManagementPhase = false;
            if (gameState.Equals(GameState.RiskAnalysis))
            {
                Debug.LogError("Sono qui?");
                startNextTurn(player, false);                
            }
            else if (gameState.Equals(GameState.PlanningPoker))
            {
                startNextTurn(player, false);

            }
            else if (gameState.Equals(GameState.RiskManagement))
            {
                gameState = GameState.CardReading;
                startNextTurn(player, false);
            }
            else if (gameState.Equals(GameState.CardReading))
            {
                gameState = GameState.RiskManagement;
                startNextTurn(lastPlayerToThrowDice, false);
            }
        }
    }

    void startNextTurn(Player lastplayer, bool isItAnewTurn) //Vedere se riesce a saltare da un giocatore all'ultimo
    {
        try
        {
            lastplayer = lastplayer.GetNext();
        }
        catch (NullReferenceException)
        {
            if (gameState.Equals(GameState.RiskManagement))
            {
                lastPlayerToThrowDice = PhotonNetwork.MasterClient;
                Debug.LogError("*** lastPlayerToThrowDice is MasterClient ***");
            }
            if (PhotonNetwork.IsMasterClient)
            {
                this.turnManager.BeginTurn();
                Debug.LogError("startNextTurn: called BeginTurn()");
            }
        }
        finally
        {
            if (lastplayer == null)
            {
                Debug.LogError("Giocatore non trovato, passo al successivo");
                isLocalPlayerTurn = false; //blocca la possibilità di lanciare il dado
                areCardsSelectable = false; //blocca la possibilità di scegliere le carte a prescindere da quelle scelte dal compagno
                if (gameState.Equals(GameState.PlanningPoker))
                {
                    this.turnManager.SendMove(true, true);
                }
                else if (gameState.Equals(GameState.RiskAnalysis))
                {
                    Debug.LogError("Sending ad empty move");
                    this.turnManager.SendMove(null, true);
                    Debug.LogError("Empty move sent");
                }
                else if (gameState.Equals(GameState.RiskManagement))
                {
                    this.turnManager.SendMove(null, false);
                    Debug.LogError(PhotonNetwork.LocalPlayer.ActorNumber + " sent an empty move");
                }
                else if (gameState.Equals(GameState.Procurement))
                {
                    if (TeamSelection.Teams[TeamSelection.myTeamIndex].Count <= 1)
                    {
                        this.turnManager.SendMove(new int[] { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 }, true);
                    }
                }
                else if (gameState.Equals(GameState.CardReading))
                {
                    this.turnManager.SendMove((string)"finishedReading", true);
                }
            }
            else
            {
                if (gameState.Equals(GameState.RiskManagement))
                {
                    lastPlayerToThrowDice = lastplayer;
                    Debug.LogError("*** lastPlayerToThrowDice " + lastPlayerToThrowDice + " ***");
                }
                if (lastplayer.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
                {
                    if (!isItAnewTurn)
                    {
                        this.BeginMyTurn();
                        Debug.LogError("startNextTurn: started my turn");
                    }
                    else if (isItAnewTurn)
                    {
                        this.turnManager.BeginTurn();
                        Debug.LogError("startNextTurn: called BeginTurn()");
                    }
                }
            }
        }
    }



    //Every player endend their turn
    void IPunTurnManagerCallbacks.OnTurnCompleted(int turn)
    {
        HasnumberOfActorsThatFinishedReadingIcreased = false;
        Debug.LogError("Ogni giocatore ha finito");
        if (gameState.Equals(GameState.RiskAnalysis))
        {
            Debug.LogError("Probabilities :" + probabilities[0] + ", " + probabilities[1] + ", " + probabilities[2] + ", " + probabilities[3] + ", " + probabilities[4] + ", " + probabilities[5]);
            Debug.LogError("Impacts :" + impacts[0] + ", " + impacts[1] + ", " + impacts[2]);

            /*Aggiungo la carta al temp riskDeck*/
            Debug.LogError("Adding to riskdeck...");
            temp_riskDeck.Add(riskDeck[card_index]);
            byte fp = findProbability();
            byte fi = findImpact();
            if (fp == 255 || fi == 255)
            {
                Debug.LogError("Starting vote objection...");               
                objectingVotes = 1; //entro nella fase di "discussione delle carte con rifiuto"
                areCardsSelectable = false;
                Debug.LogError("Imposto la fase di planning poker");
                gameState = GameState.PlanningPoker;
                probabilities = new int[6];
                impacts = new int[3];
                if (PhotonNetwork.IsMasterClient)
                {
                    this.turnManager.BeginTurn();
                }
            }
            else
            {
                probabilities = new int[6];
                impacts = new int[3];
                riskDeck[card_index].probability = fp;
                riskDeck[card_index].impact = fi;
                insertInBoard(riskDeck[card_index].probability, riskDeck[card_index]);
                if (actualCard != null) GameObject.Destroy(actualCard);
                actualCard = null;
                Debug.LogError("I'll remove at " + riskDeck[card_index].id);
                riskDeck.RemoveAt(card_index);
                resetEveryCard(); //ripristina le carte impatto e rischio che sono state disabilitate nell'ultima "ri-votazione"
                areCardsSelectable = false;
                if (objectingVotes == 2) //Si è già stati nello stato di planning poker nel turno precedente
                {
                    Debug.LogError("Si è già stati nello stato di planning poker, passo a RiskAnalysis");
                    gameState = GameState.RiskAnalysis;
                    objectingVotes = 0;
                    if (PhotonNetwork.IsMasterClient && riskDeck.Count != 0) chooseIndex(UnityEngine.Random.Range(0, riskDeck.Count));
                    else if (riskDeck.Count == 0 && PhotonNetwork.IsMasterClient)
                    {
                        this.turnManager.BeginTurn();
                    }
                }
                else
                {
                    Debug.LogError("Imposto la fase di planning poker, in assenza di rigetto del voto");
                    gameState = GameState.PlanningPoker;
                    if (PhotonNetwork.IsMasterClient)
                    {
                        this.turnManager.BeginTurn();
                    }
                }              
                if (riskDeck.Count == 0)
                {
                    areCardsSelectable = false;
                    gameState = GameState.Procurement;
                    StartCoroutine(waitThenStartTurn(0.9f, gameState));
                }
            }
        }
        else if (gameState.Equals(GameState.PlanningPoker) && objectingVotes != 2)
        {
            GameObject.Destroy(pairOfCards);
            gameState = GameState.RiskAnalysis;
            if (objectingVotes == 1)
            {
                obiettaVotazione();
            }
            else
            {
                if (PhotonNetwork.IsMasterClient && riskDeck.Count != 0) chooseIndex(UnityEngine.Random.Range(0, riskDeck.Count));
                else if (PhotonNetwork.IsMasterClient) 
                {
                    this.turnManager.BeginTurn();
                }
            }
        }
        else if (gameState.Equals(GameState.Procurement))
        {
            completedUserGameSheets++;
            if (completedUserGameSheets >= numberOfNonTeamMates)
            {
                Debug.LogError("This is completedUserGameSheets >= numberOfNonTeamMates");
                gameState = GameState.RiskManagement;
                OnRiskManagementStateSetCloseUGS?.Invoke();
                GameObject.Find("Fase (sottotesto)").GetComponent<TMP_Text>().text = "";
                OnRiskManagementStateSet?.Invoke(2, 1, 1);
                lastPlayer = PhotonNetwork.MasterClient;
                if (PhotonNetwork.IsMasterClient)
                {
                    this.turnManager.BeginTurn();
                }
            }
        }
        else if (gameState.Equals(GameState.CardReading))
        {
            HasUpdatedUGS = false;
            numberOfActorsThatFinishedReading++;
            if (playersTurnCompletition >= 9 * numberOfNonTeamMates)
            {
                gameState = GameState.Premiation;
                Debug.LogError("Entering Premiation phase");
                if (PhotonNetwork.IsMasterClient)
                {
                    this.BeginMyTurn();
                }
            }
            else if (numberOfActorsThatFinishedReading >= numberOfNonTeamMates)
            {
                numberOfActorsThatFinishedReading = 0;
                gameState = GameState.RiskManagement;
                if (PhotonNetwork.CurrentRoom.PlayerCount > 1)
                {
                    startNextTurn(lastPlayerToThrowDice, true);
                    Debug.LogError("lastplayertothrowdice " + lastPlayerToThrowDice.NickName);
                }
                else
                {
                    this.turnManager.BeginTurn();
                }
            }
        }
    }
    void populateBoard()
    {
        Debug.LogError("Populating board...");
        Board.Add(0, new List<RiskCard>());
        Board.Add(1, new List<RiskCard>());
        Board.Add(2, new List<RiskCard>());
        Board.Add(3, new List<RiskCard>());
    }
    void insertInBoard(byte probability, RiskCard r)
    {
        Debug.LogError("Inserting in board\nprobability"+probability+"\nthe card "+r);
        float height = 0.143f;
        if (gameState > GameState.Procurement)
        {
            smokeParticle.GetComponent<ParticleSystem>().startDelay = 16f;
        }
        else
        {
            smokeParticle.GetComponent<ParticleSystem>().startDelay = 0f;
        }
        switch (probability)
        {
            case 0:
                Board[0].Add(r);
                if (gameState > GameState.Procurement)
                {
                    smokeParticle.transform.position = new Vector3(-0.017f, height, -7.425f);
                    smokeParticle.GetComponent<ParticleSystem>().Play();
                }
                break;
            case 20:
                Board[0].Add(r);
                if (gameState > GameState.Procurement)
                {
                    smokeParticle.transform.position = new Vector3(-0.017f, height, -7.425f);
                    smokeParticle.GetComponent<ParticleSystem>().Play();
                }
                break;
            case 40:
                Board[1].Add(r);
                if (gameState > GameState.Procurement)
                {
                    smokeParticle.transform.position = new Vector3(-0.017f, height, -7.425f);
                    smokeParticle.GetComponent<ParticleSystem>().Play();
                }
                break;
            case 60:
                Board[1].Add(r);
                if (gameState > GameState.Procurement)
                {
                    smokeParticle.transform.position = new Vector3(-0.017f, height, -7.425f);
                    smokeParticle.GetComponent<ParticleSystem>().Play();
                }
                break;
            case 80:
                Board[2].Add(r);
                if (gameState > GameState.Procurement)
                {
                    smokeParticle.transform.position = new Vector3(-0.017f, height, -7.425f);
                    smokeParticle.GetComponent<ParticleSystem>().Play();
                }
                break;
            case 100:
                Board[2].Add(r);
                if (gameState > GameState.Procurement)
                {
                    smokeParticle.transform.position = new Vector3(-0.017f, height, -7.425f);
                    smokeParticle.GetComponent<ParticleSystem>().Play();
                }
                break;
        }
    }

    void IPunTurnManagerCallbacks.OnTurnTimeEnds(int turn)
    {
        startNextTurn(lastPlayer, true);
    }    
    void FinishTalking()
    {
        isMyTurnToTalk = false;
        timer = false;
        turnManager.SendMove(true, true);
    }
    void raisePlayerTurnEvent()
    {
        if (gameState.Equals(GameState.RiskManagement) && lastPlayerToThrowDice.Equals(PhotonNetwork.LocalPlayer))
        {
            string nickname = PhotonNetwork.LocalPlayer.NickName;
            byte evCode = printPlayerTurnEvent;
            string content = nickname;
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
            PhotonNetwork.RaiseEvent(evCode, content, raiseEventOptions, SendOptions.SendReliable);
        }
        else
        {
            string nickname = PhotonNetwork.LocalPlayer.NickName;
            byte evCode = printPlayerTurnEvent;
            string content = nickname;
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
            PhotonNetwork.RaiseEvent(evCode, content, raiseEventOptions, SendOptions.SendReliable);
        }

    }
    void printMyTurn()
    {
        if (fase.activeSelf)
        { 
            if (gameState.Equals(GameState.RiskAnalysis))
            {
                hasVoted = false;
            }
            GameObject.Find("Fase (sottotesto)").GetComponent<TMP_Text>().text = "È il tuo turno";
        }
    }
    void BeginMyTurn()
    {
        Debug.LogError("turn started");
        if (!ActorNumbers.Contains(PhotonNetwork.LocalPlayer.ActorNumber) || TeamSelection.isTeamMate) //Se è un compagno di squadra 
        {
            Debug.LogError("Il giocatore di turno non è presente tra gli ActorNumbers");
            isLocalPlayerTurn = false; //blocca la possibilità di lanciare il dado
            areCardsSelectable = false; //blocca la possibilità di scegliere le carte a prescindere da quelle scelte dal compagno
            if (gameState.Equals(GameState.PlanningPoker))
            {
                this.turnManager.SendMove(true, true);
            }
            else if (gameState.Equals(GameState.RiskAnalysis))
            {
                Debug.LogError("Sending ad empty move");
                this.turnManager.SendMove(null, true);
                Debug.LogError("Empty move sent");
            }
            else if (gameState.Equals(GameState.RiskManagement))
            {
                this.turnManager.SendMove(null, false);
                Debug.LogError(PhotonNetwork.LocalPlayer.ActorNumber + " sent an empty move");
            }            
        }
        else
        {
            if (riskDeck.Count != 0 && gameState.Equals(GameState.RiskAnalysis))
            {
                areCardsSelectable = true;
                raisePlayerTurnEvent();
                printMyTurn();
            }
            else if (gameState.Equals(GameState.PlanningPoker))
            {
                isMyTurnToTalk = true;
                raisePlayerTurnEvent();
                printMyTurn();
                StartTimer(61f); //Time to wait 60
                byte evCode = showPairOfCardsEvent;
                byte[] bcontent = lastVote;
                Debug.LogError("Raising showPairOfCardsEvent...");
                RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All }; // You would have to set the Receivers to All in order to receive this event on the local client as well
                PhotonNetwork.RaiseEvent(evCode, bcontent, raiseEventOptions, SendOptions.SendReliable);
            }
            else if (gameState.Equals(GameState.RiskManagement))
            {
                if (sprintNumber >= 9)
                {
                    this.turnManager.SendMove(null, false); //passa il turno
                }
                else
                {
                    HasUpdatedUGS = false;
                    isLocalPlayerTurn = true;
                    raisePlayerTurnEvent();
                    printMyTurn();
                }
            }
            else if (gameState.Equals(GameState.CardReading))
            {
                finishedReading = false;
                Debug.LogError("finishedReading = " + finishedReading);
            }
        }
        if (gameState.Equals(GameState.Premiation))
        {
            isLocalPlayerTurn = false; //blocca la possibilità di lanciare il dado
            areCardsSelectable = false; //blocca la possibilità di scegliere le carte a prescindere da quelle scelte dal compagno
        }
    }

    void showRanking()
    {
        premiazione.SetActive(true);
        rankTeams();
    }

    void hideRanking()
    {
        premiazione.SetActive(false);
        clearRanking();
    }
    void StartTimer(float total_time)
    {
        this.time = total_time; 
        this.timer = true;
    }

    IEnumerator waitThenStartTurn(float seconds, GameState phase)
    {
        if (phase.Equals(GameState.CardReading))
        {
            Debug.LogError("Waiting then I'll start the turn");
            GameObject.Find("Fase (sottotesto)").GetComponent<TMP_Text>().text = "Cosa c'è scritto sulla carta?";
            yield return new WaitForSeconds(0);
            if (PhotonNetwork.IsMasterClient)
            {
                this.turnManager.BeginTurn();
            }
        }
        else if (phase.Equals(GameState.Procurement))
        {
            yield return new WaitForSeconds(seconds);
            Debug.LogError("Invoking OnProcurementStateSet");
            OnProcurementStateSet?.Invoke();
            if (PhotonNetwork.IsMasterClient)
            {
                this.turnManager.BeginTurn();
            }
        }
    }

    void finishReading()
    {
        finishedReading = true;
        this.turnManager.SendMove((string)"finishedReading", true);
        Debug.LogError("Finished Reading Move sent");
    }

    void showPairOfCards(byte[] lastVote)
    {
        if (actualCard != null)
        {
            GameObject.Destroy(actualCard);
        }
        if (pairOfCards != null)
        {
            GameObject.Destroy(pairOfCards);
            pairOfCards = null;
        }
        Quaternion rotation = new Quaternion(0, PlayerController.playerCam.rotation.y, 0, 0);
        pairOfCards = Instantiate(Resources.Load<GameObject>("Cards"), new Vector3(0.148000002f, 1.201f, -6.89499998f), rotation);
        pairOfCards.transform.localScale = new Vector3(2f, 2f, 2f);
        //mostro l'impatto
        pairOfCards.transform.GetChild(0).gameObject.transform.GetChild(0).gameObject.GetComponent<Renderer>().materials[1].mainTexture = Resources.Load<Texture>("Images/Impact/" + lastVote[1].ToString());
        //mostro la probabilità
        pairOfCards.transform.GetChild(1).gameObject.transform.GetChild(0).gameObject.GetComponent<Renderer>().materials[1].mainTexture = Resources.Load<Texture>("Images/Percentage/" + lastVote[0].ToString());
    }
    void populateEventDeck()
    {
        Debug.LogError("Populating EventDeck...");
        eventCards.Add(new Satisfied());
        eventCards.Add(new HighFocusFactor());
        eventCards.Add(new ScopeCreep());
        eventCards.Add(new Hostility());
        eventCards.Add(new LowFocusFactor());
        eventCards.Add(new NewReleaseDate());
        eventCards.Add(new NewTechnologies());
        eventCards.Add(new CustomerVeryPresent());
        eventCards.Add(new UnexpectedProblemsWithReources());
        eventCards.Add(new AvidCompetitors());
        eventCards.Add(new UnclearCustomer());
        eventCards.Add(new AbsentCustomer());
        eventCards.Add(new FundingCut());
        eventCards.Add(new HighComplexity());
        Debug.LogError("Card deck count " + eventCards.Count);
    }


    void spawnCard(bool IsEventCard, int index)
    {
        if (actualEventCard != null)
        {
            GameObject.Destroy(actualEventCard);
            actualEventCard = null;
        }
        if (actualCard != null)
        {
            GameObject.Destroy(actualCard);
            actualCard = null;
        }
        if (pairOfCards != null)
        {
            GameObject.Destroy(pairOfCards);
            pairOfCards = null;
        }
        if (IsEventCard)
        {
            hasPlayersTurnCompletitionIncreased = false;
            //Il nome delle carte nella lista è lo stesso dei prefab!
            Quaternion rotation = new Quaternion(0, PlayerController.playerCam.rotation.y, 0, 0);
            Debug.LogError("Spawning "+ eventCards[index].Name + " ... ");
            actualEventCard = Instantiate(Resources.Load<GameObject>(eventCards[index].Name), new Vector3(0.148000002f,1.201f,-6.89499998f) /*new Vector3(0.163000003f, -0.465999991f, -6.70599985f)*/, rotation);
        if (!hasmodified)
            {
                hasmodified = true;
                eventCards[index].modify(Board);
            }
        }
        else if (!IsEventCard)
        {
            Quaternion rotation = new Quaternion(0, PlayerController.playerCam.rotation.y, 0, 0);
            Debug.LogError("Instantiating risk card...");
            actualCard = GameObject.Instantiate(Resources.Load<GameObject>(riskDeck[index].id), new Vector3(0.148000002f,1.201f,-6.89499998f), rotation);
            decks_in_scene[0].transform.GetChild(nextCardTransformIndex).gameObject.SetActive(false);
            nextCardTransformIndex++;
        }
    }
    //[PunRPC]
    void spawnCardFromTempRiskDeck(int index)
    {
        if (actualEventCard != null)
        {
            GameObject.Destroy(actualEventCard);
            actualEventCard = null;       
        }
        if (actualCard != null)
        { 
            GameObject.Destroy(actualCard);
            actualCard = null;
        }
        if (pairOfCards != null)
        {
            GameObject.Destroy(pairOfCards);
            pairOfCards = null;
        }
        //if (explosionParticle.activeSelf) explosionParticle.GetComponent<ParticleSystem>().Play();
        Quaternion rotation = new Quaternion(0, PlayerController.playerCam.rotation.y, 0, 0);
        actualCard = GameObject.Instantiate(Resources.Load<GameObject>(temp_riskDeck[index].id), new Vector3(0.148000002f,1.201f,-6.89499998f), rotation);
    }

    //[PunRPC]
    void spawnCardFromBoard(byte pile, int index)
    {

        hasPlayersTurnCompletitionIncreased = false;
        if (actualEventCard != null)
        {
            GameObject.Destroy(actualEventCard);       
            actualEventCard = null;
        }
        if (actualCard != null)
        {
            GameObject.Destroy(actualCard);
            actualCard = null;
        }
        if (actualCard == null)
        {
            Quaternion rotation = new Quaternion(0, PlayerController.playerCam.rotation.y, 0, 0);
            GameObject mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            actualCard = GameObject.Instantiate(Resources.Load<GameObject>(Board[pile][index % Board[pile].Count].id), new Vector3(0.148000002f,1.201f,-6.89499998f) /*new Vector3(0.163000003f, -0.465999991f, -6.70599985f)*/, rotation);
            //Mostra il prevention plan
            actualCard.transform.GetChild(1).gameObject.SetActive(false);
            //Mostra la probabilità sulla carta
            actualCard.transform.GetChild(0).gameObject.transform.GetChild(1).GetComponent<TMP_Text>().text = Board[pile][index].probability.ToString() + "%";
            //Mostra l'impatto sulla carta
            switch (Board[pile][index % Board[pile].Count].impact)
            {
                case 0:
                    actualCard.transform.GetChild(0).gameObject.transform.GetChild(0).GetComponent<TMP_Text>().text = "Low";
                    break;
                case 1:
                    actualCard.transform.GetChild(0).gameObject.transform.GetChild(0).GetComponent<TMP_Text>().text = "Medium";
                    break;
                case 2:
                    actualCard.transform.GetChild(0).gameObject.transform.GetChild(0).GetComponent<TMP_Text>().text = "High";
                    break;
            }
        }
    }
    void assignPoints(byte pile, int index)
    {
        if (!HasUpdatedUGS)
        {
            HasUpdatedUGS = true;
            int points = verifyRiskContingency(Board[pile][index % Board[pile].Count]);
            sprintNumber++;
            Debug.LogError("mysprints: " + sprintNumber);
            if (sprintNumber >= 1 && sprintNumber <= 9)
            {
                OnRiskMitigationOnSprint?.Invoke(Board[pile][index % Board[pile].Count].impact, Board[pile][index % Board[pile].Count].id, points);
            }
            if (!TeamSelection.isTeamMate)
            {
                Debug.LogError("Updating poits for each team");
                byte evCode = updatePointsForEachTeamEvent;
                int[] content = new int[2];
                content[0] = TeamSelection.myTeamIndex;
                content[1] = points;
                RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All }; // You would have to set the Receivers to All in order to receive this event on the local client as well
                PhotonNetwork.RaiseEvent(evCode, content, raiseEventOptions, SendOptions.SendReliable);
                Debug.LogError("Event raised");
            }
        }
    }

    void updatePointsForEachTeam(int teamIndex, int points)
    {
        pointsForEachTeam[teamIndex] += points;
    }

    int whichTeamWins()
    {
        if (!checkPareggio())
        {
            return Array.IndexOf(pointsForEachTeam, (int)pointsForEachTeam.Max());
        }
        return -1;
    }
    //vede se ogni squadra ha lo stesso punteggio
    bool checkPareggio()
    {
        bool result = false;
        for (int i = 0; i <= pointsForEachTeam.Length - 1; i++)
        {
            for (int j = 0; j <= pointsForEachTeam.Length - 1; j++)
            {
                if (i != j)
                {
                    if (pointsForEachTeam[i] != pointsForEachTeam[j])
                        return false;
                    else if (pointsForEachTeam[i] == pointsForEachTeam[j]) 
                        result = true;
                }
            }
        }
        return result;
    }
    //vede se la mia squadra ha pareggiato con un'altra
    bool checkPareggio(int teamIndex)
    {
        bool result = false;
        for (int i = 0; i <= pointsForEachTeam.Length - 1; i++)
        {
            if (i != teamIndex)
            {
                if (pointsForEachTeam[i] != pointsForEachTeam[teamIndex]) result = false;
                else if (pointsForEachTeam[i] == pointsForEachTeam[teamIndex]) return true;
            }
        }
        return result;
    }

    void rankTeams()
    {
        isRankingCleared = false;
        int[] ranking = new int[12];
        int[] points_copy = new int[pointsForEachTeam.Length];
        pointsForEachTeam.CopyTo(points_copy, 0);
        premiazione.SetActive(true);
        int[] pointsInOrder = new int[12];
        int posizione = 1;
        int lastPosizione = posizione;
        for (int j = 0; j <= ranking.Length - 1; j++)
        {
            ranking[j] = Array.IndexOf(points_copy, (int)points_copy.Max());
            pointsInOrder[j] = points_copy[ranking[j]];
            lastPosizione = posizione;
            if (j != 0)
            {
                if (points_copy[ranking[j]] != pointsInOrder[j-1])
                {
                    posizione++;
                }
                else
                {
                    posizione = lastPosizione;
                }
            }
            points_copy[Array.IndexOf(points_copy, (int)points_copy.Max())] = -1;
            if (TeamSelection.Teams[ranking[j]].Count == 2)
            {
                premiazione.transform.GetChild(2).gameObject.transform.GetChild(j).gameObject.GetComponent<TMP_Text>().text = posizione.ToString();
                premiazione.transform.GetChild(1).gameObject.transform.GetChild(j).gameObject.GetComponent<TMP_Text>().text = TeamSelection.printTeam(ranking[j], TeamSelection.Teams)[0]
                                                                                                                        + ", " + TeamSelection.printTeam(ranking[j], TeamSelection.Teams)[1]
                                                                                                                        + ", SCORE:" + pointsForEachTeam[ranking[j]];
            }
            if (TeamSelection.Teams[ranking[j]].Count == 1)
            {
                premiazione.transform.GetChild(2).gameObject.transform.GetChild(j).gameObject.GetComponent<TMP_Text>().text = posizione.ToString();
                if (TeamSelection.printTeam(ranking[j], TeamSelection.Teams)[0] == null || TeamSelection.printTeam(ranking[j], TeamSelection.Teams)[0].Equals(""))
                {
                    premiazione.transform.GetChild(1).gameObject.transform.GetChild(j).gameObject.GetComponent<TMP_Text>().text = TeamSelection.printTeam(ranking[j], TeamSelection.Teams)[1]
                                                                                                        + ", SCORE:" + pointsForEachTeam[ranking[j]];
                }
                else if (TeamSelection.printTeam(ranking[j], TeamSelection.Teams)[1] == null || TeamSelection.printTeam(ranking[j], TeamSelection.Teams)[1].Equals(""))
                {
                    premiazione.transform.GetChild(1).gameObject.transform.GetChild(j).gameObject.GetComponent<TMP_Text>().text = TeamSelection.printTeam(ranking[j], TeamSelection.Teams)[0]
                                                                                                        + ", SCORE:" + pointsForEachTeam[ranking[j]];
                }
            }
        }
    }

    void clearRanking()
    {
        if (!isRankingCleared)
        {
            isRankingCleared = true;
            for (int j = 0; j <= 11; j++)
            {
                premiazione.transform.GetChild(1).gameObject.transform.GetChild(j).gameObject.GetComponent<TMP_Text>().text = "";
                premiazione.transform.GetChild(2).gameObject.transform.GetChild(j).gameObject.GetComponent<TMP_Text>().text = "";
            }

        }
    }

    void populateRiskDeck()
    {
        Debug.LogError("Populating risk deck...");
        RiskCard temp = new RiskCard("ID-R1", "Delay in delivery",
            "The artifacts are not ready in time for \r\nthe end of the sprint. The reasons \r\nmay be the lack of understanding of \r\nthe tasks that need to be done or the \r\nquality of those tasks.",
            new List<byte> { 7 }, new List<byte> { 3, 2 }, new List<byte> { 0 }, 0, 0, (Texture)Resources.Load("/Images/Risks/0005.jpg"));
        riskDeck.Add(temp);
        
        temp = new RiskCard("ID-R2", "Poor participation and \r\nLack of dedication to the \r\ncommon goal",
            "One of the Team Members is not \r\nvery active during project \r\ndevelopment and has a low \r\nFocus Factor as they focus on \r\nother projects.",
            new List<byte> { 5 }, new List<byte> { 1 }, null, 0, 0, (Texture)Resources.Load("/Images/Risks/0006.jpg"));
        riskDeck.Add(temp);

        temp = new RiskCard("ID-R3", "Incorrect or Incomplete Functionality",
            "Many of the requirements and user \r\nstories are rejected by the Client. \r\nOr a sprint is completed without \r\nfulfillment of a requirement.",
            new List<byte> { 4, 8 }, new List<byte> { 7 }, new List<byte> { 1 }, 0, 0, (Texture)Resources.Load("/Images/Risks/0007.jpg"));
        riskDeck.Add(temp);

        temp = new RiskCard("ID-R4", "Poor Communication and Internal Arguments",
            "The project has delays or artifacts are \r\nconflict with each other due to a lack \r\nof communication and coordination \r\nbetween team members.",
            new List<byte> { 1 }, new List<byte> { 5 }, null, 0, 0, (Texture)Resources.Load("/Images/Risks/0008.jpg"));
        riskDeck.Add(temp);

        temp = new RiskCard("ID-R5", "Low-Quality Artifacts",
            "artifacts produced require further \r\nreview and work.",
            new List<byte> { 7, 0 }, new List<byte> { 3, 4, 6 }, null, 0, 0, (Texture)Resources.Load("/Images/Risks/0009.jpg"));
        riskDeck.Add(temp);

        temp = new RiskCard("ID-R6", "Error in cost and effort estimation for the planned activities",
            "The time and resources allocated \r\nto a task are not adequate \r\n(overestimated or underestimated).",
            new List<byte> { 7 }, new List<byte> { 0 }, null, 0, 0, (Texture)Resources.Load("/Images/Risks/0010.jpg"));
        riskDeck.Add(temp);

        temp = new RiskCard("ID-R7", "Unrealistic Expectations",
            "Features promised to the \r\ncustomer are infeasible to \r\nachieve with the set budget.",
            new List<byte> { 8 }, new List<byte> { 7 }, null, 0, 0, (Texture)Resources.Load("/Images/Risks/0011.jpg"));
        riskDeck.Add(temp);

        temp = new RiskCard("ID-R8", "Design choices negatively impact code quality",
            "The product is built with a very \r\ncomplex architecture, making it \r\ndifficult to maintain.",
            new List<byte> { 9 }, new List<byte> { 0 }, null, 0, 0, (Texture)Resources.Load("/Images/Risks/0012.jpg"));
        riskDeck.Add(temp);

        temp = new RiskCard("ID-R9", "Low Business Value",
            "The product produced turns out \r\nto have low business value and \r\ndoes not meet the expectations \r\nof top management.",
            new List<byte> { 8 }, new List<byte> { 1, 7 }, null, 0, 0, (Texture)Resources.Load("/Images/Risks/0013.jpg"));
        riskDeck.Add(temp); 
    }

    public void vote(byte[] move)
    {
        switch (move[0])
        {
            case 0:
                probabilities[0]++;
                break;
            case 20:
                probabilities[1]++;
                break;
            case 40:
                probabilities[2]++;
                break;
            case 60:
                probabilities[3]++;
                break;
            case 80:
                probabilities[4]++;
                break;
            case 100:
                probabilities[5]++;
                break;
        }
        if (move[1] == 0) impacts[0]++;
        else if (move[1] == 1) impacts[1]++;
        else impacts[2]++;
        Debug.LogError("Completed execution of vote()");
    }
    public void chooseCards()
    {
        byte[] move = { CardController.probability, CardController.impact };
        lastVote = move;
        turnManager.SendMove(move, true);
    }

    public void findProbabilityDifferentFromZero(int[] array)
    {
        //int result = 0;
        for (int i = 0; i <= array.Length-1; i++)
        {
            if (array[i] == 0)
            {
                deactivatePercentageCard(i);                
            }
            //else result+=array[i];
        }
        //Debug.LogError("findProbabilityDifferentFromZero: "+result);
        //return result;
    }
    public void findImpactDifferentFromZero(int[] array)
    {
        //int result = 0;
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i] == 0)
            {
                deactivateImpactCard(i);
            }
            //else result+=array[i];
        }
        //Debug.LogError("findImpactDifferentFromZero: " + result);
        //return result;
    }
    public byte findProbability()
    {
        byte p = 0;
        int index = -1;
        findProbabilityDifferentFromZero(probabilities);
        int n = numberOfNonTeamMates / 2;
        for (int i = 0; i <= probabilities.Length - 1; i++)
        {
            if (probabilities[i] > n)
            {
                index = i;
                break;
            }
            else
            {
                index = 255;
            }
        }
        Debug.LogError(index);
        
            switch (index)
            {
                case 0:
                    p = 0;
                    break;
                case 1:
                    p = 20;
                    break;
                case 2:
                    p = 40;
                    break;
                case 3:
                    p = 60;
                    break;
                case 4:
                    p = 80;
                    break;
                case 5:
                    p = 100;
                    break;
                case 255:
                    p = 255;
                    break;
        }
        probabilities = new int[6];
        return p;
    }

    public byte findImpact()
    {
        byte p = 0;
        int index = -1;
        findImpactDifferentFromZero(impacts);
        int n = numberOfNonTeamMates/2;
        for(int i = 0; i <= impacts.Length-1; i++)
        {
            if (impacts[i] > n)
            {
                index = i;
                break;
            }
            else
            {
                index = 255;
            }
        }
        switch (index)
            {
                case 0:
                    p = 0;
                    break;
                case 1:
                    p = 1;
                    break;
                case 2:
                    p = 2;
                    break;
                case 255:
                    p = 255;
                    break;
            }
        impacts = new int[3];
        return p;    
    }

    public void setUserGameSheetCompleted()
    {
        int[] move = UserGameSheet.resources;
        turnManager.SendMove(move, true);
    }
    
    void printBoard()
    {
        GameObject.Find("BoardHere").GetComponent<TMP_Text>().text = "";
        GameObject.Find("BoardHere").GetComponent<TMP_Text>().text += "0%-20%\n";
        foreach (var item in Board[0])
        {
            GameObject.Find("BoardHere").GetComponent<TMP_Text>().text += item.id + " ";
        }
        GameObject.Find("BoardHere").GetComponent<TMP_Text>().text += "\n40%-60%\n";
        foreach (var item in Board[1])
        {
            GameObject.Find("BoardHere").GetComponent<TMP_Text>().text += item.id + " ";
        }
        GameObject.Find("BoardHere").GetComponent<TMP_Text>().text += "\n80%-100%\n";
        foreach (var item in Board[2])
        {
            GameObject.Find("BoardHere").GetComponent<TMP_Text>().text += item.id + " ";
        }
    }

    void chooseIndex(int randomNumber)
    {   
        if (PhotonNetwork.IsMasterClient)
        {
            byte evCode = ChooseIndexEvent;
            int content = randomNumber;
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All }; // You would have to set the Receivers to All in order to receive this event on the local client as well
            PhotonNetwork.RaiseEvent(evCode, content, raiseEventOptions, SendOptions.SendReliable);
            Debug.LogError("Lanciato l'evento ChooseIndexEvent");
            this.turnManager.BeginTurn();
        }
        //GameObject.Find("EventCardSpawn (2)").GetComponent<TMP_Text>().text = "Raised event.";
    }

    public RiskCard findCardInBoard(RiskCard card)
    {
        for (byte i = 0; i < 3; i++)
        {
            for (byte j = 0; j < Board[i].Count; j++)
            {
                if (Board[i][j].Equals(card))
                {
                    return Board[i][j];
                }
            }
        }
        throw new Exception("Couldn't find RiskCard in Board");
    }
    public void switchPileInBoard(RiskCard cardToMove)
    {
        RiskCard riskCardCopy = cardToMove;
        if (Board[0].Contains(cardToMove)) Board[0].Remove(cardToMove);
        else if (Board[1].Contains(cardToMove)) Board[1].Remove(cardToMove);
        else if (Board[2].Contains(cardToMove)) Board[2].Remove(cardToMove);
        switch (riskCardCopy.probability)
        {
            case 0:
                insertInBoard(riskCardCopy.probability, riskCardCopy);
                return;
            case 20:
                insertInBoard(riskCardCopy.probability, riskCardCopy);
                return;
            case 40:
                insertInBoard(riskCardCopy.probability, riskCardCopy);
                return;
            case 60:
                insertInBoard(riskCardCopy.probability, riskCardCopy);
                return;
            case 80:
                insertInBoard(riskCardCopy.probability, riskCardCopy);
                return;
            case 100:
                insertInBoard(riskCardCopy.probability, riskCardCopy);
                return;
        }
    }

    public void subscribeToEveryEventCard()
    {
        HighComplexity.OnProbabilityAndImpactChanged += switchPileInBoard;
        FundingCut.OnProbabilityAndImpactChanged += switchPileInBoard;
        AbsentCustomer.OnProbabilityAndImpactChanged += switchPileInBoard;
        UnclearCustomer.OnProbabilityAndImpactChanged += switchPileInBoard;
        AvidCompetitors.OnProbabilityAndImpactChanged += switchPileInBoard;
        UnexpectedProblemsWithReources.OnProbabilityAndImpactChanged += switchPileInBoard;
        CustomerVeryPresent.OnProbabilityAndImpactChanged += switchPileInBoard;
        NewTechnologies.OnProbabilityAndImpactChanged += switchPileInBoard;
        NewReleaseDate.OnProbabilityAndImpactChanged += switchPileInBoard;
        LowFocusFactor.OnProbabilityAndImpactChanged += switchPileInBoard;
        Hostility.OnProbabilityAndImpactChanged += switchPileInBoard;
        ScopeCreep.OnProbabilityAndImpactChanged += switchPileInBoard;
        HighFocusFactor.OnProbabilityAndImpactChanged += switchPileInBoard;
        Satisfied.OnProbabilityAndImpactChanged += switchPileInBoard;
    }

    int pickApairValue()
    {
        float randomValue = UnityEngine.Random.Range(0f, 1f);
        if (randomValue >= 0 && randomValue < 0.33f)
        {
            return 2;
        }
        else if (randomValue >= 0.33 && randomValue < 0.66)
        {
            return 4;
        }
        else
        {
            return 6;
        }
    }
    //move[2] = 0 allora è una carta evento altrimenti
    //move[2] = 1 se è una carta rischio.
    //move[1] = indice da cui pescare la carta.
    //move[0] = risultato del lancio.
    //move[3] = team index.
    public void throwFirstDice(float dice_resulting_value)
    {
        if (actualEventCard != null)
        {
            GameObject.Destroy(actualEventCard);
        }
        int[] move = new int[4];
        move[0] = default;
        move[1] = 0;
        move[2] = -1;
        move[3] = TeamSelection.myTeamIndex;
        Debug.LogError("throwFirstDice reached");
        if (dice_resulting_value >= 0f && dice_resulting_value < 0.25f) //carta evento
        {
            move[0] = pickApairValue();
            Debug.LogError("event card condition reached, choosing index");
            int index = UnityEngine.Random.Range(0, eventCards.Count);
            move[1] = index;
            move[2] = 0; //è una carta evento
            Debug.LogError("This is throwFirstDice event cards are " + eventCards.Count);
            turnManager.SendMove(move, false);
        }
        else
        {
            Debug.LogError("second throw condition reached");
            move[2] = 1; //è una carta rischio
            turnManager.SendMove(move, false); //secondo lancio (scelgo la pila)
        }
    }

    public void throwSecondDice()
    {
        int dice_result = UnityEngine.Random.Range(1, 7); 
        int index = -1;
        bool empty = isChosenPileEmpty(dice_result);
        if (empty)
        {
            Debug.LogError("Second dice result : " + dice_result + " the pile is empty? " + empty);
            this.throwSecondDice();
            return;
        }
        else
        {
            Debug.LogError("Second dice result : " + dice_result + " the pile is empty? " + empty);
            if (dice_result == 1) //pila 0%-20%
            {
                Debug.LogError("dice_result == 1 scelgo tra " + 0 + " e " + (Board[0].Count - 1));
                index = UnityEngine.Random.Range(0, Board[0].Count - 1);
            }
            else if (dice_result >= 2 && dice_result < 4) //pila 40%-60%
            {
                Debug.Log("dice_result == 1 scelgo tra " + 0 + " e " + (Board[1].Count - 1));
                index = UnityEngine.Random.Range(0, Board[1].Count - 1);
            }
            else //pila 80%-100%
            {
                Debug.Log("dice_result == 1 scelgo tra " + 0 + " e " + (Board[2].Count - 1));
                index = UnityEngine.Random.Range(0, Board[2].Count - 1);
            }
            if (index == lastIndex)
            {
                this.throwSecondDice();
                return;
            }
            else
            {
                int[] move = { dice_result, index, 1, TeamSelection.myTeamIndex };
                Debug.LogError("ThrowSecondDice: dice value: " + dice_result + " ");
                turnManager.SendMove(move, false);
            }
        }
    }
    public void obiettaVotazione()
    {
        if (actualCard != null) GameObject.Destroy(actualCard);//PhotonNetwork.Destroy(actualCard);
        Debug.LogError("Rejecting vote");   
        byte evCode = spawnCardFromTempRiskDeckEvent;
        int content = temp_riskDeck.Count-1;
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All }; // You would have to set the Receivers to All in order to receive this event on the local client as well
        PhotonNetwork.RaiseEvent(evCode, content, raiseEventOptions, SendOptions.SendReliable);
        objectingVotes = 2;
        gameState = GameState.RiskAnalysis;
        if (PhotonNetwork.IsMasterClient)
        {
            this.turnManager.BeginTurn();
        }
    }

    public void resetEveryCard()
    {
        CardController.resetImpactAndProbabilityCards(CardController.locked_probabilities);
        CardController.resetImpactAndProbabilityCards(CardController.locked_impacts);
    }

    public void deactivatePercentageCard(int i)
    {
        CardController.locked_probabilities[i] = true;
    }

    public void deactivateImpactCard(int i)
    {
        CardController.locked_impacts[i] = true;
    }

    [PunRPC]
    void waitforSeconds(int seconds)
    {
        StartCoroutine(waitforseconds(seconds));
    }

    IEnumerator waitforseconds(int seconds)
    {
        yield return new WaitForSeconds(seconds);
    }

    bool isChosenPileEmpty(int dice_result)
    {
        bool result = false;
        if (dice_result == 1)
        {
            if (Board[0].Count == 0) return true;
        }
        else if (dice_result > 1 && dice_result <= 3)
        {
            if (Board[1].Count == 0) return true;
        }
        else
        {
            if (Board[2].Count == 0) return true;   
        }
        Debug.LogError("isChosenPileEmpty is returning result ...");
        return result;
    }
    //restituisce i punti punti accumulati
    int verifyRiskContingency(RiskCard r)
    {
        int points = 0;
        switch (r.impact)
        {
            case 0:
                foreach (int a in r.prevention_plan_low)//Per ogni risorsa richiesta, con livello di prevesione Low ...
                {
                    if (UserGameSheet.resources[a] >= 0)//Per la risorsa richiesta è stato speso un token verde o uno con valore maggiore
                    {
                        points = 3;
                    }
                }
                break;
            case 1:
                foreach (int a in r.prevention_plan_middle)//Per ogni risorsa richiesta, con livello di prevesione Middle ...
                {
                    if (UserGameSheet.resources[a] >= 1) //Per la risorsa richiesta è stato speso un token giallo o uno con valore maggiore
                    {
                        points = 3;
                    }
                    else if (UserGameSheet.actual_Yellow_Tokens > 0)
                    {
                        UserGameSheet.actual_Yellow_Tokens--;
                        points = 1;
                    }
                    else
                    {
                        points = 0;
                    }
                }
                break;
            case 2:
                foreach (int a in r.prevention_plan_high)//Per ogni risorsa richiesta, con livello di prevesione High ...
                {
                    if (UserGameSheet.resources[a] >= 2)//Per la risorsa richiesta è stato speso un token rosso
                    {
                        points = 3;
                    }
                    else if (UserGameSheet.actual_Red_Tokens > 0)
                    {
                        UserGameSheet.actual_Red_Tokens--;
                        points = 1;
                    }
                    else
                    {
                        points = 0;
                    }
                }
                break;
        }
        return points;
    }

    //[PunRPC]
    void erasePreventionPlanFromCard()
    {
        if (actualCard != null)
        {
            GameObject g1 = GameObject.FindGameObjectWithTag("PreventionPlanToHide");
            Destroy(g1);
            return;
        }
            
    }
    
    void printEventCardsDeck()
    {
        
        TMP_Text g = GameObject.Find("EventCardsHere").GetComponent<TMP_Text>();
        g.text = "";
        foreach (var i in eventCards)
        {
            g.text += i.Name;
        }
    }
    byte diceToPile(int dice)
    {
        byte pile;
        if (dice == 1)
        {
            pile = 0;
        }
        else if (dice >= 2 && dice < 4)
        {
            pile = 1;
        }
        else
        {
            pile = 2;
        }
        return pile;
    }

    public void OnEvent(EventData photonEvent)
    {
        byte code = photonEvent.Code;
        if (code == foundPlayerToStart)
        {
            int result = (int)photonEvent.CustomData;
            PlayersMaxCount = result;
            Debug.LogError("Received evento foundPlayerToStart\nNextPlayer (starting) is " + nextPlayer);
        }
        else if (code == ChooseIndexEvent) //chooseIndex
        {
            card_index = (int)(photonEvent.CustomData);
            Debug.LogError("Instantiating risk card...");
            spawnCard(false, card_index);
            decks_in_scene[0].transform.GetChild(0).gameObject.SetActive(false);
        }
        else if (code == spawnCardFromTempRiskDeckEvent)
        {
            int index = (int)photonEvent.CustomData;
            spawnCardFromTempRiskDeck(temp_riskDeck.Count - 1);
            probabilities = new int[6];
            impacts = new int[3];
        }
        else if (code == showPairOfCardsEvent) //showPairOfCards
        {
            byte[] data = (byte[])(photonEvent.CustomData);
            Debug.LogError("showPairOfCardsEvent catched " + data[0] + " " + data[1]);
            showPairOfCards(data);
            probabilities = new int[6];
            impacts = new int[3];
        }
        else if (code == spawnCardFromBoardEvent) //spawnCardFromBoard
        {
            Debug.LogError("playersTurnCompletition " + playersTurnCompletition);
            object[] objects = (object[])(photonEvent.CustomData);
            byte dice_to_pile = (byte)objects[0];
            int index = (int)objects[1];
            int playerTeamIndex = (int)objects[2];
            Debug.LogError("SPAWNCARDFROMBOARDEVENT -- INDEX: " + index + " -- PILE: " + dice_to_pile + " -- TEAM: " + playerTeamIndex);
            if (!hasThrown)
            {
                hasThrown = true;
                spawnCardFromBoard(dice_to_pile, index);
            }
        }
        else if (code == printPlayerTurnEvent) //printPlayerTurn
        {
            string nickname = photonEvent.CustomData as string;
            hasThrown = false;
            hasVoted = false;
            hasmodified = false;
            HasUpdatedUGS = false;
            Debug.LogError("received printPlayerTurnEvent "+nickname);
            if (fase.activeSelf)
            {
                GameObject.Find("Fase (sottotesto)").GetComponent<TMP_Text>().text = "È il turno di " + nickname;
                areCardsSelectable = false;
            }
        }
        else if (code == throwFirstDiceEvent) //throwFirstDice
        {
            card_index = (int)(photonEvent.CustomData);
            Debug.LogError("First dice thrown. Got " + card_index);

        }
        else if (code == throwSecondDiceEvent) //throwSecondDice
        {
            card_index = (int)(photonEvent.CustomData);
            Debug.LogError("Second dice thrown. Got " + card_index);
            //GameObject.Find("Fase (sottotesto)").GetComponent<TMP_Text>().text = "Cosa c'è scritto sulla carta?";
            if (isRiskCardInstantiated == false)
            {
                isRiskCardInstantiated = true;
                if (card_index <= 1 && card_index >= 2)
                {
                    spawnCardFromBoard(0, card_index);
                }
                else if (card_index <= 3 && card_index >= 4)
                {
                    spawnCardFromBoard(1, card_index);
                }
                else
                {
                    spawnCardFromBoard(2, card_index);
                }
            }
        }
        else if (code == spawnCardEvent)
        {
            //GameObject.Find("Fase (sottotesto)").GetComponent<TMP_Text>().text = "Cosa c'è scritto sulla carta?";
            int index = (int)photonEvent.CustomData;
            Debug.LogError("SpawnCardEvent index: " + index);
            spawnCard(true, index);
        }
        /*else if (code == terminateGameEvent)
        {
            Debug.LogError("Received terminateGameEvent");
            object[] result = (object[])photonEvent.CustomData;
            int actorNumber = (int)result[0];
            string nickname = (string)result[1];
            int teamIndex = (int)result[2];
            premibackslash.SetActive(false);
            premiazione.SetActive(false);
            fase.SetActive(false);
            Debug.LogError("Terminating game...");
            isGameStarted = false;
            ActorNumbers.Clear();
            Destroy(this.turnManager);
            eventCards = new List<IEventCard>();
            riskDeck = new List<RiskCard>();
            temp_riskDeck = new List<RiskCard>();
            Board = new Dictionary<byte, List<RiskCard>>();
            probabilities = new int[6];
            impacts = new int[3];
            sprintNumber = 0;
            if (actualCard != null)
            {
                Destroy(actualCard);
                actualCard = null;
            }
            if (actualEventCard != null)
            {
                Destroy(actualEventCard);
                actualEventCard = null;
            }
            if (pairOfCards != null)
            {
                Destroy(pairOfCards);
                pairOfCards = null;
            }
            playersTurnCompletition = 0; //quanti utenti hanno completato il turno
            completedUserGameSheets = 0;
            areCardsSelectable = false;
            isLocalPlayerTurn = false;
            PhotonNetwork.CurrentRoom.IsOpen = true;
            pointsForEachTeam = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            lastVote = new byte[2];
            isMyTurnToTalk = false;
            cardControls.SetActive(false);
            objectingVotes = 0;
            numberOfVotes = 0;
            numberOfNonTeamMates = 0;
            isStartingPlayer = false;
            if (gameState >= GameState.Procurement)
            {
                OnTerminetingGameClearUGS?.Invoke();
            }
            gameState = GameState.RiskAnalysis;
            resetTeamVariables?.Invoke();
            OnMyPlayerLeftTheRoom?.Invoke(); //clears every chair in the room for a future connection
            isPossibleToLeave();
        }*/
        else if (code == updatePointsForEachTeamEvent) //updatePointsForEachTeam
        {
            Debug.LogError("Ricevuto evento updatePointsForEachTeamEvent");
            int[] ints = (int[])photonEvent.CustomData;
            updatePointsForEachTeam(ints[0], ints[1]);
        }
        else if (code == playerLeftEvent)
        {
            Debug.LogError("Received evento playerLeftEvent");
            object[] result = (object[])photonEvent.CustomData;
            int actorNumber = (int)result[0];
            string nickname = (string)result[1];
            int teamIndex = (int)result[2];
            OnLeave?.Invoke(teamIndex, nickname);
            if (ActorNumbers.Contains(actorNumber))
            {
                numberOfNonTeamMates--;
            }
            Debug.LogError("number of non teamMates is now: " + numberOfNonTeamMates);
            /*
            foreach (int i in PhotonNetwork.CurrentRoom.Players.Keys)
            {
                if (i.Equals(actorNumber))
                {
                    startNextTurn(PhotonNetwork.CurrentRoom.Players[i], false);
                    break;
                }
            }*/
            //OnGameFinished?.Invoke(); //resets the chairState
            Logger.Instance.LogInfo($"<color=yellow>{nickname}</color> si è disconnesso dalla partita");
            LogManager.Instance.LogInfo($"{nickname} si è disconnesso dalla partita");
        }
    }
    void isPossibleToLeave()
    {
        OnGoingToLeave?.Invoke(); 
    }
    IEnumerator blinkText(string text, GameObject text_gameobject)
    {
        text_gameobject.GetComponent<TMP_Text>().text = text;
        Color original = new Color(255f, 237f, 0f);
        original.a = 159;
        yield return new WaitForSeconds(6f);
        mostraVotoRifiutato = false;
    }
    void terminateGame()
    {
        //if (/*PlayersActorNumbers.Count()*/ConnectToServer.ActorNumberOfplayersInRoom.Count() > 1 /*4*/ || gameState.Equals(GameState.Premiation))
        if (true)
        {
            byte evCode = playerLeftEvent;
            object[] content = new object[3];
            content[0] = PhotonNetwork.LocalPlayer.ActorNumber;
            content[1] = PhotonNetwork.LocalPlayer.NickName;
            content[2] = TeamSelection.myTeamIndex;
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others }; // You would have to set the Receivers to All in order to receive this event on the local client as well
            PhotonNetwork.RaiseEvent(evCode, content, raiseEventOptions, SendOptions.SendReliable);
            isLocalPlayerTurn = false; //blocca la possibilità di lanciare il dado
            areCardsSelectable = false; //blocca la possibilità di scegliere le carte a prescindere da quelle scelte dal compagno
            if (fase.activeSelf && (GameObject.Find("Fase (sottotesto)").GetComponent<TMP_Text>().text.Equals("È il tuo turno") || 
            GameObject.Find("Fase (sottotesto)").GetComponent<TMP_Text>().text.Equals("Premi la freccia destra per lanciare il dado") ||
            GameObject.Find("Fase (sottotesto)").GetComponent<TMP_Text>().text.Contains("Hai")) && PhotonNetwork.CurrentRoom.PlayerCount > 1)
            {
                if (gameState.Equals(GameState.PlanningPoker))
                {
                    this.turnManager.SendMove(true, true);
                }
                else if (gameState.Equals(GameState.RiskAnalysis))
                {
                    Debug.LogError("Sending ad empty move");
                    this.turnManager.SendMove(null, true);
                    Debug.LogError("Empty move sent");
                }
                else if (gameState.Equals(GameState.RiskManagement))
                {
                    this.turnManager.SendMove(null, false);
                    Debug.LogError(PhotonNetwork.LocalPlayer.ActorNumber + " sent an empty move");
                }
                else if (gameState.Equals(GameState.Procurement))
                {
                    if (TeamSelection.Teams[TeamSelection.myTeamIndex].Count <= 1)
                    {
                        this.turnManager.SendMove(new int[] { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 }, true);
                    }
                }
                else if (gameState.Equals(GameState.CardReading))
                {
                    this.turnManager.SendMove((string)"finishedReading", true);
                }
            }
            Destroy(this.turnManager);
            premibackslash.SetActive(false);
            premiazione.SetActive(false);
            fase.SetActive(false);
            Debug.LogError("Terminating game...");
            isGameStarted = false;            
            ActorNumbers.Clear();
            eventCards = new List<IEventCard>();
            riskDeck = new List<RiskCard>();
            temp_riskDeck = new List<RiskCard>();
            cardControls.SetActive(false);
            Board = new Dictionary<byte, List<RiskCard>>();
            probabilities = new int[6];
            impacts = new int[3];
            sprintNumber = 0;
            if (actualCard != null)
            {
                Destroy(actualCard);
                actualCard = null;
            }
            if (actualEventCard != null)
            {
                Destroy(actualEventCard);
                actualEventCard = null;
            }
            if (pairOfCards != null)
            {
                Destroy(pairOfCards);
                pairOfCards = null;
            }
            playersTurnCompletition = 0; //quanti utenti hanno completato il turno
            completedUserGameSheets = 0;
            areCardsSelectable = false;
            isLocalPlayerTurn = false;
            pointsForEachTeam = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            lastVote = new byte[2];
            isMyTurnToTalk = false;
            objectingVotes = 0;
            numberOfVotes = 0;
            numberOfNonTeamMates = 0;
            isStartingPlayer = false;
            if (gameState >= GameState.Procurement)
            {
                OnTerminetingGameClearUGS?.Invoke();
            }
            gameState = GameState.RiskAnalysis;
            resetTeamVariables?.Invoke();
            OnMyPlayerLeftTheRoom?.Invoke(); //clears every chair in the room for a future connection
            isPossibleToLeave();
            lastPlayer = null;
            lastPlayerToThrowDice = null;
            isFirstTurnInRiskManagement = true;
        }
    }
}