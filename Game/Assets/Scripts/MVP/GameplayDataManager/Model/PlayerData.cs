using System;


[Serializable]
public struct PlayerData
{
    public string _id;
    public string worldId;
    public string accountId;
    public float positionX;
    public float positionY;
    public int sectionIndex;

    // Appearance config IDs (paper-doll layers)
    public string hairConfigId;
    public string outfitConfigId;
    public string hatConfigId;
    public string toolConfigId;
}