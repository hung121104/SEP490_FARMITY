using UnityEngine;

public class MenuController : MonoBehaviour
{
    public GameObject menuCanvas;
    public GameObject hotbarPanel;
    public KeyCode addItem = KeyCode.E;

    void Start()
    {
        menuCanvas.SetActive(false);
    }
    void Update()
    {
        if (Input.GetKeyDown(addItem))
        {
            hotbarPanel.SetActive(menuCanvas.activeSelf);
            menuCanvas.SetActive(!menuCanvas.activeSelf);            
        }
    }
}
