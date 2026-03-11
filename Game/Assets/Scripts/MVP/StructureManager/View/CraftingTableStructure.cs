using UnityEngine;

public class CraftingTableStructure : MonoBehaviour, IInteractable
{
    [Tooltip("Key to interact with the crafting table.")]
    [SerializeField] private KeyCode pickupKey = KeyCode.F;

    public string InteractionPrompt => "Crafting";

    private CraftingSystemManager craftingSystemManager;

    private void Start()
    {
        craftingSystemManager = FindFirstObjectByType<CraftingSystemManager>();
    }

    public void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("PlayerEntity") && Input.GetKeyDown(pickupKey))
        {
            Interact();
        }
    }

    public void Interact()
    {
        if (craftingSystemManager != null)
        {
            craftingSystemManager.OpenCraftingUI();
        }
        else
        {
            Debug.LogWarning("[CraftingTableStructure] CraftingSystemManager not found in scene!");
        }
    }
}
