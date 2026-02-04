using UnityEngine;
using UnityEngine.Tilemaps;
using Photon.Pun;

public class CropRelatedActionManagerView : MonoBehaviourPun
{
    [SerializeField] private TileBase unplowedTile; // Assign the unplowed tile asset (e.g., dirt) in the Inspector
    [SerializeField] private TileBase plowedTile; // Assign the plowed tile asset (e.g., tilled soil) in the Inspector
    [SerializeField] private PlantDataSO plantData; // Assign the plant data in the Inspector

    private CropActionPresenter presenter;
    private Tilemap ploughableTilemap; // Cache for RPC

    void Start()
    {
        ICropActionService service = new CropActionService(unplowedTile, plowedTile);
        presenter = new CropActionPresenter(service, plantData);
    }

    void Update()
    {
        if (!photonView.IsMine) return; // Only the owner processes input

        // Use switch-case for extensibility with multiple actions
        switch (true)
        {
            case var _ when Input.GetKeyDown(KeyCode.F):
                var plowResult = presenter.PlowAtPlayerPosition();
                if (plowResult.success)
                {
                    photonView.RPC("PlowTileRPC", RpcTarget.Others, plowResult.tilePos.x, plowResult.tilePos.y, plowResult.tilePos.z);
                }
                break;
            case var _ when Input.GetKeyDown(KeyCode.E):
                var plantResult = presenter.PlantAtPlayerPosition();
                if (plantResult.success)
                {
                    plantResult.worldPos.z=0; // Ensure the plant is at ground level
                    PhotonNetwork.Instantiate(plantData.plantPrefab.name, plantResult.worldPos, Quaternion.identity);
                }
                break;
            // Add more cases here for additional actions, e.g., KeyCode.H for harvest
        }
    }

    [PunRPC]
    private void PlowTileRPC(int x, int y, int z)
    {
        Vector3Int tilePos = new Vector3Int(x, y, z);
        // Find the tilemap if not cached (for remote clients)
        if (ploughableTilemap == null)
        {
            GameObject ploughableGO = GameObject.Find("Ploughable_tile");
            if (ploughableGO != null)
            {
                ploughableTilemap = ploughableGO.GetComponent<Tilemap>();
            }
        }
        if (ploughableTilemap != null)
        {
            ploughableTilemap.SetTile(tilePos, plowedTile);
        }
    }
}
