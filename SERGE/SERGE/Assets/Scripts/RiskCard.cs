using ExitGames.Client.Photon;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class RiskCard
{
    public string id;
    public string name;
    public string description;
    public byte probability;
    public List<byte> prevention_plan_high = new List<byte>(); //L'array contiene l'indice relativo alla risorsa utilizzata per mitigare il rischio
    public List<byte> prevention_plan_middle = new List<byte>();
    public List<byte> prevention_plan_low = new List<byte>();
    /*public byte[] prevention_plan_high = new byte[3]; //L'array contiene l'indice relativo alla risorsa utilizzata per mitigare il rischio,  255 vuol dire che non ha nessun rischio associato a quell 
    public byte[] prevention_plan_middle = new byte[3];
    public byte[] prevention_plan_low = new byte[3];*/
    public byte impact;
    public Texture texture;

    public RiskCard(string id, string name, string description, List<byte> preventionplan_high, List<byte> preventionplan_medium, List<byte> preventionplan_low, byte probability, byte impact, Texture texture)
    {
        this.id = id;
        this.name = name;
        this.description = description;
        foreach(var a in preventionplan_high)
        {
            prevention_plan_high.Add(a);
        }
        foreach (var a in preventionplan_medium)
        {
            prevention_plan_middle.Add(a);
        }
        foreach (var a in preventionplan_medium)
        {
            prevention_plan_low.Add(a);
        }
        /*for (int i = 0; i < 3; i++) 
        {
            prevention_plan_high[i] = preventionplan_high[i];
        }
        for (int i = 0; i < 3; i++)
        {
            prevention_plan_middle[i] = preventionplan_medium[i];
        }
        for (int i = 0; i < 3; i++)
        {
            prevention_plan_low[i] = preventionplan_low[i];
        }*/
        this.probability = probability;
        this.impact = impact;
        this.texture = texture;
        //PhotonPeer.RegisterType(typeof(RiskCard), 1, RiskCard.Serialize, RiskCard.Deserialize);
    }
    /*
    public static object Deserialize(byte[] data)
    {
        var result = new RiskCard((Texture)Resources.Load("/Images/Risks/"+result.id+".jpg");
        result.Id = data[0];
        return result;
    }

    public static byte[] Serialize(object customType)
    {
        var c = (RiskCard)customType;
        return new byte[] { c.Id };
    }
    */
    public override bool Equals(object obj)
    {
        return obj is RiskCard card &&
               id == card.id;
    }

    public void increaseImpatto()
    {
        switch (impact)
        {
            case 0:
                impact = 1;
                break;
            case 1:
                impact = 2;
                break;
            case 2:
                impact = 2;
                break;
        }
    }

    public void decreaseImpatto()
    {
        switch (impact)
        {
            case 0:
                impact = 0;
                break;
            case 1:
                impact = 0;
                break;
            case 2:
                impact = 1;
                break;
        }
    }

    public void increaseProbabilita()
    {
        switch (probability)
        {
            case 0:
                probability = 20;
                break;
            case 20:
                probability = 40;
                break;
            case 40:
                probability = 60;
                break;
            case 60:
                probability = 80;
                break;
            case 80:
                probability = 100;
                break;
            case 100:
                probability = 100;
                break;
        }
    }

    public void decreaseProbabilita()
    {
        switch (probability)
        {
            case 0:
                probability = 0;
                break;
            case 20:
                probability = 0;
                break;
            case 40:
                probability = 20;
                break;
            case 60:
                probability = 40;
                break;
            case 80:
                probability = 60;
                break;
            case 100:
                probability = 80;
                break;
        }
    }
    public override string ToString()
    {
        string result = this.id+"\n"+this.name+"\n"+this.description+"\nImpact:\t\t"+this.impact+"\nProbability:\t"+this.probability+"\nPrevention plan to hide:\n";
        if (prevention_plan_high.Count != 0)
        {
            result += "\nHigh Level:\t";
            foreach (var a in prevention_plan_high)
            {
                result += a.ToString()+", ";
            }
        }
        if (prevention_plan_middle.Count != 0)
        {
            result += "\nMiddle Level:\t";
            foreach (var a in prevention_plan_middle)
            {
                result += a.ToString() + ", ";
            }
        }
        if (prevention_plan_low.Count != 0)
        {
            result += "\nLow Level:\t";
            foreach (var a in prevention_plan_low)
            {
                result += a.ToString() + ", ";
            }
        }
        return result;
    }
}
