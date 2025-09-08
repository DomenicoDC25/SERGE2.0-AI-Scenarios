using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.UIElements.UxmlAttributeDescription;

public class ScopeCreep : IEventCard
{
    public static event Action<RiskCard> OnProbabilityAndImpactChanged;

    string IEventCard.Name => "Scope Creep";

    void IEventCard.modify(Dictionary<byte, List<RiskCard>> board)
    {
        bool firstCard = false;
        RiskCard riskCard = null;
        for (byte i = 0; i < 3; i++)
        {
            for (byte j = 0; j < board[i].Count; j++)
            {
                if (board[i][j].id.Equals("ID-R7") && !firstCard)
                {
                    board[i][j].increaseImpatto();
                    board[i][j].increaseProbabilita();
                    firstCard = true;
                    riskCard = board[i][j];
                }
                /*
                    switch (board[i][j].id)
                    {
                        case "ID-R7":
                            board[i][j].increaseImpatto();
                            board[i][j].increaseProbabilita();
                            OnProbabilityAndImpactChanged?.Invoke(i, j);
                            break;
                    }
                */
            }
        }
        OnProbabilityAndImpactChanged?.Invoke(riskCard);
    }
}