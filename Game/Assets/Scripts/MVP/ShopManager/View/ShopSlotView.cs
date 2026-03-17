using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public class ShopSlotView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemPriceText;

    [SerializeField] private Image backgroundImage;

    private Button _buyButton;
    private int _slotIndex;

    public event Action<int> OnBuyClicked;

    private void Awake()
    {
        InitButton();
    }

    
    private void InitButton()
    {
        if (_buyButton == null)
        {
            _buyButton = GetComponent<Button>();
            if (_buyButton != null)
            {
                _buyButton.onClick.RemoveAllListeners(); 
                _buyButton.onClick.AddListener(() => OnBuyClicked?.Invoke(_slotIndex));
            }
        }
    }

    public void Setup(int index, Sprite icon, string name, int price, bool isSoldOut)
    {
        _slotIndex = index;

        
        InitButton();

        if (itemIcon != null) itemIcon.sprite = icon;
        if (itemNameText != null) itemNameText.text = name;
        if (itemPriceText != null) itemPriceText.text = price.ToString();
        if (_buyButton != null)
        {
            if (isSoldOut)
            {
                _buyButton.interactable = false;
                if (backgroundImage != null) backgroundImage.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            }
            else
            {
                _buyButton.interactable = true;
                if (backgroundImage != null) backgroundImage.color = Color.white;
            }
        }
       
    }

    public void FlashRed()
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = new Color(1f, 0.5f, 0.5f, 1f);
            Invoke(nameof(ResetColor), 0.2f);
        }
    }

    private void ResetColor()
    {
        if (backgroundImage != null) backgroundImage.color = Color.white;
    }
}