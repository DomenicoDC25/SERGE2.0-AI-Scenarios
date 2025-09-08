using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Satisfied : IEventCard
{
    public static event Action<RiskCard> OnProbabilityAndImpactChanged;

    public string Name = "Satisfied Product Owner";

    string IEventCard.Name { get => "Satisfied Product Owner"; }

    void IEventCard.modify(Dictionary<byte, List<RiskCard>> board)
    {
        bool firstCard = false;
        bool secondCard = false;
        RiskCard[] riskCards = new RiskCard[2]; 
        for (byte i = 0; i <= 2; i++)
        {
            for (byte j = 0; j <= board[i].Count-1; j++)
            {
                if (board[i][j].id.Equals("ID-R6") && !firstCard)
                {
                    board[i][j].decreaseImpatto();
                    board[i][j].decreaseProbabilita();
                    firstCard = true;
                    riskCards[0] = board[i][j];
                }
                if (board[i][j].id.Equals("ID-R9") && !secondCard)
                {
                    board[i][j].decreaseImpatto();
                    board[i][j].decreaseProbabilita();
                    secondCard = true;
                    riskCards[1] = board[i][j];
                }
                /*
                switch (board[i][j].id)
                {
                    case "ID-R6":
                    board[i][j].decreaseImpatto();
                    board[i][j].decreaseProbabilita();
                    switchPileInBoard(i, j, board);
                    //OnProbabilityAndImpactChanged?.Invoke(i, j);

                    case "ID-R9":
                    board[i][j].decreaseImpatto();
                    board[i][j].decreaseProbabilita();
                    switchPileInBoard(i, j, board);
                    //OnProbabilityAndImpactChanged?.Invoke(i, j);
                    break;
                }*/
            }
        }
        OnProbabilityAndImpactChanged?.Invoke(riskCards[0]);
        OnProbabilityAndImpactChanged?.Invoke(riskCards[1]);
    }
}