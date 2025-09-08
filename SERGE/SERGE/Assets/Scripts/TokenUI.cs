using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


public class TokenUI : MonoBehaviour
{
    // Start is called before the first frame update
    //public Animator[] token_animators;
    //public AnimatorController ac;
    public static bool isVisible;
    public static event Action close;
    void Start()
    {
        DontDestroyOnLoad(this.gameObject);
        PlayerController.showUGS += showTokens;
        GameMechanics.OnProcurementStateSet += showTokens;
        PlayerController.hideUGS += hideTokens;
        InfoCardsController.close += hideTokens;
        UserCardController.close += hideTokens;
        ConnectToServer.hideUGSandTokensOnJoinedRoom += hideTokens;
    }

    // Update is called once per frame
    void Update()
    {
        if (GameMechanics.gameState == GameState.Procurement)
        {
            GameObject.Find("GreenTokensCounter (Text)").GetComponent<TMP_Text>().text = UserGameSheet.actual_Green_Tokens.ToString();
            GameObject.Find("RedTokensCounter (Text)").GetComponent<TMP_Text>().text = UserGameSheet.actual_Red_Tokens.ToString();
            GameObject.Find("YellowTokensCounter (Text)").GetComponent<TMP_Text>().text = UserGameSheet.actual_Yellow_Tokens.ToString();
        }
        else if (GameMechanics.gameState == GameState.RiskManagement || GameMechanics.gameState == GameState.CardReading)
        {
            GameObject.Find("GreenTokensCounter (Text)").GetComponent<TMP_Text>().text = "0";
            GameObject.Find("RedTokensCounter (Text)").GetComponent<TMP_Text>().text = "0";
            GameObject.Find("YellowTokensCounter (Text)").GetComponent<TMP_Text>().text = "0";
            GameObject.Find("GreenTokensCounter (Text) (1)").GetComponent<TMP_Text>().text = UserGameSheet.actual_Green_Tokens.ToString();
            GameObject.Find("RedTokensCounter (Text) (1)").GetComponent<TMP_Text>().text = UserGameSheet.actual_Red_Tokens.ToString();
            GameObject.Find("YellowTokensCounter (Text) (1)").GetComponent<TMP_Text>().text = UserGameSheet.actual_Yellow_Tokens.ToString();
        }
    }
    void showTokens()
    {
            this.gameObject.SetActive(true);
            isVisible = true;
            //close?.Invoke();
    }
    void hideTokens()
    {
        this.gameObject.SetActive(false);
        isVisible = false;
    }
}
