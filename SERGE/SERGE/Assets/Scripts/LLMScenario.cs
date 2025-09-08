using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using System;

[System.Serializable]
public class ScenarioResponse
{
    public string response;
}

public class LLMScenario : MonoBehaviourPunCallbacks, IOnEventCallback
{
    [Header("UI")]
    public TMP_Text scenarioText;
    public TMP_Text debugText;

    [Header("Server")]
    public string serverURL = "http://127.0.0.1:8000/generate";

    private bool scenarioReady = false;
    private bool isRequestInProgress = false;
    private string scenario = "";
    private string scenarioToDisplay = "";

    private const byte ScenarioEventCode = 1;

    void Update()
    {
        if (!string.IsNullOrEmpty(scenarioToDisplay))
        {
            scenarioText.text = scenarioToDisplay;
            scenarioToDisplay = "";
        }
    }

    void Start()
    {
        Log("LLMScenario Start chiamato!");
    }

    public override void OnJoinedRoom()
    {
        Log("Entrato nella stanza Photon");

        if (PhotonNetwork.IsMasterClient)
        {
            Log("Sono il MasterClient → genero scenario...");
            StartCoroutine(RequestScenario());
        }
        else
        {
            Log("Sono un client → attendo scenario dall’host...");

            // Se lo scenario è già stato generato prima del mio ingresso,
            // posso recuperarlo dalle Room Properties
            if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("scenario"))
            {
                string existingScenario = (string)PhotonNetwork.CurrentRoom.CustomProperties["scenario"];
                ApplyScenario(existingScenario, "Ricevuto scenario dalle Room Properties");
            }
        }
    }

    IEnumerator RequestScenario()
    {
        isRequestInProgress = true;

        WWWForm form = new WWWForm();
        form.AddField("prompt", "Generami uno scenario di progetto");

        using (UnityWebRequest www = UnityWebRequest.Post(serverURL, form))
        {
            yield return www.SendWebRequest();
            isRequestInProgress = false;

            if (www.result != UnityWebRequest.Result.Success)
            {
                LogError("Errore nella richiesta: " + www.error);
                scenarioToDisplay = "Server IA non disponibile. Impossibile generare lo scenario.";
                scenarioReady = false;
            }
            else
            {
                string responseText = www.downloadHandler.text;
                ScenarioResponse scenarioResp = JsonUtility.FromJson<ScenarioResponse>(responseText);

                string cleaned = scenarioResp.response.Replace("Generami uno scenario di progetto", "").Trim();
                scenario = cleaned.Replace("\\n", Environment.NewLine);

                ApplyScenario(scenario, "Scenario generato dall’host");

                // Salvo lo scenario nelle Room Properties
                ExitGames.Client.Photon.Hashtable roomProps = new ExitGames.Client.Photon.Hashtable();
                roomProps["scenario"] = scenario;
                PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);

                // Invio lo scenario anche via RaiseEvent (per i client già connessi)
                object content = scenario;
                RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
                SendOptions sendOptions = new SendOptions { Reliability = true };
                PhotonNetwork.RaiseEvent(ScenarioEventCode, content, options, sendOptions);

                Log("Scenario inviato a tutti i client e salvato nelle Room Properties.");
            }
        }
    }

    public void OnEvent(EventData photonEvent)
    {
        Debug.Log("OnEvent chiamato → Code: " + photonEvent.Code);

        if (photonEvent.Code == ScenarioEventCode)
        {
            string receivedScenario = (string)photonEvent.CustomData;
            ApplyScenario(receivedScenario, "Scenario ricevuto dal MasterClient (RaiseEvent)");
        }
    }

    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey("scenario"))
        {
            string receivedScenario = (string)propertiesThatChanged["scenario"];
            ApplyScenario(receivedScenario, "Scenario ricevuto tramite RoomProperties (sync automatico)");
        }
    }

    private void ApplyScenario(string newScenario, string source)
    {
        scenario = newScenario;
        scenarioToDisplay = newScenario;
        scenarioReady = true;
        GameMechanics.scenarioGenerated = true;
        Log($"{source}:\n{newScenario}");
    }

    public void ShowScenario()
    {
        if (scenarioReady)
        {
            scenarioText.text = scenario;
        }
        else if (isRequestInProgress)
        {
            scenarioText.text = "Generazione dello scenario...";
        }
        else
        {
            scenarioText.text = "Attendere lo scenario prima di sedersi...";
        }
    }

    public bool CanSit()
    {
        if (scenarioReady) return true;
        scenarioText.text = isRequestInProgress ? "Generazione dello scenario..." : "Scenario non pronto";
        return false;
    }

    private void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    private void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    // ---------------------------
    // Funzioni di log su schermo
    // ---------------------------
    private void Log(string message)
    {
        Debug.Log(message);
        if (debugText != null) debugText.text = message;
    }

    private void LogError(string message)
    {
        Debug.LogError(message);
        if (debugText != null) debugText.text = "ERROR: " + message;
    }

    public override void OnLeftRoom()
    {
        // Reset dello scenario quando esco dalla stanza
        scenario = "";
        scenarioToDisplay = "";
        scenarioReady = false;
        GameMechanics.scenarioGenerated = false;

        // Aggiorno la UI per rimuovere il testo precedente
        if (scenarioText != null)
            scenarioText.text = "";

        Log("Uscito dalla stanza → scenario resettato");
    }

}
