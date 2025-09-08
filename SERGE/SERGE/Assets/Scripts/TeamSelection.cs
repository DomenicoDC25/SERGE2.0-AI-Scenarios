using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Pun.UtilityScripts;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using TMPro;
using TMPro.Examples;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;


public class TeamSelection : MonoBehaviour, IOnEventCallback
{
    private readonly byte addPlayerToTeamEvent = 199;
    private readonly byte removePlayerFromTeamEvent = 186;
    private readonly byte terminateGameEvent = 184;
    public static Dictionary<int, List<string>> Teams = new Dictionary<int, List<string>>();
    private int maxPlayersPerTeam = 2;
    public static bool isSoloMode = true;
    public static event Action OnTeamsReady;
    private bool hasSelected = false;
    public static int myTeamIndex = -1;
    public static event Action<byte> OnTeamSelected; //imposta il photonViewGroup per le carte e per lo UGS
    public static event Action OnRemovedFromTeam;
    public static event Action<int> OnSelectTeamInsertInList;
    public static event Action<int> OnExitTeamRemoveFromList;
    public static bool isTeamMate = false;
    public static int playersInRoom = 0;
    [SerializeField] private GameObject cardControls;
    [SerializeField] private GameObject solochairs, duochairs;

    public bool getIsSoloMode()
    {
        return isSoloMode;
    }
    void Awake()
    {
        //cardControls = GameObject.FindGameObjectWithTag("CardsControls");
    }



    void Start()
    {
        bool result = this.gameObject.GetComponent<TeamSelection>().enabled;

        ConnectToServer.OnCreatedOrJoinedRoom += setGameMode;
        PlayerController.OnSeat += JoinTeam;
        PlayerController.OnGetUp += removefromTeam;
        GameMechanics.OnLeave += removePlayerFromTeam;
        GameMechanics.resetTeamVariables += resetVariables;
        for (int i = 0; i <= 12; i++)
        {
            if (!Teams.ContainsKey(i))
            {
                Teams.Add(i, new List<string>());
            }
        }

    }

    private void OnEnable()
    {
        //PhotonNetwork.NetworkingClient.EventReceived += OnEvent;
        PhotonNetwork.AddCallbackTarget(this);
    }

