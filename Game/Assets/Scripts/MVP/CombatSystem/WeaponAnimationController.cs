using UnityEngine;
using Photon.Pun;
using System.Collections;

/// <summary>
/// Spawns a sword visual in combat mode.
/// Pivot root is fixed on player; pivot rotates toward mouse.
/// Child sword visual plays attack animation via trigger "Attack".
/// </summary>
public class WeaponAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject weaponAnimationPrefab;

    [Header("Anchor On Player (fixed pivot position)")]
    [SerializeField] private Vector3 anchorOffset = Vector3.zero;

    [Header("Sword Visual Offset From Pivot")]
    [Tooltip("Move visual so sword handle/grip sits at pivot. Example: (0.45, 0, 0)")]
    [SerializeField] private Vector3 gripLocalOffset = Vector3.zero;

    [Header("Rotation")]
    [Tooltip("If sword sprite points RIGHT at 0°, keep 0. If points UP, set -90.")]
    [SerializeField] private float rotationOffsetDegrees = 0f;

    private Transform centerPoint;
    private Camera mainCamera;

    private GameObject pivotRoot;      // fixed point on player
    private GameObject weaponVisual;   // animated sword child
    private Animator weaponAnimator;

    private const string ATTACK_TRIGGER = "Attack";

    private void Start()
    {
        CombatModeManager.OnCombatModeChanged += OnCombatModeChanged;
    }

    private void Update()
    {
        if (pivotRoot == null || centerPoint == null)
            return;

        // keep pivot fixed to player anchor
        pivotRoot.transform.position = centerPoint.position + anchorOffset;

        RotatePivotToMouse();
    }

    private void OnDestroy()
    {
        CombatModeManager.OnCombatModeChanged -= OnCombatModeChanged;
    }

    private void OnCombatModeChanged(bool isActive)
    {
        if (isActive) StartCoroutine(SpawnWhenPlayerReady());
        else DespawnWeapon();
    }

    private IEnumerator SpawnWhenPlayerReady()
    {
        float timeout = 5f;
        float t = 0f;

        while (t < timeout)
        {
            if (TryInitPlayerRefs())
            {
                SpawnWeapon();
                yield break;
            }

            t += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        Debug.LogError("[WeaponAnimationController] Could not find local player/center point.");
    }

    private bool TryInitPlayerRefs()
    {
        if (mainCamera == null)
            mainCamera = Camera.main != null ? Camera.main : FindObjectOfType<Camera>();

        GameObject playerObj = FindLocalPlayerEntity();
        if (playerObj == null) return false;

        Transform found = playerObj.transform.Find("CenterPoint");
        centerPoint = found != null ? found : playerObj.transform;

        return centerPoint != null;
    }

    private GameObject FindLocalPlayerEntity()
    {
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("Player"))
        {
            PhotonView pv = go.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine) return go;
        }

        foreach (GameObject go in GameObject.FindGameObjectsWithTag("PlayerEntity"))
        {
            PhotonView pv = go.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine) return go;
        }

        return null;
    }

    private void SpawnWeapon()
    {
        if (weaponAnimationPrefab == null || centerPoint == null)
            return;

        DespawnWeapon();

        pivotRoot = new GameObject("WeaponPivotRoot");
        pivotRoot.transform.SetParent(centerPoint);
        pivotRoot.transform.position = centerPoint.position + anchorOffset;
        pivotRoot.transform.rotation = Quaternion.identity;

        weaponVisual = Instantiate(weaponAnimationPrefab, pivotRoot.transform);
        weaponVisual.name = "WeaponVisual";
        weaponVisual.transform.localPosition = gripLocalOffset;
        weaponVisual.transform.localRotation = Quaternion.identity;

        weaponAnimator = weaponVisual.GetComponent<Animator>();
        if (weaponAnimator == null)
            weaponAnimator = weaponVisual.GetComponentInChildren<Animator>();
    }

    private void DespawnWeapon()
    {
        if (weaponVisual != null) Destroy(weaponVisual);
        if (pivotRoot != null) Destroy(pivotRoot);

        weaponVisual = null;
        pivotRoot = null;
        weaponAnimator = null;
    }

    private void RotatePivotToMouse()
    {
        if (mainCamera == null) return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.forward, centerPoint.position);

        if (!plane.Raycast(ray, out float dist))
            return;

        Vector3 mouseWorld = ray.GetPoint(dist);
        Vector3 dir = mouseWorld - pivotRoot.transform.position;
        dir.z = 0f;

        if (dir.sqrMagnitude < 0.0001f)
            return;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + rotationOffsetDegrees;
        pivotRoot.transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    public void PlayAttackAnimation()
    {
        if (weaponAnimator == null) return;
        weaponAnimator.SetTrigger(ATTACK_TRIGGER);
    }

    public bool IsWeaponActive => pivotRoot != null;
}