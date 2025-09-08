using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Runtime.CompilerServices;
using TMPro;
using Unity.VisualScripting;
//using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR;

public class UserGameSheet : MonoBehaviour, IOnEventCallback 
{
    private readonly byte resourcePurchased = 182;
    private readonly byte resourcesSubmitted = 181;
    private readonly byte annullaEvent = 180;
    private readonly byte setFirstSprintEvent = 179;
    private readonly byte setSecondSprintEvent = 178;
    private readonly byte setThirdSprintEvent = 177;
    private int max_Red_Tokens, max_Yellow_Tokens, max_Green_Tokens; 
    [SerializeField] public static int actual_Red_Tokens, actual_Yellow_Tokens, actual_Green_Tokens;
    [SerializeField] private Sprite[] tokenSprites;
    public static bool compilationTerminated = false;
    public static int[] resources = { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 }; //resources va aggiornato per l'intero team
    private int i = 0;
    [SerializeField] private GameObject[] resources_go;
    public static event Action OnUGScompleted;
    public static bool isVisible = false;
    public static bool lock_i = false;
    [SerializeField] private GameObject[] tokens;
    [SerializeField] private GameObject[] sprintData;
    PhotonView photonView;
    public static event Action close;
    public GameObject premiBackSlash;
    private bool easterEggMode = false;
    public void setI(int i)
    {
        this.i = i;
    }

