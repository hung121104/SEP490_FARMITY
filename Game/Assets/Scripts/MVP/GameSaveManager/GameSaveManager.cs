using System.Collections;
using UnityEngine;

public class GameSaveManager : MonoBehaviour
{
    [SerializeField] private float autoSaveInterval = 5f;

    void Start()
    {
        InvokeRepeating(nameof(OnAutoSaveTick), autoSaveInterval, autoSaveInterval);
    }

    private void OnAutoSaveTick()
    {
        Debug.Log("[GameSaveManager] Auto save tick");
        // put your save logic here
    }

    private bool ApiCall(){
        

        return false;
    }
}