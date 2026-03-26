using UnityEngine;
using Photon.Pun;

public class RainManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject rainPrefab;

    [Header("Settings")]
    [SerializeField] private float heightOffset = 20f;

    private Transform player;
    private GameObject rainInstance;
    private bool initialized = false;
    private bool isRaining = false;

    private void Update()
    {
        if (!isRaining)
            return;

        if (!initialized)
        {
            foreach (GameObject p in GameObject.FindGameObjectsWithTag("PlayerEntity"))
            {
                PhotonView pv = p.GetComponent<PhotonView>();
                if (pv != null && pv.IsMine)
                {
                    player = p.transform;
                    SpawnRain();
                    initialized = true;
                    break;
                }
            }
            return;
        }

        // Player may be destroyed during scene/room transition — reset so we re-find next frame.
        if (player == null)
        {
            DestroyRain();
            initialized = false;
        }
    }

    private void SpawnRain()
    {
        if (rainInstance != null)
            return;

        rainInstance = Instantiate(rainPrefab, player);
        rainInstance.transform.localPosition = new Vector3(0f, heightOffset, 0f);
    }

    private void DestroyRain()
    {
        if (rainInstance != null)
        {
            Destroy(rainInstance);
            rainInstance = null;
        }
    }

    public void SetRainState(bool state)
    {
        isRaining = state;

        if (!isRaining)
        {
            DestroyRain();
            initialized = false;
        }
    }
}