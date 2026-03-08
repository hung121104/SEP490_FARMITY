using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Fishing/FishDatabase")]
public class FishDatabase : ScriptableObject
{
    public List<FishSO> fishes;
}