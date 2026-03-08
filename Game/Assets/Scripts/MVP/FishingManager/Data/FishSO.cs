using UnityEngine;

[CreateAssetMenu(menuName = "Fishing/Fish")]
public class FishSO : ScriptableObject
{
    public string fishName;

    public Sprite icon;

    [Range(0f, 1f)]
    public float catchChance;

    public string itemID;
}