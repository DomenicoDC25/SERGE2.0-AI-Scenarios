using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CardController : MonoBehaviour, IOnEventCallback
{
    private readonly byte OnChangedCard = 185;
    private readonly byte terminateGameEvent = 184;
    public Texture[] spriteLevels;
    public static byte probability = 0; //ba aggiornato per l'intero team
    public static byte impact = 0; //va aggiornato per l'intero team
    public GameObject impactCard;
    public GameObject probCard;
    private float previousScroll = 0f; //(mouse)
    //private GameObject cursor;
    //public GameObject pair;
    public Vector3 offset;
    public GameObject impactCard_prefab = null;
    public GameObject probCard_prefab = null;
    public GameObject main_camera;

    public Texture[] Impact_spriteLevels;

    private bool leftMousePressed;
    private bool alteringImpatto = false;
    private bool alteringProbabilità = false;
    private bool scrolledForward = false;
    private bool scrolledBackward = false;

    public static event Action onCardChoosen, rejectVote;

    public static bool[] locked_probabilities = new bool[6];
    public static bool[] locked_impacts = new bool[3];
    

    public static void resetImpactAndProbabilityCards(bool[] array)
    {
        for (int i = 0; i <= array.Length-1; i++)
        {
            array[i] = false;
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
    private void RaisePurchaseEvent()
    {
        byte evCode = OnChangedCard;
        int[] content = new int[3];
        content[0] = TeamSelection.myTeamIndex;
        content[1] = probability;
        content[2] = impact;
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All }; // You would have to set the Receivers to All in order to receive this event on the local client as well
        PhotonNetwork.RaiseEvent(evCode, content, raiseEventOptions, SendOptions.SendReliable);
    }
    /*
    public void InstantiateCards()
    {

        GameObject cards = Instantiate(Resources.Load<GameObject>("Cards"), GameObject.Find("mixamorig:RightHandIndex1").transform.position, PlayerController.playerCam.rotation);
        impactCard_prefab = cards.transform.GetChild(0).gameObject;
        probCard_prefab = cards.transform.GetChild(1).gameObject;
    }
*/

    public void Awake()
    {
        GameMechanics.OnProcurementStateSet += hideCards;
        TeamSelection.OnTeamsReady += showCards;
        //TeamSelection.OnRemovedFromTeam += unsetPhotonViewGroup;
        //TeamSelection.OnTeamSelected += setPhotonViewGroup;
        impactCard.GetComponent<RawImage>().texture = Impact_spriteLevels[0];
        probCard.GetComponent<RawImage>().texture = spriteLevels[0];
        Debug.LogWarning("Non hai scrollato!\n\t impatto è: " + impact);
        Debug.LogWarning("Non hai scrollato!\n\t probabilità è: " + probability);
        this.gameObject.SetActive(false);
        //GameMechanics.OnGameStart += InstantiateCards;
        //this.photonView = this.GetComponent<PhotonView>();
    }

    public void Update()
    {
        checkFronteCartaImpatto();
        checkFronteCartaPercentuale();
        if (!TeamSelection.isTeamMate)
        {
        /* positionig cards on player's hands*/
        if (GameObject.FindGameObjectWithTag("Player") != null && PhotonNetwork.InRoom && impactCard_prefab != null && probCard_prefab != null)
        {
            impactCard_prefab.transform.position = PlayerController.leftHand.position;
            probCard_prefab.transform.position = PlayerController.rightHand.position;
            impactCard_prefab.transform.LookAt(main_camera.transform);
            probCard_prefab.transform.LookAt(main_camera.transform);
        }
        if (locked_probabilities[0] && probability == 0)
        {
            probability = 20;
        }
        if (locked_probabilities[1] && probability == 20)
        {
            probability = 40;
        }
        if (locked_probabilities[2] && probability == 40)
        {
            probability = 60;
        }
        if (locked_probabilities[3] && probability == 60)
        {
            probability = 80;
        }
        if (locked_probabilities[4] && probability == 80)
        {
            probability = 100;
        }
        if (locked_probabilities[5] && probability == 100)
        {
            probability = 0;
        }
        if (locked_impacts[0] && impact == 0)
        {
            impact = 1;
        }
        if (locked_impacts[1] && impact == 1)
        {
            impact = 2;
        }
        if (locked_impacts[2] && impact == 2)
        {
            impact = 0;
        }
        if (Input.GetAxisRaw("Mouse ScrollWheel") > 0f && !leftMousePressed)
        {
            alteringImpatto = true;
            increaseImpatto();

        }
        else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0f && !leftMousePressed)
        {
            alteringImpatto = true;
            decreaseImpatto();    
        }
        else if (Input.GetButtonDown("Fire2") && !leftMousePressed)
        {
            leftMousePressed = true;
            impactCard.transform.position -= new Vector3(0f, 19.8f, 0f);
            probCard.transform.position += new Vector3(0f, 19.8f, 0f);
            Debug.LogWarning("\n\t leftMousePressed is on");
        }
        else if (Input.GetButtonDown("Fire2") && leftMousePressed)
        {
            leftMousePressed = false;
            probCard.transform.position -= new Vector3(0f, 19.8f, 0f);
            impactCard.transform.position += new Vector3(0f, 19.8f, 0f);
            Debug.LogWarning("\n\t leftMousePressed is off");
        }
        else if (Input.GetAxisRaw("Mouse ScrollWheel") > 0f && leftMousePressed)
        {
            increaseProbabilita();
            
        }
        else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0f && leftMousePressed)
        {
            decreaseProbabilita();
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow) && GameMechanics.areCardsSelectable && GameMechanics.gameState.Equals(GameState.RiskAnalysis))
        {
            GameMechanics.areCardsSelectable = false;
            onCardChoosen?.Invoke();
        }
        }
    }
    [PunRPC]
    public void checkFronteCartaPercentuale()
    {
        switch (probability)
        {
            case 0:
                probCard.GetComponent<RawImage>().texture = spriteLevels[0];
                //probCard_prefab.transform.GetChild(0).GetComponent<Renderer>().materials[1].mainTexture = spriteLevels[0];
                break;
            case 20:
                probCard.GetComponent<RawImage>().texture = spriteLevels[1];
                //probCard_prefab.transform.GetChild(0).GetComponent<Renderer>().materials[1].mainTexture = spriteLevels[1];
                break;
            case 40:
                probCard.GetComponent<RawImage>().texture = spriteLevels[2];
                //probCard_prefab.transform.GetChild(0).GetComponent<Renderer>().materials[1].mainTexture = spriteLevels[2];
                break;
            case 60:
                probCard.GetComponent<RawImage>().texture = spriteLevels[3];
                //probCard_prefab.transform.GetChild(0).GetComponent<Renderer>().materials[1].mainTexture = spriteLevels[3];
                break;
            case 80:
                probCard.GetComponent<RawImage>().texture = spriteLevels[4];
                //probCard_prefab.transform.GetChild(0).GetComponent<Renderer>().materials[1].mainTexture = spriteLevels[4];
                break;
            case 100:
                probCard.GetComponent<RawImage>().texture = spriteLevels[5];
                //probCard_prefab.transform.GetChild(0).GetComponent<Renderer>().materials[1].mainTexture = spriteLevels[5];
                break;
        }
    }

    [PunRPC]
    public void increaseProbabilita()
    {
        if (probability == 0)
        {
            probability = 20;
        }
        else if (probability == 20)
        {
            probability = 40;
        }
        else if (probability == 40)
        {
            probability = 60;
        }
        else if (probability == 60)
        {
            probability = 80;
        }
        else if (probability == 80)
        {
            probability = 100;
        }
        else if (probability == 100)
        {
            probability = 0;
        }
        Debug.LogWarning("\n\t probabilità è: " + probability);
        RaisePurchaseEvent();
    }
    [PunRPC]
    public void decreaseProbabilita()
    {
        if (probability == 0)
        {
            probability = 100;
        }
        else if (probability == 20)
        {
            probability = 0;
        }
        else if (probability == 40)
        {
            probability = 20;
        }
        else if (probability == 60)
        {
            probability = 40;
        }
        else if (probability == 80)
        {
            probability = 60;
        }
        else if (probability == 100)
        {
            probability = 80;
        }
        Debug.LogWarning("\n\t probabilità è: " + probability);
        RaisePurchaseEvent();
    }
    [PunRPC]
    public void checkFronteCartaImpatto()
    {
        if (impact == 0)
        {
            //impactCard_prefab.transform.GetChild(0).GetComponent<Renderer>().materials[1].mainTexture = Impact_spriteLevels[0];
            impactCard.GetComponent<RawImage>().texture = Impact_spriteLevels[0];
        }
        else if (impact == 1)
        {
            //impactCard_prefab.transform.GetChild(0).GetComponent<Renderer>().materials[1].mainTexture = Impact_spriteLevels[1];
            impactCard.GetComponent<RawImage>().texture = Impact_spriteLevels[1];
        }
        else
        {
            //impactCard_prefab.transform.GetChild(0).GetComponent<Renderer>().materials[1].mainTexture = Impact_spriteLevels[2];
            impactCard.GetComponent<RawImage>().texture = Impact_spriteLevels[2];
        }
    }
    public void increaseImpatto()
    {
        if (impact == 0)
        {
            impact = 1;
        }
        else if (impact == 1)
        {
            impact = 2;
        }
        else if (impact == 2)
        {
            impact = 0;
        }
        Debug.LogWarning("\n\t impatto è: " + impact);
        RaisePurchaseEvent();
    }
    [PunRPC]
    public void decreaseImpatto()
    {
        if (impact == 0)
        {
            impact = 2;
        }
        else if (impact == 1)
        {
            impact = 0;
        }
        else if (impact == 2)
        {
            impact = 1;
        }
        Debug.LogWarning("\n\t impatto è: " + impact);
        RaisePurchaseEvent();
    }
    public void hideCards()
    {
        GameMechanics.areCardsSelectable = false;
        this.gameObject.SetActive(false);
    }
    public void showCards()
    {
        this.gameObject.SetActive(true);
    }

    public void OnEvent(EventData photonEvent)
    {
        byte code = photonEvent.Code;
        if (code == OnChangedCard)
        {
            Debug.LogError("Recived event " + code);
            int[] result = (int[])photonEvent.CustomData;
            if (TeamSelection.myTeamIndex == result[0] && TeamSelection.isTeamMate)
            {
                probability = (byte)result[1];
                impact = (byte)result[2];
            }
        }
        else if (code == terminateGameEvent)
        {
            hideCards();
        }
    }
}
