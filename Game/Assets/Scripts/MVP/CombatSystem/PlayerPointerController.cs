using UnityEngine;
using Photon.Pun;
using System.Collections;

/// <summary>
/// Controls the attack direction pointer that orbits around the player.
/// Waits for player to spawn before initializing (multiplayer compatible).
/// </summary>
public class PlayerPointerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject pointerPrefab;

    [Header("Settings")]
    [SerializeField] private float orbitRadius = 1.5f;
    [SerializeField] private float initializationDelay = 0.5f;

    private Transform playerTransform;
    private Transform centerPoint;
    private SpriteRenderer playerSpriteRenderer;
    private Camera mainCamera;
    private Transform pointerTransform;
    private SpriteRenderer pointerSpriteRenderer;
    private Vector3 currentDirection = Vector3.right;
    private bool isInitialized = false;

    private void Start()
    {
        StartCoroutine(DelayedInitialize());
    }

    private void Update()
    {
        if (!isInitialized || playerTransform == null) return;

        UpdateMouseDirection();
        UpdatePointerPosition();
        UpdatePlayerFlip();
    }

    private IEnumerator DelayedInitialize()
    {
        yield return new WaitForSeconds(initializationDelay);
        InitializeComponents();
        SpawnPointer();
    }

    private void InitializeComponents()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
            mainCamera = FindObjectOfType<Camera>();

        GameObject playerObj = FindLocalPlayerEntity();
        if (playerObj == null)
            return;

        playerTransform = playerObj.transform;
        playerSpriteRenderer = playerObj.GetComponent<SpriteRenderer>();

        Transform found = playerTransform.Find("CenterPoint");
        centerPoint = found != null ? found : playerTransform;
    }

    private GameObject FindLocalPlayerEntity()
    {
        // Try "Player" tag first (multiplayer spawn)
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("Player"))
        {
            PhotonView pv = go.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
                return go;
        }

        // Fallback to "PlayerEntity" tag (test scenes)
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("PlayerEntity"))
        {
            PhotonView pv = go.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
                return go;
        }

        return null;
    }

    private void SpawnPointer()
    {
        if (pointerPrefab == null || centerPoint == null)
            return;

        GameObject pointerGO = Instantiate(pointerPrefab, centerPoint.position, Quaternion.identity);
        pointerTransform = pointerGO.transform;
        pointerTransform.SetParent(centerPoint);
        pointerTransform.localPosition = Vector3.zero;

        // Get SpriteRenderer from child (prefab structure: Parent -> Child with sprite)
        pointerSpriteRenderer = pointerGO.GetComponentInChildren<SpriteRenderer>();
        if (pointerSpriteRenderer != null)
        {
            pointerSpriteRenderer.enabled = true;
            pointerSpriteRenderer.sortingOrder = 100;
        }

        isInitialized = true;
    }

    private void UpdateMouseDirection()
    {
        if (mainCamera == null)
            return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
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

    private void UpdatePointerPosition()
    {
        if (pointerTransform == null) 
            return;

        pointerTransform.localPosition = currentDirection * orbitRadius;

        float angle = Mathf.Atan2(currentDirection.y, currentDirection.x) * Mathf.Rad2Deg;
        pointerTransform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void UpdatePlayerFlip()
    {
        if (playerSpriteRenderer == null) 
            return;

        playerSpriteRenderer.flipX = currentDirection.x < 0;
    }

    public Vector3 GetPointerDirection() => currentDirection;
    public Vector3 GetPointerPosition() => pointerTransform != null ? pointerTransform.position : Vector3.zero;
    public float GetOrbitRadius() => orbitRadius;
}