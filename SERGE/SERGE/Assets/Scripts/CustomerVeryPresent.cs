using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.UIElements.UxmlAttributeDescription;

public class CustomerVeryPresent : IEventCard
{
    public static event Action<RiskCard> OnProbabilityAndImpactChanged;

    string IEventCard.Name => "Customer very present";

    void IEventCard.modify(Dictionary<byte, List<RiskCard>> board)
    {
        bool firstCard = false;
        bool secondCard = false;
        bool thirdCard = false;
        RiskCard[] cardsToMove = new RiskCard[3];
        for (byte i = 0; i < 3; i++)
        {
            Debug.LogError("board[2].Count "+ board[2].Count);
            for (int j = 0; j < board[i].Count; j++)
            {
                Debug.Log($"Esaminando carta {board[i][j].id}");
                if (board[i][j].id.Equals("ID-R3") && !firstCard)
                {
                    board[i][j].decreaseImpatto();
                    board[i][j].decreaseProbabilita();
                    firstCard = true;
                    cardsToMove[0] = board[i][j];
                }
                if (board[i][j].id.Equals("ID-R7") && !secondCard)
                {
                    board[i][j].decreaseImpatto();
                    board[i][j].decreaseProbabilita();
                    secondCard = true;
                    cardsToMove[1] = board[i][j];
                }
                if (board[i][j].id.Equals("ID-R1") && !thirdCard)
                {
                    board[i][j].decreaseImpatto();
                    board[i][j].decreaseProbabilita();
                    thirdCard = true;
                    cardsToMove[2] = board[i][j];
                }
            }
        }
        OnProbabilityAndImpactChanged?.Invoke(cardsToMove[0]);
        OnProbabilityAndImpactChanged?.Invoke(cardsToMove[1]);
        OnProbabilityAndImpactChanged?.Invoke(cardsToMove[2]);
    }
}