    private void OnDisable()
    {
        //PhotonNetwork.NetworkingClient.EventReceived -= OnEvent;
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    private void Update()
    {
        if (PhotonNetwork.CurrentRoom != null)
        {
            playersInRoom = PhotonNetwork.CurrentRoom.PlayerCount;
            if (isSoloMode)
            {
                solochairs.SetActive(true);
                duochairs.SetActive(false);
            }
            else if (!isSoloMode)
            {
                duochairs.SetActive(true);
                solochairs.SetActive(false);
            }
        }
        /*GameObject.Find("EventCardSpawn").GetComponent<TMP_Text>().text = 
        "playersTurnCompletition: " + GameMechanics.playersTurnCompletition + "\n" +
                                                                          "maxPlayersPerTeam:\t " + maxPlayersPerTeam + "\n" +
                                                                          "Team   plrs pts\tplayers\n" +
                                                                          "Team 0: " + Teams[0].Count + "\t" + GameMechanics.pointsForEachTeam[0] + " ( " + printTeam(0, Teams)[0] + ", " + printTeam(0, Teams)[1] + ")\n" +
                                                                          "Team 1: " + Teams[1].Count + "\t" + GameMechanics.pointsForEachTeam[1] + " ( " + printTeam(1, Teams)[0] + ", " + printTeam(1, Teams)[1] + ")\n" +
                                                                          "Team 2: " + Teams[2].Count + "\t" + GameMechanics.pointsForEachTeam[2] + " ( " + printTeam(2, Teams)[0] + ", " + printTeam(2, Teams)[1] + ")\n" +
                                                                          "Team 3: " + Teams[3].Count + "\t" + GameMechanics.pointsForEachTeam[3] + " ( " + printTeam(3, Teams)[0] + ", " + printTeam(3, Teams)[1] + ")\n" +
                                                                          "Team 4: " + Teams[4].Count + "\t" + GameMechanics.pointsForEachTeam[4] + " ( " + printTeam(4, Teams)[0] + ", " + printTeam(4, Teams)[1] + ")\n" +
                                                                          "Team 5: " + Teams[5].Count + "\t" + GameMechanics.pointsForEachTeam[5] + " ( " + printTeam(5, Teams)[0] + ", " + printTeam(5, Teams)[1] + ")\n" +
                                                                          "Team 6: " + Teams[6].Count + "\t" + GameMechanics.pointsForEachTeam[6] + " ( " + printTeam(6, Teams)[0] + ", " + printTeam(6, Teams)[1] + ")\n" +
                                                                          "Team 7: " + Teams[7].Count + "\t" + GameMechanics.pointsForEachTeam[7] + " ( " + printTeam(7, Teams)[0] + ", " + printTeam(7, Teams)[1] + ")\n" +
                                                                          "Team 8: " + Teams[8].Count + "\t" + GameMechanics.pointsForEachTeam[8] + " ( " + printTeam(8, Teams)[0] + ", " + printTeam(8, Teams)[1] + ")\n" +
                                                                          "Team 9: " + Teams[9].Count + "\t" + GameMechanics.pointsForEachTeam[9] + " ( " + printTeam(9, Teams)[0] + ", " + printTeam(9, Teams)[1] + ")\n" +
                                                                          "Team10: " + Teams[10].Count + "\t" + GameMechanics.pointsForEachTeam[10] + " ( " + printTeam(10, Teams)[0] + ", " + printTeam(10, Teams)[1] + ")\n" +
                                                                          "Team11: " + Teams[11].Count + "\t" + GameMechanics.pointsForEachTeam[11] + " ( " + printTeam(11, Teams)[0] + ", " + printTeam(11, Teams)[1] + ")\n" +
                                                                          "isThisUserTeamMate: " + TeamSelection.isTeamMate + "\n" +
                                                                          "isStartingPlayer:\t " + GameMechanics.isStartingPlayer + "\n" +
                                                                          "PlayerActorNumber:\t" + PhotonNetwork.LocalPlayer.ActorNumber + "\n" +
                                                                          "completedUserGameSheets:\t" + GameMechanics.completedUserGameSheets + "\n" +
                                                                          "playersTurnCompletition:\t" + GameMechanics.playersTurnCompletition + "\n" +
                                                                          "areCardsSelectable:\t" + GameMechanics.areCardsSelectable + "\n" +
                                                                          "gameMode:\t " + PhotonNetwork.CurrentRoom.CustomProperties["g"].ToString() + "\n" +
                                                                          "myTeamIndex:\t " + TeamSelection.myTeamIndex + "\n";
        /*printList(GameMechanics.ActorNumbers);
        GameObject.Find("EventCardSpawn").GetComponent<TMP_Text>().text += "nextActorNumberIndex: " + GameMechanics.nextActorNumberIndex;*/
        //wcGameObject.Find("EventCardSpawn").GetComponent<TMP_Text>().text += "PlayerToPlayer: " + GameMechanic.ActorNumbers[GameMechanic.nextActorNumberIndex];
    }
    
    public void printList(List<int> list)
    {
        //GameObject.Find("EventCardSpawn").GetComponent<TMP_Text>().text += "ActorNumbers: ";
        string actornumbers = "";
        foreach (int i in list)
        {
            actornumbers += i+" "; 
        }
        actornumbers += "\n";
        Debug.LogError(actornumbers);
    }
    public void JoinTeam(int teamNumber)
    {
        Debug.LogError("Joining team...");
        myTeamIndex = teamNumber;
        PhotonNetwork.LocalPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable(){ {"myTeamIndex", teamNumber}});
        string nickname = PhotonNetwork.LocalPlayer.NickName;
        PhotonNetwork.SetInterestGroups(byte.Parse((teamNumber+1).ToString()), true);
        if (Teams[teamNumber].Count >= 1) /* C'è già un utente nel team */
        {
            Debug.LogError("C'è già un utente nel team");
            isTeamMate = true;
        }
        else isTeamMate = false;        
        OnTeamSelected?.Invoke(byte.Parse(teamNumber.ToString()));
        string member = PhotonNetwork.LocalPlayer.NickName;
        //this.photonView.RPC("addPlayerToTeam", RpcTarget.All, teamNumber, member);
        byte evCode = addPlayerToTeamEvent;
        object[] content = new object[2];
        content[0] = teamNumber;
        content[1] = member;
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All }; // You would have to set the Receivers to All in order to receive this event on the local client as well
        PhotonNetwork.RaiseEvent(evCode, content, raiseEventOptions, SendOptions.SendReliable);
    }

    /*[PunRPC]*/
    public void addPlayerToTeam(int teamNumber, string member)
    {
        if (!Teams[teamNumber].Contains(member))
        {
            int memberActorNumber = getActorNumberFromNickname(member);
            int debug_info = Teams[teamNumber].Count;
            Debug.LogError("This memberActorNumber " + memberActorNumber);
            if (Teams[teamNumber].Count < maxPlayersPerTeam)
            {
                Teams[teamNumber].Add(member);
                //Logger.Instance.LogInfo(member + " si è unito al team " + teamNumber);
                if (Teams[teamNumber].Count == 1) //è il primo giocatore ad essere inserito nel team
                {
                    Debug.LogError("Invocando OnSelectTeamInsertInList");
                    OnSelectTeamInsertInList?.Invoke(memberActorNumber);//<-- Aggiunge l'actorNumber alla lista di giocatori che possono giocare
                }
                Debug.LogError(member + " è stato aggiunto al team " + teamNumber + " il count era " + debug_info + " ora è " + Teams[teamNumber].Count);
                if (checkOnePlayerPerTeam())
                {
                    OnTeamsReady?.Invoke();
                }
            }
        }
    }
    public bool checkOnePlayerPerTeam()
    {
        int test = 0;
        int totalOfPlayers = 0;
        bool result = false;
        for (int i = 0; i <= 12; i++)
        {
            totalOfPlayers += Teams[i].Count; //Conto quanti giocatori si sono seduti
            if (Teams[i].Count >= 1) test++;
        }
        if (test >= 1 /*1*/ && totalOfPlayers >= PhotonNetwork.CurrentRoom.PlayerCount) result = true;
        else result = false;
        return result;
    }

    public void removefromTeam()
    {
        if (myTeamIndex != -1)
        {
            string nickname = PhotonNetwork.LocalPlayer.NickName;
            //Annullamento dell'iscrizione all'interest group della squadra identificata dal proprio teamNumber
            //PhotonNetwork.SetInterestGroups(byte.Parse((myTeamIndex+1).ToString()), false);
            OnRemovedFromTeam?.Invoke();
            //this.photonView.RPC("decrementPlayersInTeam", RpcTarget.All, myTeamIndex, nickname);
            byte evCode = removePlayerFromTeamEvent;
            object[] content = new object[2];
            content[0] = myTeamIndex;
            content[1] = nickname;
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All }; // You would have to set the Receivers to All in order to receive this event on the local client as well
            PhotonNetwork.RaiseEvent(evCode, content, raiseEventOptions, SendOptions.SendReliable);
            PhotonNetwork.LocalPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable() { { "myTeamIndex", -1 } });
            myTeamIndex = -1;
        }
    }
    

    /*[PunRPC]*/
    public void removePlayerFromTeam(int teamIndex, string member)
    {
        if (teamIndex != -1)
        {
            int memberActorNumber = getActorNumberFromNickname(member);
            Debug.LogError("L'index del player " + Teams[teamIndex].IndexOf(member));
            Debug.LogError("This memberActorNumber " + memberActorNumber);
            if (Teams[teamIndex].IndexOf(member) == 0) //Sto rimuovendo il primo giocatore
            {
                Debug.LogError("Invocando OnExitTeamRemoveFromList");
                OnExitTeamRemoveFromList?.Invoke(memberActorNumber); //<-- Rimuove l'actorNumber alla lista di giocatori che possono giocare
                Teams[teamIndex].Remove(member);
                if (Teams[teamIndex].Count != 0) //C'è ancora un utente nella squadra
                {
                    if (PhotonNetwork.LocalPlayer.NickName.Equals(Teams[teamIndex][0]))
                    {
                        isTeamMate = false;
                    }
                    memberActorNumber = getActorNumberFromNickname(Teams[teamIndex][0]);
                    OnSelectTeamInsertInList?.Invoke(memberActorNumber); //<-- Inserisce l'unico utente rimasto nel team nella lista di giocatori che possono giocare
                }
            }
            else
            {
                Teams[teamIndex].Remove(member);
            }
        }
    }

    public static int getActorNumberFromNickname(string nickname)
    {
        foreach (var player in PhotonNetwork.CurrentRoom.Players.Values)
        {
            if (player.NickName == nickname)
            {
                //Debug.LogError(nickname + " == " + player.NickName + " ?");
                return player.ActorNumber;
            }
        }
        return -1; //Restituisce -1 se non trova il nickname
    }

    public static string getNicknameFromActorNumber(int actorNumber)
    {
        foreach (var player in PhotonNetwork.CurrentRoom.Players.Values)
        {
            if (player.ActorNumber == actorNumber)
            {
                //Debug.LogError(nickname + " == " + player.NickName + " ?");
                return player.NickName;
            }
        }
        return null; //Restituisce -1 se non trova il nickname
    }

    public static string[] printTeam(int team, Dictionary<int, List<string>> Teams)
    {
        string[] result = {"", ""};
        if (Teams[team].Count > 0)
        {
            result[0] = Teams[team][0];
            if (Teams[team].Count > 1) //C'è più di un utente nel team
            {
                result[1] = Teams[team][1];
            }
        }
        return result;
    }

    public void setGameMode()
    {
        string mode = (string)PhotonNetwork.CurrentRoom.CustomProperties["g"];
        if (mode.Equals("SINGLE"))
        {
            isSoloMode = true;
            maxPlayersPerTeam = 1;
        }
        else
        {
            isSoloMode = false;
            maxPlayersPerTeam = 2;
        }
    }
    public void OnEvent(EventData photonEvent)
    {
        byte code = photonEvent.Code;
        if (code == addPlayerToTeamEvent) 
        {
            Debug.LogError("Recived event addPlayerToTeamEvent");
            printList(GameMechanics.ActorNumbers);
            object[] result = (object[])photonEvent.CustomData;
            int teamNumber = (int)result[0];
            string member = (string)result[1];
            addPlayerToTeam(teamNumber, member);
        }
        else if (code == removePlayerFromTeamEvent)
        {
            Debug.LogError("Recived event removePlayerFromTeamEvent");
            object[] result = (object[])photonEvent.CustomData;
            int teamNumber = (int)result[0];
            string member = (string)result[1];
            removePlayerFromTeam(teamNumber, member);
        }
    }

    void resetVariables()
    {
        Debug.LogError("Resetting team variables");
        for (int i = 0; i <= 12; i++)
        {
            Teams[i].Clear();
        }
        maxPlayersPerTeam = 2;
        //isSoloMode = true;
        hasSelected = false;
        myTeamIndex = -1;
        isTeamMate = false;
    }
}
