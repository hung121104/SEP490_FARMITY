using UnityEngine;

[CreateAssetMenu(fileName = "SaveGameModel", menuName = "Scriptable Objects/SaveGameModel")]
public class SaveGameDataSO : ScriptableObject
{
    [SerializeField] private Transform _transform;
    
}
