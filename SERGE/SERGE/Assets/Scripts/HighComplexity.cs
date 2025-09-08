using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.UIElements.UxmlAttributeDescription;

public class HighComplexity : IEventCard
{
    public static event Action<RiskCard> OnProbabilityAndImpactChanged;

    string IEventCard.Name => "High Complexity";

    void IEventCard.modify(Dictionary<byte, List<RiskCard>> board)
    {
        bool firstCard = false;
        bool secondCard = false;
        RiskCard[] cardsToMove = new RiskCard[2];
        for (byte i = 0; i < 3; i++)
        {
            for (byte j = 0; j < board[i].Count; j++)
            {
                if (board[i][j].id.Equals("ID-R8") && !firstCard)
                {
                    board[i][j].increaseImpatto();
                    board[i][j].increaseProbabilita();
                    firstCard = true;
                    cardsToMove[0] = board[i][j];
                }
                if (board[i][j].id.Equals("ID-R5") && !secondCard)
                {
                    board[i][j].increaseImpatto();
                    board[i][j].increaseProbabilita();
                    secondCard = true;
                    cardsToMove[1] = board[i][j];
                }
                /*
                    switch (board[i][j].id)
                    {
                        case "ID-R8":
                            board[i][j].increaseImpatto();
                            board[i][j].increaseProbabilita();
                            OnProbabilityAndImpactChanged?.Invoke(i, j);
                            break;
                        case "ID-R5":
                            board[i][j].increaseImpatto();
                            board[i][j].increaseProbabilita();
                            OnProbabilityAndImpactChanged?.Invoke(i, j);
                            break;
                    }
                */
            }
        }
        OnProbabilityAndImpactChanged?.Invoke(cardsToMove[0]);
        OnProbabilityAndImpactChanged?.Invoke(cardsToMove[1]);
    }
}