    public int getI()
    {
        return this.i;
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
    // Start is called before the first frame update

    private void Awake()
    {
        premiBackSlash = GameObject.Find("LoggedUI").transform.GetChild(0).gameObject;
        premiBackSlash.SetActive(false);
        PlayerController.showUGS += showUserGameSheet;
        GameMechanics.OnRiskManagementStateSetCloseUGS += hideUserGameSheet;
        GameMechanics.OnProcurementStateSet += showUserGameSheet;
        PlayerController.hideUGS += hideUserGameSheet;
        GameMechanics.OnRiskManagementStateSet += SetUpTokens;
        GameMechanics.OnRiskMitigationOnSprint += setSprint;
        GameMechanics.autoFillUserGameSheet += autoFill;
        GameMechanics.OnGameStartSetUpTokens += SetUpTokens;
        GameMechanics.OnTerminetingGameClearUGS += clearUGS;
        TextChat.easterEgg += SetUpTokens;
        ConnectToServer.hideUGSandTokensOnJoinedRoom += hideUserGameSheet;
        hideUserGameSheet();
        InfoCardsController.close += hideUserGameSheet;
        UserCardController.close += hideUserGameSheet;
    }
    void Start()
    {
        SetUpTokens(3, 3, 4);
    }

    private void Update()
    {
        if (TeamSelection.isTeamMate)
        {
            GameObject.FindGameObjectWithTag("TokenLow").GetComponent<Button>().interactable = false;
            GameObject.FindGameObjectWithTag("TokenHigh").GetComponent<Button>().interactable = false;
            GameObject.FindGameObjectWithTag("TokenMedium").GetComponent<Button>().interactable = false;
            GameObject.Find("Conferma").GetComponent<Button>().interactable = false;
            GameObject.Find("Annulla").GetComponent<Button>().interactable = false;
        }
        else if (GameMechanics.gameState == GameState.Procurement && !TeamSelection.isTeamMate)
        {
            if (resources[resources.Length-1] != -1)
            {
                GameObject.Find("Conferma").GetComponent<Button>().interactable = true;
            }
            if (i >= 0)
            {
                GameObject.Find("Annulla").GetComponent<Button>().interactable = true;
            }
            if (actual_Green_Tokens == 0)
            {
                GameObject.FindGameObjectWithTag("TokenLow").GetComponent<Button>().interactable = false;
            }
            if (actual_Red_Tokens == 0)
            {
                GameObject.FindGameObjectWithTag("TokenHigh").GetComponent<Button>().interactable = false;
            }
            if (actual_Yellow_Tokens == 0)
            {
                GameObject.FindGameObjectWithTag("TokenMedium").GetComponent<Button>().interactable = false;
            }
            if (actual_Green_Tokens > 0)
            {
                GameObject.FindGameObjectWithTag("TokenLow").GetComponent<Button>().interactable = true;
                //GameObject.Find("Conferma").GetComponent<Button>().interactable = false;
            }
            if (actual_Red_Tokens > 0)
            {
                GameObject.FindGameObjectWithTag("TokenHigh").GetComponent<Button>().interactable = true;
                //GameObject.Find("Conferma").GetComponent<Button>().interactable = false;
            }
            if (actual_Yellow_Tokens > 0)
            {
                GameObject.FindGameObjectWithTag("TokenMedium").GetComponent<Button>().interactable = true;
                //GameObject.Find("Conferma").GetComponent<Button>().interactable = false;
            }
            if (compilationTerminated)
            {
                GameObject.Find("Conferma").GetComponent<Button>().interactable = false;
                GameObject.Find("Annulla").GetComponent<Button>().interactable = false;
            }
        }
        if (this.gameObject.activeSelf)
        {
            premiBackSlash.SetActive(false);
        }
        else
        {
            GameObject.FindGameObjectWithTag("TokenLow").GetComponent<Button>().interactable = false;
            GameObject.FindGameObjectWithTag("TokenHigh").GetComponent<Button>().interactable = false;
            GameObject.FindGameObjectWithTag("TokenMedium").GetComponent<Button>().interactable = false;
            GameObject.Find("Conferma").GetComponent<Button>().interactable = false;
            GameObject.Find("Annulla").GetComponent<Button>().interactable = false;
        }
        if (GameMechanics.isGameStarted && TeamSelection.myTeamIndex != -1)
        {
            sprintData[9].GetComponent<TMP_Text>().text = GameMechanics.pointsForEachTeam[TeamSelection.myTeamIndex].ToString();
            //sprintData[3].GetComponent<TMP_Text>().text = GameMechanics.pointsForEachTeam[(int)PhotonNetwork.LocalPlayer.CustomProperties["myTeamIndex"]].ToString();
        }
    }
    void SetUpTokens(int red, int yellow, int green)
    {
        max_Red_Tokens = red;
        max_Yellow_Tokens = yellow;
        max_Green_Tokens = green;

        actual_Red_Tokens = red;
        actual_Yellow_Tokens = yellow;
        actual_Green_Tokens = green;
        if (easterEggMode)
        {
            SetUpTokens(99, 99, 99);
        }
    }

    public void assignRedToken(bool isTeamMate)
    {
        actual_Red_Tokens--;
        playAnimation("TokenHigh");
        resources[i] = 2;
        resources_go[i].GetComponent<Image>().sprite = tokenSprites[2];
        resources_go[i].GetComponentInChildren<TMP_Text>().text = "High Level";
        if (!lock_i)
        {
            i++;
        }
        if (!isTeamMate)
        {
            RaisePurchaseEvent(2);
        }
    }

    public void assignGreenToken(bool isTeamMate)
    {
        actual_Green_Tokens--;
        playAnimation("TokenLow");
        resources[i] = 0;
        resources_go[i].GetComponent<Image>().sprite = tokenSprites[0];
        resources_go[i].GetComponentInChildren<TMP_Text>().text = "Low Level";
        if (!lock_i)
        {
            i++;
        }
        if (!isTeamMate)
        {
            RaisePurchaseEvent(0);
        }
    }

    public void assignYellowToken(bool isTeamMate)
    {
        actual_Yellow_Tokens--;
        playAnimation("TokenMedium");
        resources[i] = 1;
        resources_go[i].GetComponent<Image>().sprite = tokenSprites[1];
        resources_go[i].GetComponentInChildren<TMP_Text>().text = "Medium Level";
        if (!lock_i)
        {
            i++;
        }
        if (!isTeamMate)
        {
            RaisePurchaseEvent(1);
        }
    }

    private void RaisePurchaseEvent(int tokenSpent)
    {
        byte evCode = resourcePurchased;
        int[] content = new int[2];
        content[0] = TeamSelection.myTeamIndex;
        content[1] = tokenSpent;
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All }; // You would have to set the Receivers to All in order to receive this event on the local client as well
        PhotonNetwork.RaiseEvent(evCode, content, raiseEventOptions, SendOptions.SendReliable);
    }
    
