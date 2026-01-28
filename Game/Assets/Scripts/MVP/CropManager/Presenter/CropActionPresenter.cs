using UnityEngine;

public class CropActionPresenter
{
    private ICropActionService cropActionService;
    private PlantDataSO currentPlantData; // Assign or load this as needed

    public CropActionPresenter(ICropActionService service, PlantDataSO plantData)
    {
        cropActionService = service;
        currentPlantData = plantData;
    }

    public (bool success, Vector3Int tilePos) PlowAtPlayerPosition()
    {
        GameObject player = GameObject.FindGameObjectWithTag("PlayerEntity");
        if (player == null)
        {
            Debug.LogWarning("Player not found. Ensure the player GameObject has the 'PlayerEntity' tag.");
            return (false, Vector3Int.zero);
        }

        return cropActionService.PlowAtWorldPosition(player.transform.position);
    }

    public (bool success, Vector3 worldPos) PlantAtPlayerPosition()
    {
        GameObject player = GameObject.FindGameObjectWithTag("PlayerEntity");
        if (player == null)
        {
            Debug.LogWarning("Player not found. Ensure the player GameObject has the 'PlayerEntity' tag.");
            return (false, Vector3.zero);
        }

        return cropActionService.PlantAtWorldPosition(player.transform.position, currentPlantData);
    }
}