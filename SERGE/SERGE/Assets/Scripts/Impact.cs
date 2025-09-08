using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;

public class Impact
{
    public Bands impact;

    public void increase()
    {
        if (impact.Equals(Bands.Low)) impact = Bands.Middle;
        else impact = Bands.High;

    }
    public void decrease()
    {
        if (impact.Equals(Bands.High)) impact = Bands.Middle;
        else impact = Bands.Low;
    }

    public Impact()
    {
        impact = Bands.Low;
    }

    public Bands getImpact()
    {
        return impact;
    }
}
