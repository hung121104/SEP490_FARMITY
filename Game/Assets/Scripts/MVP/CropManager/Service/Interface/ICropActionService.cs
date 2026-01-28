using UnityEngine;

public interface ICropActionService
{
    void InitializeTilemap();
    (bool success, Vector3Int tilePos) PlowAtWorldPosition(Vector3 worldPos);
    (bool success, Vector3 worldPos) PlantAtWorldPosition(Vector3 worldPos, PlantDataSO plantData);
}