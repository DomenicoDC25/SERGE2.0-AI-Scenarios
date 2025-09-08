using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using Photon.Pun;
using static Photon.Pun.UtilityScripts.PunTeams;
using System;

public class TextChat : MonoBehaviourPunCallbacks
{
    public TMP_InputField inputField;
    public bool isSelected = false;
    private GameObject commandInfo;
    public static event Action<int, int, int> easterEgg;
    public static event Action commandRank;
    private bool isTeam = false;
    private void Start()
    {
        commandInfo = GameObject.Find("CommandInfo");
    }

    public void LateUpdate()
    {
        if(Input.GetKeyUp(KeyCode.Return) && !isSelected)
        {
            isSelected = true;
            // Set the selected GameObject to the input field
            EventSystem.current.SetSelectedGameObject(inputField.gameObject);
            inputField.caretPosition = inputField.text.Length;
            commandInfo.SetActive(false);
        }

        else if(Input.GetKeyUp(KeyCode.Escape) && isSelected)
        {
            isSelected = false;
            // Reset the selected GameObject 
            EventSystem.current.SetSelectedGameObject(null);
            commandInfo.SetActive(true);
        }

        else if (Input.GetKeyUp(KeyCode.Return) && isSelected && inputField.text != "")
        {
            photonView.RPC("SendMessageRpc", RpcTarget.AllBuffered, PhotonNetwork.NickName, inputField.text, TeamSelection.myTeamIndex, PhotonNetwork.LocalPlayer.ActorNumber);
            inputField.text = "";
            isSelected = false;
            EventSystem.current.SetSelectedGameObject(null);
            commandInfo.SetActive(true);
        }
    }

    [PunRPC]
    public void SendMessageRpc(string sender, string msg, int senderTeamIndex, int senderActorNumber)
    {
        byte checkCommand = checkTeamMateCommand(msg);
        if (checkCommand == 1)
        {
            if (msg.Contains("/team"))
            {
                msg = msg.Remove(0, 5);
            }
            msg = msg.Trim();
            string message = $"<color=\"yellow\">{sender}</color>: {msg}";
            if (TeamSelection.myTeamIndex == senderTeamIndex && TeamSelection.myTeamIndex != -1 && msg.Length != 0)
            {
                Logger.Instance.LogInfo(message);
                LogManager.Instance.LogInfo($"{sender} wrote in the chat: \"{msg}\"");
            }
            else if (TeamSelection.myTeamIndex == -1 && senderActorNumber.Equals(PhotonNetwork.LocalPlayer.ActorNumber) && msg.Length != 0)
            {
                Logger.Instance.LogInfo(message);
                LogManager.Instance.LogInfo($"{sender} wrote in the chat: \"{msg}\"");
            }
        }
        else if (checkCommand == 2)
        {
            if (senderActorNumber.Equals(PhotonNetwork.LocalPlayer.ActorNumber))
            {
                easterEgg?.Invoke(99,99,99);
            }
        }
        else if (checkCommand == 3)
        {
            if (msg.Contains("/group"))
            {
                msg = msg.Remove(0, 6);
            }
            msg = msg.Trim();
            if (msg.Length != 0)
            {
                string message = $"<color=\"yellow\">{sender}</color>: {msg}";
                Logger.Instance.LogInfo(message);
                LogManager.Instance.LogInfo($"{sender} wrote in the chat: \"{msg}\"");
            }
        } 
    }
    public byte checkTeamMateCommand(string message)
    {
        if (message.StartsWith("/team"))
        {
            isTeam = true;
            return 1;
        }
        if (message.StartsWith("/taperfade"))
        {
            return 2;
        }
        /*else if (message.StartsWith("/rankTeams"))
        {
            commandRank?.Invoke(); 
            return 4;
        }*/
        if (message.StartsWith("/group"))
        {
            isTeam = false;
            return 3;
        }
        if (isTeam)
        {
            return 1;
        }
        if (!isTeam)
        {
            return 3;
        }
        else return 0;
    }
}