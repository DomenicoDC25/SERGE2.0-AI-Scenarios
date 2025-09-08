using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbsentCustomer : IEventCard
{
    public static event Action<RiskCard> OnProbabilityAndImpactChanged;
    string IEventCard.Name => "Absent Customer";

    void IEventCard.modify(Dictionary<byte, List<RiskCard>> board)
    {
        bool firstCard = false;
        RiskCard card = null;
        for (byte i = 0; i < 3; i++)
        {
            for (byte j = 0; j < board[i].Count; j++)
            {
                if (board[i][j].id.Equals("ID-R3") && !firstCard)
                {
                    board[i][j].increaseImpatto();
                    board[i][j].increaseProbabilita();
                    firstCard = true;
                    card = board[i][j];
                }
                /*
                    switch (board[i][j].id)
                    {
                        case "ID-R3":
                            board[i][j].increaseImpatto();
                            board[i][j].increaseProbabilita();
                            OnProbabilityAndImpactChanged?.Invoke(i, j);
                            break;
                    }
                */
            }
        }
        OnProbabilityAndImpactChanged?.Invoke(card);
    }
}
