using UnityEngine;
using TMPro;

public class GoldHUDView : MonoBehaviour
{
    [Header("UI Reference")]
    [Tooltip("Kéo TextMeshPro hiển thị tiền vào đây")]
    [SerializeField] private TextMeshProUGUI goldText;

    private int _lastGold = -1; 

    private void Start()
    {
       
        if (WorldDataManager.Instance != null)
        {
            UpdateGoldUI(WorldDataManager.Instance.Gold);
        }
    }

    private void Update()
    {
        if (WorldDataManager.Instance != null && WorldDataManager.Instance.Gold != _lastGold)
        {
            UpdateGoldUI(WorldDataManager.Instance.Gold);
        }
    }

    private void UpdateGoldUI(int currentGold)
    {
        _lastGold = currentGold;

        if (goldText != null)
        {
            goldText.text = $"{currentGold:N0} ";
        }
    }
}