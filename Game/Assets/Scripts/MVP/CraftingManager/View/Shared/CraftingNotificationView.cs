using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CraftingNotificationView : MonoBehaviour, ICraftingNotification
{
    [Header("UI References")]
    [SerializeField] private GameObject notificationPanel;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image iconImage;

    [Header("Settings")]
    [SerializeField] private float displayDuration = 2f;
    [SerializeField] private bool autoHide = true;

    [Header("Colors")]
    [SerializeField] private Color infoColor = new Color(0.2f, 0.6f, 1f);
    [SerializeField] private Color successColor = new Color(0.2f, 0.8f, 0.2f);
    [SerializeField] private Color warningColor = new Color(1f, 0.8f, 0.2f);
    [SerializeField] private Color errorColor = new Color(1f, 0.3f, 0.3f);

    [Header("Icons (Optional)")]
    [SerializeField] private Sprite infoIcon;
    [SerializeField] private Sprite successIcon;
    [SerializeField] private Sprite warningIcon;
    [SerializeField] private Sprite errorIcon;

    private Coroutine hideCoroutine;

    private void Awake()
    {
        Hide();
    }

    public void ShowNotification(string message, NotificationType type = NotificationType.Info)
    {
        if (string.IsNullOrEmpty(message)) return;

        // Stop previous auto-hide
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        // Set message
        messageText.text = message;

        // Set color based on type
        if (backgroundImage != null)
        {
            backgroundImage.color = GetColorForType(type);
        }

        // Set icon if available
        if (iconImage != null)
        {
            Sprite icon = GetIconForType(type);
            if (icon != null)
            {
                iconImage.sprite = icon;
                iconImage.gameObject.SetActive(true);
            }
            else
            {
                iconImage.gameObject.SetActive(false);
            }
        }

        // Show panel
        notificationPanel.SetActive(true);

        // Auto hide if enabled
        if (autoHide)
        {
            hideCoroutine = StartCoroutine(AutoHideCoroutine());
        }
    }

    public void ShowCraftingResult(string recipeName, int amount, bool success)
    {
        if (success)
        {
            ShowNotification($"✓ Crafted {recipeName} x{amount}", NotificationType.Success);
        }
        else
        {
            ShowNotification($"✗ Failed to craft {recipeName}", NotificationType.Error);
        }
    }

    public void Hide()
    {
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        if (notificationPanel != null)
        {
            notificationPanel.SetActive(false);
        }
    }

    private IEnumerator AutoHideCoroutine()
    {
        yield return new WaitForSeconds(displayDuration);
        Hide();
        hideCoroutine = null;
    }

    private Color GetColorForType(NotificationType type)
    {
        return type switch
        {
            NotificationType.Success => successColor,
            NotificationType.Warning => warningColor,
            NotificationType.Error => errorColor,
            _ => infoColor
        };
    }

    private Sprite GetIconForType(NotificationType type)
    {
        return type switch
        {
            NotificationType.Success => successIcon,
            NotificationType.Warning => warningIcon,
            NotificationType.Error => errorIcon,
            _ => infoIcon
        };
    }

    private void OnDestroy()
    {
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }
    }
}
