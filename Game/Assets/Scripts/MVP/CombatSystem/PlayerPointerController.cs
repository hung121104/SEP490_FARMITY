using UnityEngine;

/// <summary>
/// Controls the attack direction pointer that orbits around the player.
/// Sits on a separate GameObject inside CombatSystem.
/// Replaces attackPoint as the hitbox position for normal attack.
/// Flips player sprite based on mouse direction.
/// </summary>
public class PlayerPointerController : MonoBehaviour
{
    #region Serialized Fields

    [Header("References")]
    [SerializeField] private GameObject pointerPrefab;

    [Header("Settings")]
    [SerializeField] private float orbitRadius = 1.5f;

    #endregion

    #region Private Fields

    private Transform playerTransform;
    private Transform centerPoint;
    private SpriteRenderer playerSpriteRenderer;
    private PlayerCombat playerCombat;
    private Camera mainCamera;
    private Transform pointerTransform;
    private Vector3 currentDirection = Vector3.right;

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        InitializeComponents();
        SpawnPointer();
    }

    private void Update()
    {
        if (playerTransform == null) return;

        UpdateMouseDirection();
        UpdatePointerPosition();
        UpdatePlayerFlip();
    }

    #endregion

    #region Initialization

    private void InitializeComponents()
    {
        mainCamera = Camera.main;

        GameObject playerObj = GameObject.FindGameObjectWithTag("PlayerEntity");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerSpriteRenderer = playerObj.GetComponent<SpriteRenderer>();
            playerCombat = playerObj.GetComponent<PlayerCombat>();

            // Find CenterPoint child
            Transform found = playerTransform.Find("CenterPoint");
            if (found != null)
                centerPoint = found;
            else
            {
                Debug.LogWarning("PlayerPointerController: CenterPoint not found! Using player root instead.");
                centerPoint = playerTransform;
            }
        }
        else
        {
            Debug.LogWarning("PlayerPointerController: PlayerEntity tag not found!");
            return;
        }

        if (playerCombat == null)
            Debug.LogWarning("PlayerPointerController: PlayerCombat not found on PlayerEntity!");
    }

    private void SpawnPointer()
    {
        if (pointerPrefab == null)
        {
            Debug.LogWarning("PlayerPointerController: Pointer prefab not assigned!");
            return;
        }

        GameObject pointerGO = Instantiate(pointerPrefab, centerPoint.position, Quaternion.identity);
        pointerTransform = pointerGO.transform;

        // Parent to CenterPoint so localPosition is relative to center
        pointerTransform.SetParent(centerPoint);

        if (playerCombat != null)
            playerCombat.attackPoint = pointerTransform;
    }

    #endregion

    #region Mouse Direction

    private void UpdateMouseDirection()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        // Use CenterPoint as plane origin for accurate direction
        Plane plane = new Plane(Vector3.forward, centerPoint.position);

        if (plane.Raycast(ray, out float dist))
        {
            Vector3 mouseWorldPos = ray.GetPoint(dist);
            Vector3 direction = mouseWorldPos - centerPoint.position;
            direction.z = 0f;

            if (direction.magnitude > 0.01f)
                currentDirection = direction.normalized;
        }
    }

    #endregion

    #region Pointer And Flip

    private void UpdatePointerPosition()
    {
        if (pointerTransform == null) return;

        pointerTransform.localPosition = currentDirection * orbitRadius;

        float angle = Mathf.Atan2(currentDirection.y, currentDirection.x) * Mathf.Rad2Deg;
        pointerTransform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void UpdatePlayerFlip()
    {
        if (playerSpriteRenderer == null) return;

        playerSpriteRenderer.flipX = currentDirection.x < 0;
    }

    #endregion

    #region Public API

    public Vector3 GetPointerDirection() => currentDirection;
    public Vector3 GetPointerPosition() => pointerTransform != null ? pointerTransform.position : Vector3.zero;
    public float GetOrbitRadius() => orbitRadius;

    #endregion
}