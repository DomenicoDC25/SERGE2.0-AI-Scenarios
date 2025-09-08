using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserCardController : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject card;
    private bool isKeyPressed = false;
    public static event Action close;
    public GameObject textChatGameObject;
    private TextChat textChat;
    void Start()
    {
        textChat = textChatGameObject.GetComponent<TextChat>();
        InfoCardsController.close += hide;
        TokenUI.close += hide;
        UserGameSheet.close += hide;
        card.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F2) && isKeyPressed && !textChat.isSelected && !UserGameSheet.isVisible)
        {
            isKeyPressed = !isKeyPressed;
            card.SetActive(false);
        }
        else if (Input.GetKeyDown(KeyCode.F2) && !isKeyPressed && !GameMechanics.gameState.Equals(GameState.Premiation) && !textChat.isSelected && !UserGameSheet.isVisible)
        {
            close?.Invoke();
            isKeyPressed = !isKeyPressed;
            card.SetActive(true);
        }
    }

    private void hide()
    {
        isKeyPressed=false;
        card.SetActive(false);  
    }
}
