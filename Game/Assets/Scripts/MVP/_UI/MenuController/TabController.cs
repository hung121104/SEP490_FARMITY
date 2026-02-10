using UnityEngine;
using UnityEngine.UI;

public class TabController : MonoBehaviour
{
    public Image[] tabActive;
    public GameObject[] pages;
    // Start is called before the first frame update
    void Start() {
        ActivateTab(0);
    }

    public void ActivateTab(int tabNo) {
        for (int i = 0; i < pages.Length; i++)
        {
            pages[i].SetActive(false);
            tabActive[i].enabled = false;
        }
        pages[tabNo].SetActive(true);
        tabActive[tabNo].enabled = true;
    } 
}