    public void raiseResourcesSubmitted()
    {
        byte evCode = resourcesSubmitted;
        int content = TeamSelection.myTeamIndex;
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All }; // You would have to set the Receivers to All in order to receive this event on the local client as well
        PhotonNetwork.RaiseEvent(evCode, content, raiseEventOptions, SendOptions.SendReliable);
    }

    public void raiseAnnulla()
    {
        byte evCode = annullaEvent;
        int content = TeamSelection.myTeamIndex;
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All }; // You would have to set the Receivers to All in order to receive this event on the local client as well
        PhotonNetwork.RaiseEvent(evCode, content, raiseEventOptions, SendOptions.SendReliable);
    }
/*    private void RaiseSetSprint(int sprint, byte impact, string risk, int points)
    {
        byte evCode = setFirstSprintEvent;
        switch (sprint)
        {
            case 1:
                evCode = setFirstSprintEvent;
                break;
            case 2:
                evCode = setSecondSprintEvent;
                break;
            case 3:
                evCode = setThirdSprintEvent;
                break;
        }
        object[] content = new object[4];
        content[0] = TeamSelection.myTeamIndex;
        content[1] = impact;
        content[2] = risk;
        content[3] = points;
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All }; // You would have to set the Receivers to All in order to receive this event on the local client as well
        PhotonNetwork.RaiseEvent(evCode, content, raiseEventOptions, SendOptions.SendReliable);
    }*/

    public void annulla(bool isTeamMate)
    {
        if (i > 0) i--;        
            switch (resources[i])
            {
                case 0:
                    if (resources_go[i].GetComponent<Image>().sprite.Equals(tokenSprites[3]) && i > 0)
                    {
                        i--;
                        resources[i + 1] = -1;
                    }
                    if (actual_Green_Tokens < max_Green_Tokens) actual_Green_Tokens++;
                    resources_go[i].GetComponent<Image>().sprite = tokenSprites[3];
                    resources_go[i].GetComponentInChildren<TMP_Text>().text = "";
                    break;
                case 1:
                if (resources_go[i].GetComponent<Image>().sprite.Equals(tokenSprites[3]) && i > 0)
                {
                    i--;
                    resources[i + 1] = -1;
                }
                    if (actual_Yellow_Tokens < max_Yellow_Tokens) actual_Yellow_Tokens++;
                    resources_go[i].GetComponent<Image>().sprite = tokenSprites[3];
                    resources_go[i].GetComponentInChildren<TMP_Text>().text = "";
                    break;
                case 2:
                if (resources_go[i].GetComponent<Image>().sprite.Equals(tokenSprites[3]) && i > 0)
                {
                    i--;
                    resources[i + 1] = -1;
                }
                    if (actual_Red_Tokens < max_Red_Tokens) actual_Red_Tokens++;
                    resources_go[i].GetComponent<Image>().sprite = tokenSprites[3];
                    resources_go[i].GetComponentInChildren<TMP_Text>().text = "";
                    break;
            }
        if (!isTeamMate)
        {
            raiseAnnulla();
        }
    }

    public void hideUserGameSheet()
    {
        isVisible = false;
        this.gameObject.SetActive(false);
    }

    public void showUserGameSheet()
    {
        isVisible = true;
        this.gameObject.SetActive(true);
    }
    public void playAnimation(string tag)
    {
        GameObject.FindGameObjectWithTag(tag).GetComponent<Animator>().SetTrigger("rotate");
    }
    public void confirm(bool isTeamMate)
    {
        GameObject.Find("Annulla").GetComponent<Button>().interactable = false;
        GameObject.Find("Conferma").GetComponent<Button>().interactable = false;
        tokens[0].GetComponent<Animator>().Play("Idle");
        tokens[1].GetComponent<Animator>().Play("Idle");
        tokens[2].GetComponent<Animator>().Play("Idle");
        compilationTerminated = true;
        OnUGScompleted?.Invoke();
        if (!isTeamMate)
        {
            raiseResourcesSubmitted();
        }
        hideUserGameSheet();
    }
    public void setSprint(byte impact, string risk, int points)
    {
        string mitigation = "";
        string impatto = "";
        if (points == 0) mitigation = "No Mitigate ";
        else if (points == 1) mitigation = "Contingency";
        else if (points == 3) mitigation = "Prevention\t ";
        if (impact == 0) impatto = "Low\t\t ";
        else if (impact == 1) impatto = "Medium\t ";
        else if (impact == 2) impatto = "High\t\t ";
        string riga = risk+"         "+ impatto + " "+mitigation+"          " + points.ToString();
        sprintData[GameMechanics.sprintNumber - 1].GetComponent<TMP_Text>().text += riga;
        premiBackSlash.SetActive(true);
    }
    public void setSecondSprint(byte impact, string risk, int points, bool isTeamMate)
    {
        string mitigation = "";
        string impatto = "";
        if (points == 0) mitigation = "No Mitigate ";
        else if (points == 1) mitigation = "Contingency";
        else if (points == 3) mitigation = "Prevention\t ";
        if (impact == 0) impatto = "Low\t\t ";
        else if (impact == 1) impatto = "Medium\t ";
        else if (impact == 2) impatto = "High\t\t ";
        string riga = risk + "         " + impatto + " " + mitigation + "          " + points.ToString();
        sprintData[GameMechanics.sprintNumber - 1].GetComponent<TMP_Text>().text += riga;
        premiBackSlash.SetActive(true);
    }
    public void setThirdSprint(byte impact, string risk, int points, bool isTeamMate)
    {
        string mitigation = "";
        string impatto = "";
        if (points == 0) mitigation = "No Mitigate ";
        else if (points == 1) mitigation = "Contingency";
        else if (points == 3) mitigation = "Prevention\t ";
        if (impact == 0) impatto = "Low\t\t ";
        else if (impact == 1) impatto = "Medium\t ";
        else if (impact == 2) impatto = "High\t\t ";
        string riga = risk + "         " + impatto + " " + mitigation + "          " + points.ToString();
        sprintData[GameMechanics.sprintNumber - 1].GetComponent<TMP_Text>().text += riga;
        premiBackSlash.SetActive(true);
    }
    private void RaiseSetSprint(int sprint, byte impact, string risk, int points, int sprintNumber)
    {
        byte evCode = setFirstSprintEvent;
        switch (sprint)
        {
            case 1:
                evCode = setFirstSprintEvent;
                break;
            case 2:
                evCode = setSecondSprintEvent;
                break;
            case 3:
                evCode = setThirdSprintEvent;
                break;
        }
        object[] content = new object[5];
        content[0] = TeamSelection.myTeamIndex;
        content[1] = impact;
        content[2] = risk;
        content[3] = points;
        content[4] = sprintNumber;
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All }; // You would have to set the Receivers to All in order to receive this event on the local client as well
        PhotonNetwork.RaiseEvent(evCode, content, raiseEventOptions, SendOptions.SendReliable);
    }
    public void autoFill()
    {
        actual_Green_Tokens = 0;
        actual_Red_Tokens = 0;
        actual_Yellow_Tokens = 0;
        int[] newReosurces = { 0, 0, 0, 0, 1, 1, 1, 2, 2, 2 };
        resources = newReosurces;
        GameObject.Find("Annulla").GetComponent<Button>().interactable = false;
        GameObject.Find("Conferma").GetComponent<Button>().interactable = false;
        compilationTerminated = true;
        OnUGScompleted?.Invoke();
    }
    public void OnEvent(EventData photonEvent)
    {
        byte code = photonEvent.Code;
        if (code == resourcePurchased)
        {
            Debug.LogError("Recived resource purchased");
            int[] result = (int[])photonEvent.CustomData;
            if (TeamSelection.myTeamIndex == result[0] && TeamSelection.isTeamMate)
            {
                switch (result[1])
                {
                    case 0:
                        assignGreenToken(true);
                        break;
                    case 1:
                        assignYellowToken(true);
                        break;
                    case 2:
                        assignRedToken(true);
                        break;
                }
            } 
        }
        else if (code == resourcesSubmitted)
        {
            Debug.LogError("Recived resource submitted");
            int result = (int)photonEvent.CustomData;
            if (TeamSelection.myTeamIndex == result && TeamSelection.isTeamMate)
            {
                confirm(true);
            }
        }
        else if (code == annullaEvent)
        {
            Debug.LogError("Recived annullaEvent");
            int result = (int)photonEvent.CustomData;
            if (TeamSelection.myTeamIndex == result && TeamSelection.isTeamMate)
            {
                annulla(true);
            }
        }
        /*
        else if (code == setFirstSprintEvent)
        {
            object[] result = (object[])photonEvent.CustomData;
            int team = (int)result[0];
            byte impact = (byte)result[1];
            string risk = (string)result[2];
            int points = (int)result[3];
            int sprint = (int)result[4];
            if (TeamSelection.myTeamIndex == team && TeamSelection.isTeamMate)
            {
                GameMechanics.sprintNumber = sprint;
                Debug.LogError("Recived set first sprint event sprintNumber " + GameMechanics.sprintNumber);
                setFirstSprint(impact, risk, points, true);
            }
        }
        else if (code == setSecondSprintEvent)
        {
            object[] result = (object[])photonEvent.CustomData;
            int team = (int)result[0];
            byte impact = (byte)result[1];
            string risk = (string)result[2];
            int points = (int)result[3];
            int sprint = (int)result[4];
            if (TeamSelection.myTeamIndex == team && TeamSelection.isTeamMate)
            {
                GameMechanics.sprintNumber = sprint;
                Debug.LogError("Recived set second sprint event sprintNumber " + GameMechanics.sprintNumber);
                setSecondSprint(impact, risk, points, true);
            }
        }
        else if (code == setThirdSprintEvent)
        {
            Debug.LogError("Recived set third sprint event sprintNumber " + GameMechanics.sprintNumber);
            object[] result = (object[])photonEvent.CustomData;
            int team = (int)result[0];
            byte impact = (byte)result[1];
            string risk = (string)result[2];
            int points = (int)result[3];
            int sprint = (int)result[4];
            if (TeamSelection.myTeamIndex == team && TeamSelection.isTeamMate)
            {
                GameMechanics.sprintNumber = sprint;
                Debug.LogError("Recived set third sprint event sprintNumber " + GameMechanics.sprintNumber);
                setThirdSprint(impact, risk, points, true);
            }
        }*/
    }

    public void clearUGS()
    {
        Debug.LogError("clearUGS: compilationTerminated "+ compilationTerminated);
        compilationTerminated = false;
        for (int k = 0; k <= resources.Length-1; k++)
        {
            Debug.LogError("clearUGS: examinating resources_go["+k+"]");
            resources_go[k].GetComponent<Image>().sprite = tokenSprites[3];
            Debug.LogError("clearUGS: celaring resources_go[" + k + "] text");
            resources_go[k].GetComponentInChildren<TMP_Text>().text = "";
        }
        for (int m = 0; m <= 8; m++)
        {
            sprintData[m].GetComponent<TMP_Text>().text = "";
            Debug.LogError("clearUGS: sprintData[" + m + "]");
        }
        /*GameObject.Find("Conferma").GetComponent<Button>().interactable = true;
        GameObject.Find("Annulla").GetComponent<Button>().interactable = true;*/
        resources = new int[]{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 };
        Debug.LogError("clearUGS: resetted resources");
        i = 0;
        Debug.LogError("clearUGS: resetted i");
    }
}

