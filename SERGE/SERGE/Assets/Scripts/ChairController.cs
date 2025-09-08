using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using System;
using System.Security.Claims;

public class ChairController : MonoBehaviourPunCallbacks, IOnEventCallback
{
    private bool isBusy;
    public string playerName = "";
    public int actorNumber = -1;
    private readonly byte verifyState = 176;
    private readonly byte chairRotated = 168;
    private readonly byte playerLeftEvent = 166;
    private Quaternion originalRotation;
    private GameObject player = null;
    //public static event Action OnChairIsFreed;
    [SerializeField] private GameObject teamSelection;

    private PhotonView view;
    private void Start()
    {

        originalRotation = transform.rotation;
        //GameMechanics.OnGameFinished += raiseVerifyState;
        //PlayerController.OnGetUp += raiseVerifyState;
        GameMechanics.OnMyPlayerLeftTheRoom += resetEveryChair;
        PlayerController.passPlayerToSeat += assignPlayerToChair;
        isBusy = false;
        //view = this.GetComponent<PhotonView>();
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

    void Update()
    {
        if (isBusy && actorNumber.Equals(PhotonNetwork.LocalPlayer.ActorNumber) && this.gameObject.transform.childCount != 0)
        {
            this.transform.GetChild(0).transform.rotation = player.transform.rotation;
            byte evCode = chairRotated;
            object[] content = new object[5] { player.transform.rotation.x, player.transform.rotation.y, player.transform.rotation.z, player.transform.rotation.w, PhotonNetwork.LocalPlayer.ActorNumber };
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others }; // You would have to set the Receivers to All in order to receive this event on the local client as well
            PhotonNetwork.RaiseEvent(evCode, content, raiseEventOptions, SendOptions.SendUnreliable);
        } 
        else if (!isBusy && this.gameObject.transform.childCount != 0)
        {
            this.gameObject.transform.GetChild(0).rotation = originalRotation;
        }
    }

    public override void OnPlayerLeftRoom(Player player)
    {
        /*if (player.ActorNumber == actorNumber)
        {
            view.RPC("VerifySeatState", RpcTarget.All);
        }*/
    }

    public bool IsBusy()
    {        
        return isBusy;
    }

    public void SetBusy(bool value)
    {
        isBusy = value;
        if (value == false)
        {
            player = null;
        }
    }

    public void assignPlayerToChair(GameObject localPlayer)
    {
        this.player = localPlayer;
    }

    // These RPCs synchronize all the chairs states
    [PunRPC]
    public void VerifySeatState()
    {
        transform.rotation = originalRotation;
        SetBusy(false);
    }
    public void raiseVerifyState()
    {
        byte evCode = verifyState;
        int content = PhotonNetwork.LocalPlayer.ActorNumber;
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All }; // You would have to set the Receivers to All in order to receive this event on the local client as well
        PhotonNetwork.RaiseEvent(evCode, content, raiseEventOptions, SendOptions.SendReliable);
    }
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // This is the owner of the object; send the variable value to other clients
            stream.SendNext(isBusy);
            stream.SendNext(playerName);
        }
        else
        {
            // This is a non-owner client; receive the variable value from the owner
            isBusy = (bool)stream.ReceiveNext();
            playerName = (string)stream.ReceiveNext();
        }
    }
    public void OnEvent(EventData photonEvent)
    {
        byte code = photonEvent.Code;
        if (code == verifyState)
        {
            int actorNumber = (int)photonEvent.CustomData;
            GameObject chairs = null;
            if (TeamSelection.isSoloMode)
            {
                chairs = GameObject.Find("singlemodechairs");
            }
            else
            {
                chairs = GameObject.Find("duomodechairs");
            }
            for (int i = 0; i < 12; i++)
            {
                if (chairs.transform.GetChild(i).GetComponent<ChairController>().actorNumber.Equals(actorNumber))
                {
                    VerifySeatState();
                }
            }
        }
        else if (code == playerLeftEvent)
        {
           /* object[] result = (object[])photonEvent.CustomData;
            int actorNumber = (int)result[0];
            string nickname = (string)result[1];
            int teamIndex = (int)result[2];
            GameObject chairs = null;
            if (TeamSelection.isSoloMode)
            {
                chairs = GameObject.Find("singlemodechairs");
            }
            else
            {
                chairs = GameObject.Find("duomodechairs");
            }
            for (int i = 0; i < 12; i++)
            {
                if (chairs.transform.GetChild(i).GetComponent<ChairController>().actorNumber.Equals(actorNumber) || chairs.transform.GetChild(i).GetComponent<ChairController>().actorNumber.Equals(-1))
                {
                    VerifySeatState();
                }
            }*/
        }
        else if (code == chairRotated)
        {
            //Debug.LogError("Recived chairRotated");
            object[] content = (object[])photonEvent.CustomData;
            int actorNumber = (int)content[4];
            float[] eventContent = { (float)content[0], (float)content[1], (float)content[2], (float)content[3] };
            Quaternion rotation = new Quaternion();
            GameObject chairs = null;
            if (TeamSelection.isSoloMode)
            {
                chairs = GameObject.Find("singlemodechairs");
            }
            else
            {
                chairs = GameObject.Find("duomodechairs");
            }
            for (int i = 0; i < 12;  i++)
            {
                if (chairs.transform.GetChild(i).GetComponent<ChairController>().actorNumber.Equals(actorNumber))
                {
                    chairs.transform.GetChild(i).transform.GetChild(0).transform.rotation = new Quaternion(eventContent[0], eventContent[1], eventContent[2], eventContent[3]);
                }
            }
        }
    }

    void resetEveryChair()
    {
        GameObject chairs = null;
        if (TeamSelection.isSoloMode)
        {
            chairs = GameObject.Find("singlemodechairs");
        }
        else
        {
            chairs = GameObject.Find("duomodechairs");
        }
        for (int i = 0; i < 12; i++)
        {
            VerifySeatState();
        }
    }
}
