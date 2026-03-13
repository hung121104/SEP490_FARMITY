using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

public class ShopSlotView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI priceText;

    [Tooltip("Nút mua (Có thể là nút bọc lấy cái priceText hoặc cả ô đồ)")]
    [SerializeField] private Button buyButton;

    private int _slotIndex;
    public event Action<int> OnBuyClicked;

    private Coroutine _flashCoroutine;

    public void Setup(int index, Sprite icon, string name, int price, bool isSoldOut)
    {
        _slotIndex = index;
        iconImage.sprite = icon;
        nameText.text = name;
        priceText.text = price.ToString();

        
        buyButton.interactable = !isSoldOut;

        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(() => OnBuyClicked?.Invoke(_slotIndex));
    }

    public void FlashRed()
    {
        if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
        _flashCoroutine = StartCoroutine(DoFlashRed());
    }

    private IEnumerator DoFlashRed()
    {
        Color originalColor = Color.white; 

        
        for (int i = 0; i < 2; i++)
        {
            priceText.color = Color.red;
            yield return new WaitForSeconds(0.15f);
            priceText.color = originalColor;
            yield return new WaitForSeconds(0.15f);
        }

        priceText.color = originalColor;
    }
}