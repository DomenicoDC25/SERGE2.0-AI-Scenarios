using UnityEngine;
using Photon.Pun;
using TMPro;
using UnityEngine.UI;
using Photon.Realtime;
using UnityEngine.SceneManagement;
using ExitGames.Client.Photon;
using System;
using System.Collections.Generic;
using Photon.Pun.UtilityScripts;
using Unity.VisualScripting;
using Photon.Voice;


public static class SerializableColor
{
    public static byte[] Serialize(object obj)
    {
        Color32 color = (Color32)obj;
        byte[] bytes = new byte[4];
        bytes[0] = color.r;
        bytes[1] = color.g;
        bytes[2] = color.b;
        bytes[3] = color.a;
        return bytes;
    }

    public static object Deserialize(byte[] bytes)
    {
        Color32 color = new Color32();
        color.r = bytes[0];
        color.g = bytes[1];
        color.b = bytes[2];
        color.a = bytes[3];
        return color;
    }
}
public class ConnectToServer : MonoBehaviourPunCallbacks, ILobbyCallbacks
{

    public TMP_InputField nameInputField;
    public TMP_InputField passwordInputField;
    public GameObject initialGUI;
    public GameObject loggedGUI;

    public Button hostButton;
    public Button clientButton;
    public Button quitButton;
    public Button leaveButton;
    public Button editButton;
    public Button singleGameModeButton;
    public Button duoGameModeButton;
    public GameObject exitForm;
    public GameObject modeform;

    public AudioSource buttonClick;

    public PhotonView player;
    //public PhotonView Dice;
    public RoomOptions room;

    public static event Action startGame, OnCreatedOrJoinedRoom, OnLeftTheRoom, hideUGSandTokensOnJoinedRoom; 
    public static event Action<int> OnCreatedOrJoinedRoomActorNumber;

