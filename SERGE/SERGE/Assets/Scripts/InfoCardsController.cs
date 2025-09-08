using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InfoCardsController : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject image;
    public static bool isVisible = false;
    private int currentImageIndex = 0;
    public Texture[] sprites;
    public static event Action close;
    public GameObject textChatGameObject;
    private TextChat textChat;
    void Start()
    {
        textChat = textChatGameObject.GetComponent<TextChat>();
        UserCardController.close += hide;
        TokenUI.close += hide;
        UserGameSheet.close += hide;
        image.SetActive(false);
    }
    /*
    public void OnEnable()
    {
        textChat = GameObject.Find("TextChat").GetComponent<TextChat>();
    }*/
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1) && !isVisible && !GameMechanics.gameState.Equals(GameState.Premiation) && !textChat.isSelected && !UserGameSheet.isVisible)
        {
            close?.Invoke();
            image.SetActive(true);
            isVisible = !isVisible;
            printImage(currentImageIndex);
        }
        else if (Input.GetKeyDown(KeyCode.F1) && isVisible && !textChat.isSelected && !UserGameSheet.isVisible)
        {
            image.SetActive(false);
            isVisible = !isVisible;
            currentImageIndex = 0;

        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) && isVisible && !textChat.isSelected && !UserGameSheet.isVisible)
        {
            currentImageIndex = (currentImageIndex + 1) % 6;
            printImage(currentImageIndex);
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow) && isVisible && !textChat.isSelected && !UserGameSheet.isVisible)
        {
            if (currentImageIndex == 0)
            {
                currentImageIndex = 5;
            }
            else
            {
                currentImageIndex = (currentImageIndex - 1) % 6;
            }
            printImage(currentImageIndex);
        }
    }

    private void printImage(int index)
    {
        image.GetComponent<RawImage>().texture = sprites[currentImageIndex];
    }

    private void hide()
    {
        isVisible = false;
        image.SetActive(false);
        currentImageIndex = 0;
        close?.Invoke();
    }
}
