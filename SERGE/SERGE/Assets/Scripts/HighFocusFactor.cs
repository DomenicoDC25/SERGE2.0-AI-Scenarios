using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.UIElements.UxmlAttributeDescription;

public class HighFocusFactor : IEventCard
{
    public static event Action<RiskCard> OnProbabilityAndImpactChanged;

    string IEventCard.Name => "High Focus Factor";

    void IEventCard.modify(Dictionary<byte, List<RiskCard>> board)
    {
        bool firstCard = false;
        bool secondCard = false;
        RiskCard[] riskCards = new RiskCard[2];
        for (byte i = 0; i < 3; i++)
        {
            for (byte j = 0; j < board[i].Count; j++)
            {
                if (board[i][j].id.Equals("ID-R1") && !firstCard)
                {
                    board[i][j].decreaseImpatto();
                    board[i][j].decreaseProbabilita();
                    firstCard = true;
                    riskCards[0] = board[i][j];
                }
                else if (board[i][j].id.Equals("ID-R2") && !secondCard)
                {
                    board[i][j].decreaseImpatto();
                    board[i][j].decreaseProbabilita();
                    secondCard = true;
                    riskCards[1] = board[i][j];
                }
                /*
                    switch (board[i][j].id)
                    {
                        case "ID-R1":
                            board[i][j].decreaseImpatto();
                            board[i][j].decreaseProbabilita();
                            OnProbabilityAndImpactChanged?.Invoke(i, j);
                            break;
                        case "ID-R2":
                            board[i][j].decreaseImpatto();
                            board[i][j].decreaseProbabilita();
                            OnProbabilityAndImpactChanged?.Invoke(i, j);
                            break;
                    }
                */
            }
        }
        OnProbabilityAndImpactChanged?.Invoke(riskCards[0]);
        OnProbabilityAndImpactChanged?.Invoke(riskCards[1]);
    }
}