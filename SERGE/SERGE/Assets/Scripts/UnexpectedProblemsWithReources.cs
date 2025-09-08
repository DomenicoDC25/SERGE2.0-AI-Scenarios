using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.UIElements.UxmlAttributeDescription;
public class UnexpectedProblemsWithReources : IEventCard
{
    string IEventCard.Name => "Unexpected Problems with Resources";

    public static event Action<RiskCard> OnProbabilityAndImpactChanged;
    void IEventCard.modify(Dictionary<byte, List<RiskCard>> board)
    {
        bool firstCard = false;
        RiskCard cards = null;
        for (byte i = 0; i < 3; i++)
        {
            for (byte j = 0; j < board[i].Count; j++)
            {
                if (board[i][j].id.Equals("ID-R1") && !firstCard)
                {
                    board[i][j].increaseImpatto();
                    board[i][j].increaseProbabilita();
                    cards = board[i][j];
                    firstCard = true;
                }
                /*
                    switch (board[i][j].id)
                    {
                        case "ID-R1":
                            board[i][j].increaseImpatto();
                            board[i][j].increaseProbabilita();
                            OnProbabilityAndImpactChanged?.Invoke(i, j);
                            break;
                    }
                */
            }
        }
        OnProbabilityAndImpactChanged?.Invoke(cards);
    }
}