using UnityEngine;

[System.Serializable]
public class NPCRelationshipModel
{
    [SerializeField] private int affection;

    public int Affection => affection;

    public void AddAffection(int amount)
    {
        affection += amount;
        affection = Mathf.Clamp(affection, -100, 1000);
    }
}