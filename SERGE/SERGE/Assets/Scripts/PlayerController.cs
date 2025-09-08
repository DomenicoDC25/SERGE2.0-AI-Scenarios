using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;
using TMPro;
using UnityEngine.EventSystems;
using Photon.Realtime;
using System;
using Photon.Pun.UtilityScripts;
using ExitGames.Client.Photon;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

public class PlayerController : MonoBehaviourPunCallbacks
{
    public static string nickname;
    private readonly byte NotifyStandUp = 175;
    private readonly byte NotifySitDown = 174;
    private readonly byte verifyState = 176;
    // Controls the camera movement
    [Header("Camera")]
    public Transform playerRoot;
    public static Transform playerCam;
    public float cameraSensitivity;
    private float rotX;
    private float rotY;

    [Header("Movement")]
    public CharacterController controller;
    public float speed;
    public float gravity;
    public Transform feet;
    public bool isGrounded;
    Vector3 velocity;

    [Header("Input")]
    public InputAction move;
    public InputAction mouseX;
    public InputAction mouseY;

    // Controls forward and backward movement speed
    private float originalSpeed;
    private float backwardSpeed;

    // Variables for animation control
    private bool isMoving;
    private bool isBackwardMoving;
    private bool isClapping;
    private bool handRaised;
    private bool isWaving;
    private bool isTyping;
    public static bool isShaking;

    public static bool isChoosing;

    private TextChat textChat;
    public TMP_Text volumeIcon;
    public TMP_Text playerName;
    public Transform overhead;
    private string boardText;
    private float idleTime;
    private float handRaiseCooldown;

    // Variables for the sitting control
    private GameObject chair;
    private WhiteBoard whiteBoard;
    private bool isSitting;
    private Vector3 originalPosition;
    private float originalFov;

    public Animator animatorController;
    private TMP_Text interactionInfo;
    private ControlInfoHanlder commandInfo;
    private Vector3 spawnPosition;
    public AudioSource clapSound;

    //Actions
    public static event Action<float> throws;
    public static event Action showUGS, showTokens, stopTalking, finishReading;
    public static event Action hideUGS, hideTokens, OnGetUp, OnGetUpNowQuit;
    public static event Action<int> OnSeat;
    public static event Action<GameObject> passPlayerToSeat;

    //Player's right and left hand
    public static Transform rightHand;
    public static Transform leftHand;
    public static bool stoppedTalking = false;
    [SerializeField] private int actorNumber;

    public int getActorNumber()
    {
        return actorNumber;
    }
    
    public void setActorNumber(int actorNumber)
    {
        this.actorNumber = actorNumber;
    }
    public override void OnEnable()
    {
        move.Enable();
        mouseX.Enable();
        mouseY.Enable();
        textChat = GameObject.Find("TextChat").GetComponent<TextChat>();
        PhotonNetwork.AddCallbackTarget(this);
    }

