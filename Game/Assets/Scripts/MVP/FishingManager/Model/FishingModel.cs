using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FishInfo
{
    public string fishName;
    public string itemID;
    public Sprite icon;

    [Range(0f, 100f)]
    public float catchChance;
}

public class FishingModel
{
    public string currentRodID;
    public string lastCaughtFishID;
    public bool isFishing = false;
}