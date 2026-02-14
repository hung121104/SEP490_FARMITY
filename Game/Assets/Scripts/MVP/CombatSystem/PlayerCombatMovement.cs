using UnityEngine;

public class PlayerCombatMovement : MonoBehaviour
{
   public PlayerCombat playerCombat;

    private void Start()
    {
        if (playerCombat == null)
        {
            playerCombat = GetComponent<PlayerCombat>();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButton(0))
        {
            if (playerCombat != null)
            {
                playerCombat.Attack();
            }
        }
    }
}