    public const string GAME_MODE_PROPERTY_KEY = "g";
    public string gameMode = "SINGLE";
    private bool didSwitchScene = false;
    [SerializeField] private GameObject solochairs, duochairs;
    public static List<int> PlayersInRoom = null;
    void Update()
    {
        if (didSwitchScene)
        {
            didSwitchScene = false;
            //PhotonNetwork.ConnectUsingSettings();
            initialGUI.SetActive(true);
            modeform.SetActive(false);
            exitForm.SetActive(false);
        }
        if (PhotonNetwork.CurrentRoom != null)
        {
            GameObject.FindGameObjectWithTag("MODE").GetComponent<TMP_Text>().text = (string)PhotonNetwork.CurrentRoom.CustomProperties["g"] + " MODE";
            GameObject[] playerObjects = GameObject.FindGameObjectsWithTag("Player");
            foreach (GameObject obj in playerObjects)
            {
                PhotonView pv = obj.GetComponent<PhotonView>();
                if (pv.CreatorActorNr == 0)
                {
                    PhotonNetwork.Destroy(obj);
                    Debug.Log("Manually destroyed player object for an actor number");
                }
            }
        }
        else if (PhotonNetwork.CurrentRoom == null)
        {
            GameObject.FindGameObjectWithTag("MODE").GetComponent<TMP_Text>().text = gameMode + " MODE";
        }
    }
    void Start()
    {
        modeform.SetActive(false);
        exitForm.SetActive(false);
        //GameMechanics.OnGoingToLeave += Leave;
        PlayerController.OnGetUpNowQuit += Leave;
        EditorManager.OnSceneSwitched += setDidSwitchScene;
        PhotonNetwork.ConnectUsingSettings();
        loggedGUI.SetActive(false);
        //clientButton.enabled = false;
        //hostButton.enabled = false;
        hostButton.onClick.AddListener(() => {
            buttonClick.Play();
            if (nameInputField.text == "")
            {
                Logger.Instance.LogError("You must enter a name!");
                nameInputField.placeholder.GetComponent<TMP_Text>().text = "You must enter a name!";
                return;
            }
            else if (passwordInputField.text == "")
            {
                Logger.Instance.LogError("You must enter a password!");
                passwordInputField.placeholder.GetComponent<TMP_Text>().text = "You must enter a password!";
                return;
            }
            else
                modeform.SetActive(true);
        });

        clientButton.onClick.AddListener(() => {
            buttonClick.Play();

            if (nameInputField.text == "")
            {
                Logger.Instance.LogError("You must enter a name!");
                nameInputField.placeholder.GetComponent<TMP_Text>().text = "You must enter a name!";
                return;
            }
            else if (passwordInputField.text == "")
            {
                Logger.Instance.LogError("You must enter a password!");
                passwordInputField.placeholder.GetComponent<TMP_Text>().text = "You must enter a password!";
                return;
            }
            else 
                JoinRoom(passwordInputField.text);
        });

        quitButton.onClick.AddListener(() => {
            buttonClick.Play();
            Application.Quit();
        });

        leaveButton.onClick.AddListener(() => {
            buttonClick.Play();
            if (PhotonNetwork.CurrentRoom != null)
            {
                exitForm.SetActive(true);
            } 
            else
            {
                Leave();
            }
            //Leave();
        });

        editButton.onClick.AddListener(() => {
            buttonClick.Play();
            //PhotonNetwork.Disconnect();
            initialGUI.SetActive(false);
            SceneManager.LoadScene(1, LoadSceneMode.Additive);
        });

        singleGameModeButton.onClick.AddListener(() => {
            buttonClick.Play();
            gameMode = "SINGLE";
            solochairs.SetActive(true);
            duochairs.SetActive(false);
            CreateRoom(passwordInputField.text);
        });

        duoGameModeButton.onClick.AddListener(() => {
            buttonClick.Play();
            gameMode = "DUO";
            solochairs.SetActive(false);
            duochairs.SetActive(true);
            CreateRoom(passwordInputField.text);
        });
    }
    private void setDidSwitchScene()
    {
        didSwitchScene = true;
    }
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        //photonView.RPC("NotifyNewMaster", RpcTarget.All, newMasterClient.NickName);
        //OnLeftTheRoom?.Invoke();
    }    

    public override void OnConnectedToMaster()
    {
        Logger.Instance.LogInfo("Connected to Master");
        hostButton.enabled = true;
        clientButton.enabled = true;
        //PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedRoom()
    {
        PhotonNetwork.NickName = nameInputField.text;
        SetCustomProperties();
        OnCreatedOrJoinedRoom?.Invoke();
        OnCreatedOrJoinedRoomActorNumber?.Invoke(PhotonNetwork.LocalPlayer.ActorNumber);
        initialGUI.SetActive(false);
        loggedGUI.SetActive(true);
        GameObject myPlayer = PhotonNetwork.Instantiate(player.name, Vector3.zero, Quaternion.identity);
        Transform playerCam = GameObject.FindWithTag("MainCamera").transform;
        if (playerCam != null)
        {
            CameraController followScript = playerCam.GetComponent<CameraController>();
            if (followScript != null)
            {
                followScript.target = myPlayer;
            }
        }
    }
    private void CreateRoom(string name)
    {
        RoomOptions roomOptions = new RoomOptions {
            BroadcastPropsChangeToAll = true,
            EmptyRoomTtl = 0,
            CleanupCacheOnLeave = true,
            MaxPlayers = 12,
            PlayerTtl = 0,
            IsOpen = true
        };
        roomOptions.CustomRoomProperties = new Hashtable() { { "g", gameMode } };
        PhotonNetwork.CreateRoom(name,roomOptions);
        LogManager.Instance.LogInfo($"{nameInputField.text} created room {name}");
        OnCreatedOrJoinedRoom?.Invoke();
        OnCreatedOrJoinedRoomActorNumber?.Invoke(PhotonNetwork.LocalPlayer.ActorNumber);
        //Presenter.Instance.presenterID = PhotonNetwork.LocalPlayer.UserId;
    }
    private void JoinRoom(string roomName)
    {
        PhotonNetwork.JoinRoom(roomName);

        //hideUGSandTokensOnJoinedRoom?.Invoke(); 
        //OnCreatedOrJoinedRoom?.Invoke();
        //OnCreatedOrJoinedRoomActorNumber?.Invoke(PhotonNetwork.LocalPlayer.ActorNumber);

        LogManager.Instance.LogInfo($"You are actor number " + PhotonNetwork.LocalPlayer.ActorNumber);
    }


    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log("Sono qui aoo");
        PlayersInRoom = null;
        Logger.Instance.LogInfo($"<color=yellow>{otherPlayer.NickName}</color> ha lasciato la stanza.");
        LogManager.Instance.LogInfo($"{otherPlayer.NickName} ha lasciato la stanza.");
    }
    private void Leave()
    {
        if (PhotonNetwork.CurrentRoom != null)
        {
            PhotonNetwork.LeaveRoom(false);
        }
        else
        {
            PhotonNetwork.Disconnect();
        }
        loggedGUI.SetActive(false);
        initialGUI.SetActive(true);
        modeform.SetActive(false);
        nameInputField.placeholder.GetComponent<TMP_Text>().text = "Enter player name...";
        passwordInputField.placeholder.GetComponent<TMP_Text>().text = "Enter password...";
        Cursor.lockState = CursorLockMode.None;
    }
    private void SetCustomProperties()
    {
        PhotonPeer.RegisterType(typeof(Color32), (byte)'C', SerializableColor.Serialize, SerializableColor.Deserialize);
    }
    [PunRPC]
    public void NotifyRpc(string msg)
    {
        Logger.Instance.LogInfo(msg);
    }
    [PunRPC]
    public void NotifyNewMaster(string name)
    {
        Logger.Instance.LogInfo("Old room owner disconnected. New owner is now <color=yellow>" + name + "</color>.");
    }
    public override void OnDisconnected(DisconnectCause cause)
    {
        OnLeftTheRoom?.Invoke();
        PlayersInRoom = null;
        Debug.LogError("Disconnesso: " + cause.ToString());
    }
    
    public void LeaveTheRoom() 
    {
        OnLeftTheRoom?.Invoke();
        //Leave();
        if (exitForm.activeSelf)
        {
            exitForm.SetActive(false);
        }
    }

    public void cancelButton()
    {
        exitForm.SetActive(false);
    }
}
