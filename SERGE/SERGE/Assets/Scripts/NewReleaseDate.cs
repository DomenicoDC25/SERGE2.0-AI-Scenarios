using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.UIElements.UxmlAttributeDescription;

public class NewReleaseDate : IEventCard
{
    string IEventCard.Name => "New Release Date";

    public static event Action<RiskCard> OnProbabilityAndImpactChanged;
    void IEventCard.modify(Dictionary<byte, List<RiskCard>> board)
    {
        bool firstCard = false;
        bool secondCard = false;
        RiskCard[] cardsToMove = new RiskCard[2];
        for (byte i = 0; i < 3; i++)
        {
            for (byte j = 0; j < board[i].Count; j++)
            {
                if (board[i][j].id.Equals("ID-R1") && !firstCard)
                {
                    board[i][j].decreaseImpatto();
                    board[i][j].decreaseProbabilita();
                    firstCard = true;
                    cardsToMove[0] = board[i][j];
                }
                if (board[i][j].id.Equals("ID-R6") && !secondCard)
                {
                    board[i][j].decreaseImpatto();
                    board[i][j].decreaseProbabilita();
                    secondCard = true;
                    cardsToMove[1] = board[i][j];
                }
                /*
                    switch (board[i][j].id)
                    {
                        case "ID-R1":
                            board[i][j].decreaseImpatto();
                            board[i][j].decreaseProbabilita();
                            OnProbabilityAndImpactChanged?.Invoke(i, j);
                            break;
                        case "ID-R6":
                            board[i][j].decreaseImpatto();
                            board[i][j].decreaseProbabilita();
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