using UnityEngine;
using Photon.Pun;
public class SpawnPlayer : MonoBehaviour
{
    [SerializeField]
    private GameObject playerPrefab;

    [SerializeField]
    private Transform[] spawnPoints;

    private void Start()
    {
        if (playerPrefab == null || spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("Player prefab or spawn points not set up correctly.");
            return;
        }
        // Choose a random spawn point
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        // Instantiate the player prefab at the chosen spawn point using PhotonNetwork
        // Instantiate with only the Z rotation (keep facing) and clear X/Y to avoid tilting in 3D.
        Quaternion spawnRot = Quaternion.Euler(0f, 0f, spawnPoint.rotation.eulerAngles.z);
        PhotonNetwork.Instantiate(playerPrefab.name, spawnPoint.position, spawnRot);
    }

}
