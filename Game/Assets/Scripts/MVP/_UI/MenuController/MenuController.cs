using UnityEngine;

public class MenuController : MonoBehaviour
{
    public GameObject menuCanvas;
    public GameObject hotbarPanel;

    void Start()
    {
        menuCanvas.SetActive(false);
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            hotbarPanel.SetActive(menuCanvas.activeSelf);
            menuCanvas.SetActive(!menuCanvas.activeSelf);            
        }
    }
}