    public override void OnDisable()
    {
        move.Disable();
        mouseX.Disable();
        mouseY.Disable();
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    public override void OnLeftRoom()
    {
        Destroy(this.gameObject);    
    }

    private void Start()
    {
        //GameMechanics.OnCardExtracted += LookAtCards;
        //GameMechanics.OnGameFinished += GetUp;
        //ConnectToServer.OnLeftTheRoom += GetUp;
        ConnectToServer.OnCreatedOrJoinedRoomActorNumber += setActorNumber;
        GameMechanics.OnGoingToLeave += GetUp;
        //GameMechanics.OnDestroyGameObject += callDestroyGameObjectRPC;
        playerCam = GameObject.FindWithTag("MainCamera").transform;
        nickname = GetComponent<PhotonView>().Controller.NickName;
        playerName.text = GetComponent<PhotonView>().Controller.NickName;
        volumeIcon.text = ""; 
        boardText = $"{DateTime.UtcNow.Date.ToString("MM/dd/yyyy")}";
        whiteBoard = null;
        commandInfo = GameObject.Find("CommandInfo").GetComponent<ControlInfoHanlder>();

        photonView.RPC("NotifySpawnRPC", RpcTarget.All);
        //GameObject.Find("WelcomeAudioSource").GetComponent<AudioSource>().Play();
        GameObject.Find("BgAudioSource").GetComponent<AudioSource>().enabled = false;


        Cursor.lockState = CursorLockMode.Locked;

        // Variables inizialitazion
        controller = GetComponent<CharacterController>();
        idleTime = 0;
        handRaiseCooldown = 10;
        originalSpeed = speed;
        originalFov = playerCam.GetComponent<Camera>().fieldOfView;
        backwardSpeed = originalSpeed - 1.3f;
        handRaised = false;
        isSitting = false;
        isClapping = false;
        isChoosing = false;
        //isShaking = false;

        interactionInfo = GameObject.Find("InteractionInfo").GetComponent<TMP_Text>();
        interactionInfo.text = "";
        spawnPosition = GameObject.Find("SpawnPosition").GetComponent<Transform>().position;

        gameObject.SetActive(false);
        transform.position = spawnPosition + new Vector3((float)(-photonView.ViewID)/10000f, 0, 0);        
        gameObject.SetActive(true);
        rightHand = GameObject.Find("mixamorig:RightHandIndex1").transform;
        leftHand = GameObject.Find("mixamorig:LeftHandIndex1").transform;
    }

    private void Update()
    {
        if (!photonView.IsMine) { return; }
        controller.Move(velocity * Time.deltaTime);
        // Camera Movement
        Vector2 mouseInput = new Vector2(mouseX.ReadValue<float>() * cameraSensitivity, mouseY.ReadValue<float>() * cameraSensitivity);
        rotX -= mouseInput.y;
        rotX = Mathf.Clamp(rotX, -30, +50);

        if(!isSitting)
            rotY += mouseInput.x;

        else if(isSitting & !isTyping)
        {
            rotY += mouseInput.x;
            //rotY = Mathf.Clamp(rotY, -180, +180);
        }

        else if(isSitting & isTyping)
        {
            rotY += mouseInput.x;
            //rotY = Mathf.Clamp(rotY, -10, +10);
        }

        playerRoot.rotation = Quaternion.Euler(0f, rotY, 0f);
        //playerCam.localRotation = Quaternion.Euler(rotX, 0f, 0f);
        playerCam.rotation = Quaternion.Euler(rotX, 0f, 0f);

        // Player Movement
        Vector2 moveInput = move.ReadValue<Vector2>();
        Vector3 moveVelocity = playerRoot.forward * moveInput.y + playerRoot.right * moveInput.x;        
        
        controller.Move(moveVelocity * speed * Time.deltaTime);

        isGrounded = Physics.Raycast(feet.position, feet.TransformDirection(Vector3.down), 0.50f);

        if (isGrounded)
        {
            velocity = new Vector3(0f, -3f, 0f);
        }
        else
        {
            velocity -= gravity * Time.deltaTime * Vector3.up;
        }

        // Player sitting 
        if (Input.GetKeyUp(KeyCode.C) && chair != null && !chair.GetComponent<ChairController>().IsBusy() && !isSitting && !isMoving && !isBackwardMoving && !textChat.isSelected)
        {            
            Seat();
            //OnSeat?.Invoke(chairToTeam(chair.name));
        }
        else if (Input.GetKeyUp(KeyCode.C) && isSitting && !Input.GetKey(KeyCode.W) && 
            !Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.S) && !Input.GetKey(KeyCode.D) && !textChat.isSelected && !isTyping && !GameMechanics.isGameStarted)
        {
            GetUp();
        }

        // Player writing on whiteboard
        if (Input.GetKeyUp(KeyCode.Space) && whiteBoard != null && !whiteBoard.isBeingEdited && !textChat.isSelected)
        {
            EditWhiteboard();
        }

        else if (Input.GetKeyUp(KeyCode.Escape) && whiteBoard != null && whiteBoard.isBeingEdited && 
            Presenter.Instance.writerID == PhotonNetwork.LocalPlayer.UserId && !textChat.isSelected)
        {
            StopEditWhiteboard();
        }

        if (handRaiseCooldown > 0)
            handRaiseCooldown -= Time.deltaTime;

        // If the player presses M, the character raises their hand
        if (Input.GetKeyUp(KeyCode.M) && handRaiseCooldown <= 0 && !textChat.isSelected && !isTyping)
        {
            RaiseHand();
            handRaiseCooldown = 10;
        }

        if (textChat.isSelected || isTyping)
        {
            controller.enabled = false;
        }
        else if (!textChat.isSelected && !isSitting && !isTyping && controller.enabled == false)
        {
            controller.enabled = true;
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) /*&& !PlayerController.isShaking*/ && GameMechanics.isLocalPlayerTurn && !TeamSelection.isTeamMate && !InfoCardsController.isVisible)
        {
            GameMechanics.isLocalPlayerTurn = false;
            float result = UnityEngine.Random.Range(0f,1f);
            Debug.LogError("PlayerController. Dice value: "+result);
            throws?.Invoke(result);
            isShaking = true;
        }
        else if (Input.GetKeyDown(KeyCode.Backslash) && (GameMechanics.gameState > GameState.Procurement || UserGameSheet.compilationTerminated) && !UserGameSheet.isVisible)
        {
            showUGS?.Invoke();
        }
        else if (Input.GetKeyDown(KeyCode.Backslash) && (GameMechanics.gameState > GameState.Procurement || UserGameSheet.compilationTerminated) && UserGameSheet.isVisible)
        {
            hideUGS?.Invoke();
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow) && GameMechanics.isMyTurnToTalk && GameMechanics.gameState.Equals(GameState.PlanningPoker) && !TeamSelection.isTeamMate)
        {
            GameMechanics.isMyTurnToTalk = false;
            stopTalking?.Invoke();
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow) && GameMechanics.gameState.Equals(GameState.CardReading) /*&& !TeamSelection.isTeamMate*/ && !GameMechanics.finishedReading)
        {
            Debug.LogError("Player Controller: Finished Reading");
            GameMechanics.finishedReading = true;
            finishReading?.Invoke();
        }
        /*
        else if (Input.GetKeyDown(KeyCode.LeftControl) && GameMechanics.gameState == GameState.RiskManagement && !TokenUI.isVisible)
        {
            showTokens?.Invoke();

        }
        else if (Input.GetKeyDown(KeyCode.LeftControl) && GameMechanics.gameState == GameState.RiskManagement && TokenUI.isVisible)
        {
            hideTokens?.Invoke();
        }*/
        if (UserGameSheet.isVisible)
        {
            move.Disable();
            mouseX.Disable();
            mouseY.Disable();
        }
        else if (!UserGameSheet.isVisible)
        {
            move.Enable();
            mouseX.Enable();
            mouseY.Enable();
        }
        AnimatorChecker(moveVelocity);
        InteractionInfoUpdate();
    }

    private void LateUpdate()
    {
        // Locks and unlocks the mouse if the player press ESC or the right mouse button
        if (Input.GetKeyUp(KeyCode.Escape))
            Cursor.lockState = CursorLockMode.None;

        if ((Cursor.lockState == CursorLockMode.None) && Input.GetMouseButton(1))
            Cursor.lockState = CursorLockMode.Locked;

        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyUp(KeyCode.L))
        {
            string msg = "";
            Logger.Instance.LogInfo($"Player in the room: {PhotonNetwork.PlayerList.Length}");
            foreach (Player p in PhotonNetwork.PlayerList)
                msg += " " + p.NickName;
            Logger.Instance.LogInfo($"Players: {msg}");
        }

            GetComponent<AudioSource>().enabled = (isMoving || isBackwardMoving) && !isSitting;
    }

    void AnimatorChecker(Vector3 moveVelocity)
    {
        isMoving = ((moveVelocity.x != 0 || moveVelocity.y != 0 || moveVelocity.z != 0) && !textChat.isSelected && !isTyping);
        isBackwardMoving = false;
        handRaised = false;
        isWaving = false;
        isClapping = false;
        isChoosing = false;
        isTyping = /*(GetComponent<TabletSpawner>().tablet.GetComponent<TabletManager>().isBeingEdited) || */(PhotonNetwork.LocalPlayer.UserId == Presenter.Instance.writerID);



        // If the player is walking backward, this changes the animation and slows down the speed
        if (Input.GetKey(KeyCode.S) && !textChat.isSelected && !isTyping)
        {
            isBackwardMoving = true;
            isMoving = false;
            animatorController.SetBool("IsMovingBackward", true);
            speed = backwardSpeed;

        }

        // When the backward walking is done, it brings the original values back
        else
        {
            isBackwardMoving = false;
            speed = originalSpeed;
            animatorController.SetBool("IsMovingBackward", false);
        }

        if (Input.GetKey(KeyCode.M) && handRaiseCooldown <= 0 && !textChat.isSelected && !isTyping)
        {
            //handRaised = true;
        }

        if (Input.GetKey(KeyCode.N) && !textChat.isSelected && !isTyping)
        {
            isWaving = true;
        }

        if (Input.GetKey(KeyCode.V) && !textChat.isSelected && !isTyping)
        {
            isClapping = true;
        }
        if (isTyping)
        {
            textChat.inputField.enabled = false;
        }

        else if(!isTyping)
        {
            textChat.inputField.enabled = true;
        }

        // If the player doesn't move for 6 seconds, perform an idle animation
        idleTime += Time.deltaTime;
        if (isMoving || isBackwardMoving)
        {
            idleTime = 0;
            animatorController.SetBool("LongPause", false);
        }

        animatorController.SetBool("LongPause", idleTime >= 30);

        if (idleTime >= 30)
        {
            idleTime = 0;
        }

        animatorController.SetBool("IsMoving", isMoving);        
        animatorController.SetBool("HandRaised", handRaised);
        animatorController.SetBool("IsWaving", isWaving);
        animatorController.SetBool("IsClapping", isClapping);
        animatorController.SetBool("IsTalking", GetComponent<PlayerVoiceController>().isTalking);
        //animatorController.SetBool("IsWriting", isTyping);

        if (photonView.GetComponent<PlayerVoiceController>().isTalking)
            photonView.RPC("NotifyTalkRPC", RpcTarget.All, "<sprite index=0>");
        else photonView.RPC("NotifyTalkRPC", RpcTarget.All, "");

        photonView.RPC("ClapRPC", RpcTarget.All, isClapping);
    }

    private void Seat()
    {
        GameObject.Find("SitAudioSource").GetComponent<AudioSource>().Play();

        // The chair is set to busy and the player who is occupying it is saved
        GetComponent<PhotonView>().RPC("NotifySitting", RpcTarget.All, true, GetComponent<PhotonView>().Controller.NickName, GetComponent<PhotonView>().Controller.ActorNumber);

        // Saves original player position for when they get up
        originalPosition = transform.position;

        // Makes the player position move on the chair
        gameObject.SetActive(false);
        string mode = (string)PhotonNetwork.CurrentRoom.CustomProperties["g"];
        if (mode.Equals("SINGLE"))
        {
            transform.position = chair.transform.position + new Vector3(0, +0.4f, +0.1f);
            playerCam.GetComponent<Camera>().fieldOfView -= 10f;
        }
        else
        {
            transform.position = chair.transform.position + new Vector3(0, +0.66f, +0.1f);
            playerCam.GetComponent<Camera>().fieldOfView -= 10f;
        }

        GetComponent<CharacterController>().enabled = false;
        //overhead.position += new Vector3(0, -0.5f, 0);
        gameObject.SetActive(true);

        // Starts the sitting animation for the player
        isSitting = true;
        animatorController.SetBool("IsSitting", true);
        //Tablet spawn
        //GetComponent<TabletSpawner>().SetTabletActive(true, transform.position + new Vector3(-0.05f, 0, 0.5f));
        int team = chairToTeam(chair.name);
        OnSeat?.Invoke(team);
        passPlayerToSeat?.Invoke(this.gameObject);
    }    
    
    public bool getIsSitting()
    {
        return isSitting;
    }
    public void GetUp()
    {
        if (isSitting) { 
            OnGetUp?.Invoke(); //removes the player from his team and from his chair.
            GameObject.Find("SitAudioSource").GetComponent<AudioSource>().Play();
            // The chair is set to free and the player who was occupying it is deleted
            GetComponent<PhotonView>().RPC("NotifySitting", RpcTarget.All, false, "", -1);
            // Makes the player position the original one


            //gameObject.SetActive(false);

            transform.position = originalPosition;
            playerCam.GetComponent<Camera>().fieldOfView = originalFov;
            GetComponent<CharacterController>().enabled = true;
            //overhead.position += new Vector3(0, 0.5f, 0);

            //gameObject.SetActive(true);

            // Stops the sitting animation for the player
            isSitting = false;
            animatorController.SetBool("IsSitting", false);
            chair = null;
            OnGetUpNowQuit?.Invoke();
        }
        else
        {
            OnGetUpNowQuit?.Invoke();
        }
        //Tablet despawn
        //GetComponent<TabletSpawner>().SetTabletActive(false, transform.position);
        /*catch (MissingReferenceException m)
        {
            // The chair is set to free and the player who was occupying it is deleted
            byte evCode = verifyState;
            bool content = false;
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All }; // You would have to set the Receivers to All in order to receive this event on the local client as well
            PhotonNetwork.RaiseEvent(evCode, content, raiseEventOptions, SendOptions.SendReliable);
            // Makes the player position the original one
            gameObject.SetActive(false);
            transform.position = originalPosition;
            playerCam.GetComponent<Camera>().fieldOfView = originalFov;
            GetComponent<CharacterController>().enabled = true;
            //overhead.position += new Vector3(0, 0.5f, 0);
            gameObject.SetActive(true);
            // Stops the sitting animation for the player
            isSitting = false;
            animatorController.SetBool("IsSitting", false);
            chair = null;
            //Tablet despawn
            //GetComponent<TabletSpawner>().SetTabletActive(false, transform.position);
        }*/

    }
    private void DestroyThisGameObject()
    {
        Destroy(this.gameObject);
    }
    private void EditWhiteboard()
    {
        whiteBoard.boardText.readOnly = false; 
        EventSystem.current.SetSelectedGameObject(whiteBoard.gameObject);
        Cursor.lockState = CursorLockMode.None;
        whiteBoard.boardText.caretPosition = whiteBoard.boardText.text.Length;
        GetComponent<CharacterController>().enabled = false;
        commandInfo.enabled = false;
        photonView.RPC("LockBoard", RpcTarget.All, true, PhotonNetwork.LocalPlayer.UserId, "");
    }

    private void StopEditWhiteboard()
    {
        whiteBoard.boardText.readOnly = true;
        EventSystem.current.SetSelectedGameObject(null);
        Cursor.lockState = CursorLockMode.Locked;
        GetComponent<CharacterController>().enabled = true;
        boardText = whiteBoard.boardText.text;
        commandInfo.enabled = true;
        photonView.RPC("LockBoard", RpcTarget.All, false, "none", boardText);
    }
    private void RaiseHand()
    {
        //GetComponent<PhotonView>().RPC("NotifyHandRaisedRPC", RpcTarget.All);
    }

    // Checks if the player is near to a chair to sit on
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Chair"))
        {
            chair = collision.gameObject;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Chair"))
        {
            chair = null;
        }
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.CompareTag("Whiteboard"))
        {
            whiteBoard = collision.gameObject.GetComponent<WhiteBoard>();
        }
    }
    private void OnTriggerExit(Collider collision)
    {
        if (collision.gameObject.CompareTag("Whiteboard"))
        {
            whiteBoard = null;
        }
    }

    private void InteractionInfoUpdate()
    {
        if (chair != null && !isSitting && !chair.GetComponent<ChairController>().IsBusy())
            interactionInfo.text = "Press C to sit";
           
        else if (isSitting && !isTyping && !GameMechanics.isGameStarted)
            interactionInfo.text = "Press C to stand up";

        else if(isSitting && isTyping)
            interactionInfo.text = "Press ESC to stop writing";

        else if (chair != null && !isSitting && chair.GetComponent<ChairController>().IsBusy())
            interactionInfo.text = "Chair is occupied";

        else if(whiteBoard != null && !whiteBoard.isBeingEdited)
            interactionInfo.text = "Press SPACE to start writing on the whiteboard";

        else if (whiteBoard != null && whiteBoard.isBeingEdited && Presenter.Instance.writerID == PhotonNetwork.LocalPlayer.UserId)
            interactionInfo.text = "Press ESC to stop writing";

        else if (whiteBoard != null && whiteBoard.isBeingEdited && Presenter.Instance.writerID != PhotonNetwork.LocalPlayer.UserId)
            interactionInfo.text = "Whiteboard is busy";

        else 
            interactionInfo.text = "";
    }

    [PunRPC]
    public void NotifySitting(bool value, string playerName, int playerActorNumber)
    {
        if (chair != null)
        {
            chair.GetComponent<ChairController>().SetBusy(value);
            chair.GetComponent<ChairController>().playerName = playerName;
            chair.GetComponent<ChairController>().actorNumber = playerActorNumber;
        }            
    }

    [PunRPC]
    public void NotifyHandRaisedRPC()
    {
        string msg = GetComponent<PhotonView>().Controller.NickName + " raised a hand!";
        Logger.Instance.LogInfo(msg);
        LogManager.Instance.LogInfo(msg);
    }

    [PunRPC]
    public void NotifySpawnRPC()
    {
        Logger.Instance.LogInfo($"<color=yellow>{GetComponent<PhotonView>().Controller.NickName}</color> just joined the room!");
        LogManager.Instance.LogInfo($"{GetComponent<PhotonView>().Controller.NickName} joined the room");
        GameObject.Find("SpawnAudioSource").GetComponent<AudioSource>().Play();
    }

    [PunRPC]
    public void NotifyTalkRPC(string msg)
    {
        volumeIcon.text = msg;
    }

    [PunRPC]
    public void ClapRPC(bool value)
    {
        clapSound.enabled = value;
    }

    [PunRPC]
    public void LockBoard(bool value, string id, string text)
    {
        if (whiteBoard == null) return;
        whiteBoard.isBeingEdited = value;
        Presenter.Instance.writerID = id;

        if (!value)
        {
            whiteBoard.boardText.text = text;
            LogManager.Instance.LogWhiteboard(text);
        }
    }
    /*
    private void LookAtCards()
    {
        isChoosing = true;
        animatorController.SetBool("IsChoosing", isChoosing);
    }

    private void doThrowDiceAnimation()
    {
        animatorController.SetTrigger("IsShaking");
    }*/

    public void setRotationY(float rotation)
    {
        rotY = rotation;
    }

    public float getRotationY()
    {
        return rotY;
    } 

    public int chairToTeam(string name)
    {
        int result = -1;  
        string mode = (string)PhotonNetwork.CurrentRoom.CustomProperties["g"];
        if (mode.Equals("DUO"))
        {
            if (name.Equals("chairs.000"))
            {
                result = 0;
            }
            else if (name.Equals("chairs.001"))
            {
                result = 1;
            }
            else if (name.Equals("chairs.002"))
            {
                result = 2;
            }
            else if (name.Equals("chairs.003"))
            {
                result = 3;
            }
            else if (name.Equals("chairs.004"))
            {
                result = 4;
            }
            else if (name.Equals("chairs.005"))
            {
                result = 5;
            }
            else if (name.Equals("chairs.000C"))
            {
                result = 0;
            }
            else if (name.Equals("chairs.001C"))
            {
                result = 1;
            }
            else if (name.Equals("chairs.002C"))
            {
                result = 2;
            }
            else if (name.Equals("chairs.003C"))
            {
                result = 3;
            }
            else if (name.Equals("chairs.004C"))
            {
                result = 4;
            }
            else if (name.Equals("chairs.005C"))
            {
                result = 5;
            }
        }
        else
        {
            if (name.Equals("chairs.000"))
            {
                result = 0;
            }
            else if (name.Equals("chairs.001"))
            {
                result = 2;
            }
            else if (name.Equals("chairs.002"))
            {
                result = 4;
            }
            else if (name.Equals("chairs.003"))
            {
                result = 6;
            }
            else if (name.Equals("chairs.004"))
            {
                result = 8;
            }
            else if (name.Equals("chairs.005"))
            {
                result = 10;
            }
            else if (name.Equals("chairs.000C"))
            {
                result = 1;
            }
            else if (name.Equals("chairs.001C"))
            {
                result = 3;
            }
            else if (name.Equals("chairs.002C"))
            {
                result = 5;
            }
            else if (name.Equals("chairs.003C"))
            {
                result = 7;
            }
            else if (name.Equals("chairs.004C"))
            {
                result = 9;
            }
            else if (name.Equals("chairs.005C"))
            {
                result = 11;
            }
        }
        return result;
    }
    public void OnEvent(EventData photonEvent)
    {
        byte code = photonEvent.Code;
        if (code == NotifySitDown)
        {
            Debug.LogError("Recived event NotifySitDown");
            object[] result = (object[])photonEvent.CustomData;
            string playerName = (string)result[0];
            int playerActorNumber = (int)result[1];
            NotifySitting(true, playerName, playerActorNumber);
        }
        else if (code == NotifyStandUp)
        {
            Debug.LogError("Recived event NotifyStandUp");
            object[] result = (object[])photonEvent.CustomData;
            string playerName = (string)result[0];
            int playerActorNumber = (int)result[1];
            NotifySitting(false, playerName, playerActorNumber);
        }
    }
}
