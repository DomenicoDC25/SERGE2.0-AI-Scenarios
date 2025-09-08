using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using Photon.Voice.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Video;

public class DiceBehaviour : MonoBehaviourPun, IOnEventCallback
{
    [SerializeField] GameObject dice;
    private int result;
    public static event Action diceThrown;
    public Vector3[] position_for_players;
    public Quaternion[] rotation_for_players;
    private Quaternion initial_rotation;
    private Vector3 initial_position;
    private Animator animator;
    private AudioSource audioSource;
    private PhotonView view;
    private readonly byte throwTheDice = 173;


    public void Awake()
    {
        //GameMechanics.OnSecondThrow += throwDice;
        GameMechanics.OnFirstThrow += throwDice;
        dice.GetComponent<Renderer>().enabled = false;
        animator = dice.GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        view = GameObject.Find("Dice").GetComponent<PhotonView>();
    }

    public void Update()
    {
    }

    private void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    private void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    public IEnumerator throwAndCheck(int result)
    {
        Debug.LogError("ThrowAndCheck. Dice result is: " + result);
        //check who is the leader and get his index;
        //initial_position = position_for_players[0];
        //initial_rotation = rotation_for_players[0];
        //GameObject go = GameObject.FindGameObjectWithTag("BackCamera");
        //GameObject go.GetComponent<Camera>().enabled = true;
        //yield return new WaitForSeconds(2);//mostra l'animazione
        //go.GetComponent<Camera>().enabled = false;  
        GameObject go = GameObject.FindGameObjectWithTag("DiceCamera"); //stacca sul dado
        dice.GetComponent<Renderer>().enabled = true;   
        go.GetComponent<Camera>().enabled = true;
        playAnimation(result);
        yield return new WaitForSeconds(3.2f);
        go.GetComponent<Camera>().enabled = false;
        Debug.Log("Resetting Dice... ");
        resetDice();
        go = GameObject.FindGameObjectWithTag("MainCamera"); //ritorna sull'utente
        go.GetComponent<Camera>().enabled = true;
    }

    public void remoteThrowDice(int result)
    {
        this.photonView.RPC("throwDice", RpcTarget.All, result);
    }

    [PunRPC]
    public void throwDice(int result)
    {
        Debug.LogError("ThrowDice. Dice result is: "+result);
        StartCoroutine(throwAndCheck(result));
    }

    public void playAnimation(int result)
    {
        setDiceResult(result);
        Debug.LogError("playAnimation. Dice result is: " + result);
        switch (result)
        {
            case 1:
                playSfx();
                animator.Play("Dice1");
                break;
            case 2:
                playSfx();
                animator.Play("Dice2");
                break;
            case 3:
                playSfx();
                animator.Play("Dice3");
                break;
            case 4:
                playSfx();
                animator.Play("Dice4");
                break;
            case 5:
                playSfx();
                animator.Play("Dice5");
                break;
            case 6:
                playSfx();
                animator.Play("Dice6");
                break;
        }
    }

    public void resetDice()
    {
        //chek who is the leader.
        PlayerController.isShaking = false;
        animator.Play("Dice");
        //dice.gameObject.SetActive(true);
    }

    public void setDiceResult(int newresult)
    {
        result = newresult;
    }

    public int getDiceResult()
    {
        return result;
    }

    public void playSfx()
    {
        audioSource.Play();
    }

    public void OnEvent(EventData photonEvent)
    {
        byte code = photonEvent.Code;
        if (code == throwTheDice) //chooseIndex
        {
            int result = (int)photonEvent.CustomData;
            throwDice(result);
        }
    }
}
