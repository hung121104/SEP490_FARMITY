using UnityEngine;
using Photon.Pun;

/// <summary>
/// Handles player input for pollen collection and wires up the pollen MVP stack.
/// Place on any GameObject in the scene (e.g. the CropManager).
///
/// The crop is NOT removed when pollen is collected.
/// Only works when the player is targeting a flowering crop (stage == PlantDataSO.pollenStage).
/// </summary>
public class CropPollenHarvestingView : MonoBehaviourPun
{
    // ── Settings ──────────────────────────────────────────────────────────
    [Header("Pollen Collection Settings")]
    [Tooltip("Key the player presses to collect pollen from a flowering crop.")]
    public KeyCode collectKey = KeyCode.G;
    [Tooltip("Interaction radius around the player.")]
    public float checkRadius = 2f;
    [Tooltip("Tag used to find the local Photon player.")]
    public string playerTag = "PlayerEntity";
    [Tooltip("Seconds between repeated collects while holding the key.")]
    public float collectRepeatInterval = 0.4f;
    public bool allowHoldToCollect = false;

    // ── MVP ───────────────────────────────────────────────────────────────
    private CropPollenPresenter presenter;

    // ── Runtime state ─────────────────────────────────────────────────────
    private Transform playerTransform;
    private Vector2Int lastPollenTile = new Vector2Int(int.MinValue, int.MinValue);
    private float holdTimer = 0f;

    // ── Lifecycle ─────────────────────────────────────────────────────────
    void Awake()
    {
        var service = new CropPollenService(
            WorldDataManager.Instance,
            FindAnyObjectByType<CropManagerView>(),
            FindAnyObjectByType<InventoryGameView>()
        );
        presenter = new CropPollenPresenter(this, service);
    }

    void Start()
    {
        FindLocalPlayer();
    }

    // ── Update ────────────────────────────────────────────────────────────
    void Update()
    {
        if (playerTransform == null) FindLocalPlayer();
        if (playerTransform == null) return;

        if (allowHoldToCollect)
        {
            if (Input.GetKeyDown(collectKey))
            {
                HandleCollectInput();
                holdTimer = collectRepeatInterval;
            }
            if (Input.GetKey(collectKey))
            {
                holdTimer -= Time.deltaTime;
                if (holdTimer <= 0f)
                {
                    HandleCollectInput();
                    holdTimer = collectRepeatInterval;
                }
            }
            if (Input.GetKeyUp(collectKey))
            {
                holdTimer = 0f;
                lastPollenTile = new Vector2Int(int.MinValue, int.MinValue);
            }
        }
        else
        {
            if (Input.GetKeyDown(collectKey))
                HandleCollectInput();
            if (Input.GetKeyUp(collectKey))
                lastPollenTile = new Vector2Int(int.MinValue, int.MinValue);
        }
    }

    // ── View callbacks (called by Presenter) ──────────────────────────────

    public void OnPollenCollected(Vector3 tilePos, PollenData pollen)
    {
        Debug.Log($"[CropPollenHarvestingView] Collected '{pollen.itemName}' from ({tilePos.x:F0},{tilePos.y:F0}).");
        // Add sound / particle here later
    }

    public void OnPollenCollectFailed(Vector3 tilePos)
    {
        // Silent — the crop just isn't flowering or inventory is full
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private void HandleCollectInput()
    {
        Vector3 target = GetDirectionalTile();
        if (target == Vector3.zero) return;
        presenter.HandleCollectPollen(target);
    }

    private Vector3 GetDirectionalTile()
    {
        if (Camera.main == null || playerTransform == null) return Vector3.zero;

        return CropTileSelector.GetDirectionalTile(
            playerTransform.position,
            Camera.main.ScreenToWorldPoint(Input.mousePosition),
            checkRadius,
            ref lastPollenTile);
    }

    private void FindLocalPlayer()
    {
        foreach (GameObject go in GameObject.FindGameObjectsWithTag(playerTag))
        {
            PhotonView pv = go.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                Transform center = go.transform.Find("CenterPoint");
                playerTransform = center != null ? center : go.transform;
                return;
            }
        }
    }
}
