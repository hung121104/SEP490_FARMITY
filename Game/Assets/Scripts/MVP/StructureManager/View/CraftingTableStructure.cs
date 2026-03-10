using UnityEngine;

public class CraftingTableStructure : MonoBehaviour, IInteractable
{
    public string InteractionPrompt => "Crafting";

    public void Interact()
    {
        // TODO: UIManager.Instance.OpenStorageUI(...)
        Debug.Log("Opening chest!");
    }
}
