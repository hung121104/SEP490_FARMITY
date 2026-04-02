using UnityEngine;

public class UnloadMapScript : LoadMapScript
{
    private void Reset()
    {
        activeStateOnTrigger = false;
    }

    private void OnValidate()
    {
        activeStateOnTrigger = false;
    }
}
