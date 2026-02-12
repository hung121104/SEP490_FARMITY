using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuNav : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// Load the Player World List Scene
    /// Call this method from a button onClick event
    /// </summary>
    public void LoadPlayerWorldListScene()
    {
        SceneManager.LoadScene("PlayerWorldListScene");
    }
}
