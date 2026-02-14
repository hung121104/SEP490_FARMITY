using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryView : MonoBehaviour, IInventoryView
{
    [Header("UI Panels")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private GameObject dragPreviewObject;

    [Header("Slot Container")]
    [SerializeField] private Transform slotContainer;
    [SerializeField] private GameObject slotPrefab;

    [Header("Other UI")]
    [SerializeField] private Button sortButton;
    [SerializeField] private TextMeshProUGUI notificationText;
    [SerializeField] private float notificationDuration = 2f;

    [Header("Drag Preview")]
    [SerializeField] private Image dragPreviewIcon;
    [SerializeField] private CanvasGroup dragPreviewCanvasGroup;

    [Header("Delete Zone")]
    [SerializeField] private ItemDeleteView itemDeleteView;

    private List<InventorySlotView> slotViews = new List<InventorySlotView>();
    private Coroutine notificationCoroutine;

    public bool IsVisible => inventoryPanel != null && inventoryPanel.activeSelf;

    #region Events

    public event Action<int> OnSlotClicked;
    public event Action<int> OnSlotBeginDrag;
    public event Action<Vector2> OnSlotDrag;
    public event Action OnSlotEndDrag;
    public event Action<int> OnSlotDrop;
    public event Action<int> OnUseItemRequested;
    public event Action<int> OnDropItemRequested;
    public event Action OnSortRequested;
    public event Action<int, Vector2> OnSlotHoverEnter;
    public event Action<int> OnSlotHoverExit;
    public event Action<int> OnItemDeleteRequested;

    #endregion

    private void Awake()
    {
        InitializeButtons();
        HideDragPreview();
        InitializeDeleteZone();

        if (notificationText != null)
            notificationText.gameObject.SetActive(false);
    }

    #region Initialize

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

            if (slotView == null)
            {
                Debug.LogError($"Slot prefab missing InventorySlotView component!");
                continue;
            }

            slotView.Initialize(i);

            // Subscribe to slot events
            slotView.OnClickedRequested += (slot) => HandleSlotClicked(slot);
            slotView.OnBeginDragRequested += (slot) => OnSlotBeginDrag?.Invoke(slot);
            slotView.OnDragRequested += (pos) => OnSlotDrag?.Invoke(pos);
            slotView.OnEndDragRequested += () => OnSlotEndDrag?.Invoke();
            slotView.OnDropRequested += (slot) => OnSlotDrop?.Invoke(slot);
            slotView.OnPointerEnterRequested += (slot, pos) => OnSlotHoverEnter?.Invoke(slot, pos);
            slotView.OnPointerExitRequested += (slot) => OnSlotHoverExit?.Invoke(slot);

            slotViews.Add(slotView);
        }
    }

    private void InitializeButtons()
    {
        if (sortButton != null)
            sortButton.onClick.AddListener(() => OnSortRequested?.Invoke());
    }

    private void InitializeDeleteZone()
    {
        if (itemDeleteView != null)
        {
            itemDeleteView.OnItemDeleteRequested += (slot) => OnItemDeleteRequested?.Invoke(slot);
        }
    }

    #endregion 

    #region IInventoryView Implementation

    public void UpdateSlot(int slotIndex, ItemModel item)
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

    public void ShowDragPreview(ItemModel item)
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

    public void CancelAllActions()
    {
        ForceStopDragInEventSystem();

        // 1. Hide drag preview
        HideDragPreview();

        // 2. Stop notification coroutine
        if (notificationCoroutine != null)
        {
            StopCoroutine(notificationCoroutine);
            notificationCoroutine = null;
        }

        // 3. Hide notification text
        if (notificationText != null)
        {
            notificationText.gameObject.SetActive(false);
        }

        // 4. Reset all slots hover state
        foreach (var slotView in slotViews)
        {
            if (slotView != null)
            {
                slotView.ForceResetState();
            }
        }

        // 5. Reset delete zone visual state
        if (itemDeleteView != null)
        {
            itemDeleteView.ForceResetState();
        }

        Debug.Log("[InventoryView] All actions cancelled");
    }

    //Force stop drag operation in EventSystem
    private void ForceStopDragInEventSystem()
    {
        if (EventSystem.current == null)
        {
            Debug.LogWarning("[InventoryView] No EventSystem found");
            return;
        }

        // Get current EventSystem
        var eventSystem = EventSystem.current;

        // Check if there's an active drag
        var currentEventData = eventSystem.currentInputModule?.GetComponent<BaseInput>();

        // Set the pointerDrag to null to clear drag state
        var pointerData = new PointerEventData(eventSystem)
        {
            position = Input.mousePosition
        };

        // Get all raycast results at current position
        var raycastResults = new List<RaycastResult>();
        eventSystem.RaycastAll(pointerData, raycastResults);

        // Clear any active drag from EventSystem
        if (eventSystem.currentInputModule != null)
        {
            // Force clear by simulating pointer up on all objects
            foreach (var slot in slotViews)
            {
                if (slot != null)
                {
                    ExecuteEvents.Execute(slot.gameObject, pointerData, ExecuteEvents.endDragHandler);
                    ExecuteEvents.Execute(slot.gameObject, pointerData, ExecuteEvents.pointerUpHandler);
                }
            }
        }
    }
    #endregion

    private void HandleSlotClicked(int slotIndex)
    {
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
