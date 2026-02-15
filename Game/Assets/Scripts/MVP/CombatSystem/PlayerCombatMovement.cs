using UnityEngine;

public class PlayerCombatMovement : MonoBehaviour
{
    public PlayerCombat playerCombat;

    [Header("Input")]
    [SerializeField] private KeyCode attackKey = KeyCode.Space;
    [SerializeField] private int attackMouseButton = 0;

    private void Start()
    {
        if (playerCombat == null)
        {
            playerCombat = GetComponent<PlayerCombat>();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(attackKey) || Input.GetMouseButton(attackMouseButton))
        {
            if (playerCombat != null)
            {
                playerCombat.Attack();
            }
        }
    }
}

