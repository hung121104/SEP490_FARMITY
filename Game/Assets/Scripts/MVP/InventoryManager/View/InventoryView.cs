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

    [Header("Grid Settings")]
    [SerializeField] private int slotsPerRow = 9;
    [SerializeField] private float horizontalSpacing = 20f; 
    [SerializeField] private float verticalSpacing = 20f; 
    [SerializeField] private float emptyRowHeight = 10f; 
    [SerializeField] private int emptyRowAfterRows = 3;

    [Header("Other UI")]
    [SerializeField] private Button sortButton;

    [Header("Drag Preview")]
    [SerializeField] private Image dragPreviewIcon;
    [SerializeField] private CanvasGroup dragPreviewCanvasGroup;

    [Header("Delete Zone")]
    [SerializeField] private ItemDeleteView itemDeleteView;

    private List<InventorySlotView> slotViews = new List<InventorySlotView>();
    private List<GameObject> rowObjects = new List<GameObject>();
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
        ConfigureVerticalLayout();
    }

    #region Initialize
    private void ConfigureVerticalLayout()
    {
        // Add or configure VerticalLayoutGroup
        VerticalLayoutGroup verticalLayout = slotContainer.GetComponent<VerticalLayoutGroup>();
        if (verticalLayout == null)
        {
            verticalLayout = slotContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            Debug.Log("[InventoryView] Added VerticalLayoutGroup to slotContainer");
        }

        verticalLayout.spacing = verticalSpacing;
        verticalLayout.childAlignment = TextAnchor.MiddleCenter;
        verticalLayout.childControlWidth = false;
        verticalLayout.childControlHeight = false;
        verticalLayout.childForceExpandWidth = false;
        verticalLayout.childForceExpandHeight = false;
        verticalLayout.padding = new RectOffset(0, 0, 0, 0);
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

        // Clear existing rows
        foreach (var row in rowObjects)
        {
            if (row != null)
                Destroy(row);
        }
        rowObjects.Clear();

        // Clear all children in container
        foreach (Transform child in slotContainer)
        {
            Destroy(child.gameObject);
        }

        //Create rows and slots
        int slotIndex = 0;
        int currentRowNumber = 0;
        GameObject currentRowObject = null;
        Transform currentRowTransform = null;
        int slotsInCurrentRow = 0;

        // Create new slots
        for (int i = 0; i < slotCount; i++)
        {
            // Check if we need to start a new row
            if (slotsInCurrentRow == 0 || slotsInCurrentRow >= slotsPerRow)
            {
                // Create empty row
                if (currentRowNumber > 0 && currentRowNumber % emptyRowAfterRows == 0)
                {
                    CreateEmptyRow();
                }

                currentRowObject = CreateRow(currentRowNumber);
                currentRowTransform = currentRowObject.transform;
                rowObjects.Add(currentRowObject);
                slotsInCurrentRow = 0;
                currentRowNumber++;

                Debug.Log($"[InventoryView] Created row {currentRowNumber}");
            }

            GameObject slotObj = Instantiate(slotPrefab, currentRowTransform);
            InventorySlotView slotView = slotObj.GetComponent<InventorySlotView>();

            if (slotView == null)
            {
                Debug.LogError($"Slot prefab missing InventorySlotView component!");
                continue;
            }

            slotView.Initialize(i);

            int capturedIndex = slotIndex;
            // Subscribe to slot events
            slotView.OnClickedRequested += (slot) => HandleSlotClicked(slot);
            slotView.OnBeginDragRequested += (slot) => OnSlotBeginDrag?.Invoke(slot);
            slotView.OnDragRequested += (pos) => OnSlotDrag?.Invoke(pos);
            slotView.OnEndDragRequested += () => OnSlotEndDrag?.Invoke();
            slotView.OnDropRequested += (slot) => OnSlotDrop?.Invoke(slot);
            slotView.OnPointerEnterRequested += (slot, pos) => OnSlotHoverEnter?.Invoke(slot, pos);
            slotView.OnPointerExitRequested += (slot) => OnSlotHoverExit?.Invoke(slot);

            slotViews.Add(slotView);
            slotIndex++;
            slotsInCurrentRow++;
        }
    }

    private GameObject CreateRow(int rowNumber)
    {
        GameObject row = new GameObject($"Row_{rowNumber}");
        row.transform.SetParent(slotContainer, false);
        row.transform.localScale = Vector3.one;

        RectTransform rectTransform = row.GetComponent<RectTransform>();
        if (rectTransform == null)
            rectTransform = row.AddComponent<RectTransform>();

        // Create row with anchor and pivot at top center
        rectTransform.anchorMin = new Vector2(0.5f, 1f);
        rectTransform.anchorMax = new Vector2(0.5f, 1f);
        rectTransform.pivot = new Vector2(0.5f, 1f);

        // Add Horizontal Layout Group
        HorizontalLayoutGroup horizontalLayout = row.AddComponent<HorizontalLayoutGroup>();
        horizontalLayout.spacing = horizontalSpacing;
        horizontalLayout.childAlignment = TextAnchor.MiddleCenter;
        horizontalLayout.childControlWidth = false;
        horizontalLayout.childControlHeight = false;
        horizontalLayout.childForceExpandWidth = false;
        horizontalLayout.childForceExpandHeight = false;
        horizontalLayout.childScaleWidth = false;
        horizontalLayout.childScaleHeight = false;

        // Add Content Size Fitter for dynamic resizing
        ContentSizeFitter sizeFitter = row.AddComponent<ContentSizeFitter>();
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return row;
    }

    private GameObject CreateEmptyRow()
    {
        GameObject emptyRow = new GameObject("EmptyRow");
        emptyRow.transform.SetParent(slotContainer, false);
        emptyRow.transform.localScale = Vector3.one;

        RectTransform rectTransform = emptyRow.GetComponent<RectTransform>();
        if (rectTransform == null)
            rectTransform = emptyRow.AddComponent<RectTransform>();

        // Create row with anchor and pivot at top center
        rectTransform.anchorMin = new Vector2(0.5f, 1f);
        rectTransform.anchorMax = new Vector2(0.5f, 1f);
        rectTransform.pivot = new Vector2(0.5f, 1f);
        rectTransform.sizeDelta = new Vector2(0f, emptyRowHeight);

        // Set height for empty row
        LayoutElement layoutElement = emptyRow.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = emptyRowHeight;
        layoutElement.minHeight = emptyRowHeight;
        //layoutElement.flexibleHeight = 0;
        //layoutElement.flexibleWidth = 1;

        rowObjects.Add(emptyRow);

        return emptyRow;
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

    /// <summary>
    /// Programmatically assigns a delete zone after Awake (e.g. from CraftingInventoryAdapter).
    /// </summary>
    public void SetDeleteZone(ItemDeleteView deleteView)
    {
        // Unsubscribe from previous delete zone
        if (itemDeleteView != null)
        {
            itemDeleteView.OnItemDeleteRequested -= (slot) => OnItemDeleteRequested?.Invoke(slot);
        }

        itemDeleteView = deleteView;
        InitializeDeleteZone();
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
}
