using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IEventCard
{
    string Name { get; }

    public static event Action<RiskCard> OnProbabilityAndImpactChanged;

    public abstract void modify(Dictionary<byte, List<RiskCard>> Board);
}
