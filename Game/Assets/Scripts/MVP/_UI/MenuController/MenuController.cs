using Unity.VisualScripting;
using UnityEngine;

public class MenuController : MonoBehaviour
{
    public GameObject menuCanvas;
    public GameObject hotbarPanel;
    public InventoryGameView inventoryGameView;
    public KeyCode useMenu = KeyCode.E;

    void Start()
    {
        menuCanvas.SetActive(false);
    }
    void Update()
    {
        if (Input.GetKeyDown(useMenu) && menuCanvas != null && hotbarPanel != null && inventoryGameView != null)
        {
            hotbarPanel.SetActive(menuCanvas.activeSelf);

            //Force cancel all ongoing actions in inventory when menu is toggled
            menuCanvas.SetActive(!menuCanvas.activeSelf);

            if (menuCanvas.activeSelf == false)
            {
                inventoryGameView.CloseInventory();
            }
            else 
            { 
                inventoryGameView.OpenInventory();
            }         
        }
    }
}
