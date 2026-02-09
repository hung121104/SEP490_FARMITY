using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryView : MonoBehaviour, IInventoryView
{
    [Header("UI Panels")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private GameObject itemDetailsPanel;
    [SerializeField] private GameObject dragPreviewObject;

    [Header("Slot Container")]
    [SerializeField] private Transform slotContainer;
    [SerializeField] private GameObject slotPrefab;

    [Header("Item Details UI")]
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;
    [SerializeField] private Image itemDetailIcon;
    [SerializeField] private TextMeshProUGUI itemStatsText;
    [SerializeField] private Button useButton;
    [SerializeField] private Button dropButton;

    [Header("Other UI")]
    [SerializeField] private Button sortButton;
    [SerializeField] private TextMeshProUGUI notificationText;
    [SerializeField] private float notificationDuration = 2f;

    [Header("Drag Preview")]
    [SerializeField] private Image dragPreviewIcon;
    [SerializeField] private CanvasGroup dragPreviewCanvasGroup;

    private List<InventorySlotView> slotViews = new List<InventorySlotView>();
    private int currentSelectedSlot = -1;
    private Coroutine notificationCoroutine;

    public bool IsVisible => inventoryPanel.activeSelf;

    #region Events

    public event Action<int> OnSlotClicked;
    public event Action<int> OnSlotBeginDrag;
    public event Action<Vector2> OnSlotDrag;
    public event Action OnSlotEndDrag;
    public event Action<int> OnSlotDrop;
    public event Action<int> OnUseItemRequested;
    public event Action<int> OnDropItemRequested;
    public event Action OnSortRequested;

    #endregion

    private void Awake()
    {
        InitializeButtons();
        HideItemDetails();
        HideDragPreview();

        if (notificationText != null)
            notificationText.gameObject.SetActive(false);
    }

    public void InitializeSlots(int slotCount)
    {
        // Clear existing slots
        foreach (var slot in slotViews)
        {
            if (slot != null)
                Destroy(slot.gameObject);
        }
        slotViews.Clear();

        // Create new slots
        for (int i = 0; i < slotCount; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotContainer);
            InventorySlotView slotView = slotObj.GetComponent<InventorySlotView>();

            slotView.Initialize(i);

            // Subscribe to slot events
            int index = i; // Capture for closure
            slotView.OnClickedRequested += (slot) => HandleSlotClicked(slot);
            slotView.OnBeginDragRequested += (slot) => OnSlotBeginDrag?.Invoke(slot);
            slotView.OnDragRequested += (pos) => OnSlotDrag?.Invoke(pos);
            slotView.OnEndDragRequested += () => OnSlotEndDrag?.Invoke();
            slotView.OnDropRequested += (slot) => OnSlotDrop?.Invoke(slot);

            slotViews.Add(slotView);
        }
    }

    private void InitializeButtons()
    {
        if (useButton != null)
            useButton.onClick.AddListener(() => OnUseItemRequested?.Invoke(currentSelectedSlot));

        if (dropButton != null)
            dropButton.onClick.AddListener(() => OnDropItemRequested?.Invoke(currentSelectedSlot));

        if (sortButton != null)
            sortButton.onClick.AddListener(() => OnSortRequested?.Invoke());
    }

    #region IInventoryView Implementation

    public void UpdateSlot(int slotIndex, InventoryItem item)
    {
        if (slotIndex >= 0 && slotIndex < slotViews.Count)
        {
            slotViews[slotIndex].UpdateSlot(item);
        }
    }

    public void ClearSlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < slotViews.Count)
        {
            slotViews[slotIndex].ClearSlot();
        }
    }

    public void ShowItemDetails(InventoryItem item)
    {
        if (itemDetailsPanel == null) return;

        itemDetailsPanel.SetActive(true);

        if (itemNameText != null)
            itemNameText.text = item.ItemName;

        if (itemDescriptionText != null)
            itemDescriptionText.text = item.Description;

        if (itemDetailIcon != null)
            itemDetailIcon.sprite = item.Icon;

        if (itemStatsText != null)
        {
            itemStatsText.text = $"Type: {item.ItemType}\n" +
                                 $"Category: {item.ItemCategory}\n" +
                                 $"Quality: {item.Quality}\n" +
                                 $"Sell Price: {item.SellPrice}";
        }

        // Enable/disable buttons based on item properties
        if (useButton != null)
            useButton.interactable = item.ItemType == ItemType.Consumable;

        if (dropButton != null)
            dropButton.interactable = !item.IsQuestItem && !item.IsArtifact;
    }

    public void HideItemDetails()
    {
        if (itemDetailsPanel != null)
            itemDetailsPanel.SetActive(false);

        currentSelectedSlot = -1;

        // Deselect all slots
        foreach (var slot in slotViews)
        {
            slot.SetSelected(false);
        }
    }

    public void ShowDragPreview(InventoryItem item)
    {
        if (dragPreviewObject == null) return;

        dragPreviewObject.SetActive(true);

        if (dragPreviewIcon != null)
            dragPreviewIcon.sprite = item.Icon;

        if (dragPreviewCanvasGroup != null)
            dragPreviewCanvasGroup.alpha = 0.6f;
    }

    public void UpdateDragPreview(Vector2 position)
    {
        if (dragPreviewObject != null)
        {
            dragPreviewObject.transform.position = position;
        }
    }

    public void HideDragPreview()
    {
        if (dragPreviewObject != null)
            dragPreviewObject.SetActive(false);
    }

    public void ShowNotification(string message)
    {
        if (notificationText == null) return;

        if (notificationCoroutine != null)
            StopCoroutine(notificationCoroutine);

        notificationCoroutine = StartCoroutine(ShowNotificationCoroutine(message));
    }

    #endregion

    private void HandleSlotClicked(int slotIndex)
    {
        // Deselect previous slot
        if (currentSelectedSlot >= 0 && currentSelectedSlot < slotViews.Count)
        {
            slotViews[currentSelectedSlot].SetSelected(false);
        }

        // Select new slot
        currentSelectedSlot = slotIndex;
        if (slotIndex >= 0 && slotIndex < slotViews.Count)
        {
            slotViews[slotIndex].SetSelected(true);
        }

        OnSlotClicked?.Invoke(slotIndex);
    }

    private System.Collections.IEnumerator ShowNotificationCoroutine(string message)
    {
        notificationText.text = message;
        notificationText.gameObject.SetActive(true);

        yield return new WaitForSeconds(notificationDuration);

        notificationText.gameObject.SetActive(false);
    }
}
