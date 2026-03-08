using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ChangeTextOnPress : MonoBehaviour
{
    [SerializeField] private TMP_Text targetText;
    [SerializeField] private string newText;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(OnButtonPressed);
    }

    private void OnButtonPressed()
    {
        if (targetText != null)
            targetText.text = newText;
    }
}
