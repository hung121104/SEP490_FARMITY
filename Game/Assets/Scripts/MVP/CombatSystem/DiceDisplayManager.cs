using UnityEngine;

public class DiceDisplayManager : MonoBehaviour
{
    public static DiceDisplayManager Instance { get; private set; }

    [SerializeField] private GameObject d6Prefab;
    [SerializeField] private GameObject d8Prefab;
    [SerializeField] private GameObject d10Prefab;
    [SerializeField] private GameObject d12Prefab;
    [SerializeField] private GameObject d20Prefab;

    [Header("Roll Display Settings")]
    [SerializeField] private Vector3 rollDisplayOffset = new Vector3(0f, 1.8f, 0f);
    [SerializeField] private float rollAnimationDuration = 0.4f;

    [Header("Animation")]
    [SerializeField] private float wobbleScale = 1.15f;
    [SerializeField] private float wobbleSpeed = 10f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public GameObject GetDicePrefab(DiceTier tier)
    {
        return tier switch
        {
            DiceTier.D6 => d6Prefab,
            DiceTier.D8 => d8Prefab,
            DiceTier.D10 => d10Prefab,
            DiceTier.D12 => d12Prefab,
            DiceTier.D20 => d20Prefab,
            _ => null
        };
    }

    public Vector3 GetRollDisplayOffset() => rollDisplayOffset;
    public float GetRollAnimationDuration() => rollAnimationDuration;
    public float GetWobbleScale() => wobbleScale;
    public float GetWobbleSpeed() => wobbleSpeed;
}