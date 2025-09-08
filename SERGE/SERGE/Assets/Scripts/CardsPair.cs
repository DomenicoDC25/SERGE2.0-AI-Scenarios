using ExitGames.Client.Photon;
using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;

public class CardsPair : MonoBehaviourPunCallbacks
{
    private byte p;
    private byte i;

    public void setProbability(byte p)
    {
        this.p = p;
    }

    public void setImpact(byte i)
    {
        this.i = i;
    }

    public byte getImpact()
    {
        return this.i;
    }

    public byte getProbability()
    {
        return this.p;
    }

    void Start()
    {
 
        // Registra il tipo di dato personalizzato utilizzando PhotonPeer.RegisterType()
        PhotonPeer.RegisterType(typeof(CardsPair), (byte)'C', SerializeCardsPair, DeserializeCardsPair);
    }

    // Metodi di serializzazione e deserializzazione per il tipo di dato personalizzato
    public static byte[] SerializeCardsPair(object customType)
    {
        CardsPair cardsPair = (CardsPair)customType;
        byte[] data = new byte[2];
        data[0] = cardsPair.p;
        data[1] = cardsPair.i;
        return data;
    }

    public static object DeserializeCardsPair(byte[] serializedData)
    {
        CardsPair cardsPair = new CardsPair();
        cardsPair.p = serializedData[0];
        cardsPair.i = serializedData[1];
        return cardsPair;
    }
}


