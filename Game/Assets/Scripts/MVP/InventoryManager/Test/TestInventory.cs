using UnityEngine;

public class TestInventory : MonoBehaviour
{
    [SerializeField] private InventoryGameView inventorySystem;
    [SerializeField] private ItemDataSO[] testItem;
    [SerializeField] private KeyCode addItemKey = KeyCode.T;

    private void Update()
    {
        // Press T to add test item
        if (Input.GetKeyDown(addItemKey) && testItem != null)
        {
            foreach (var item in testItem) 
            {
                inventorySystem.AddItem(item, 33, Quality.Normal);
            }             
            Debug.Log("Added test item");
        }
    }
}
