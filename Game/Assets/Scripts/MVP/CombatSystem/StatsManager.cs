using UnityEngine;

public class StatsManager : MonoBehaviour
{
    public static StatsManager Instance;

    [Header("Combat Stats")]
    public float attackRange;
    public float knockbackForce;
    public int attackDamage;
    public float cooldownTime;

    [Header("Health Stats")]
    public int currentHealth;
    public int maxHealth;
    public float easeSpeed;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
}
