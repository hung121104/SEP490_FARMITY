using UnityEngine;
using UnityEngine.InputSystem;

public class HotbarView : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private int hotbarSize = 9;
    [SerializeField] private int inventoryHotbarStartIndex = 27;

    [Header("UI References")]
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Transform slotsContainer;

    [Header("Visual Settings")]
    [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.7f);
    [SerializeField] private Color selectedColor = Color.white;

    [Header("Input Settings")]
    [SerializeField] private bool enableScrollWheel = true;
    [SerializeField] private bool enableLeftClick = true;

    [Header("Inventory Integration")]
    [SerializeField] private InventoryGameView inventoryGameView;

    private HotbarSlotUI[] slotUIs;
    private HotbarModel model;
    private HotbarPresenter presenter;
    private bool isInitialized = false;

    // Stored delegates so we can properly unsubscribe (lambdas can't be unsubscribed)
    private System.Action<InputAction.CallbackContext>[] _slotCallbacks;

    // Events
    public System.Action<int> OnSlotKeyPressed;
    public System.Action<float> OnScrollInput;
    public System.Action OnUseItemInput;

    private void Awake()
    {
        // Pre-build the 9 slot callbacks so OnEnable/OnDisable use the same delegate references
        _slotCallbacks = new System.Action<InputAction.CallbackContext>[9];
        for (int i = 0; i < 9; i++)
        {
            int slotIndex = i; // capture for closure
            _slotCallbacks[i] = ctx => OnHotbarSlotPressed(slotIndex);
        }

        CreateSlotUIs(hotbarSize);
    }

    private void Start()
    {
        InitializeHotbarSystem();
    }

    private void InitializeHotbarSystem()
    {
        if (isInitialized) return;

        if (inventoryGameView == null)
        {
            inventoryGameView = FindFirstObjectByType<InventoryGameView>();
        }

        if (inventoryGameView == null)
        {
            Debug.LogError("HotbarView: InventoryGameView not found");
            return;
        }

        var inventoryService = inventoryGameView.GetInventoryService();
        var inventoryModel = inventoryGameView.GetInventoryModel();

        if (inventoryService == null || inventoryModel == null)
        {
            Debug.LogWarning("HotbarView: Inventory not ready, retrying...");
            Invoke(nameof(InitializeHotbarSystem), 0.1f);
            return;
        }

        model = new HotbarModel(inventoryModel, inventoryHotbarStartIndex, hotbarSize);
        presenter = new HotbarPresenter(model, this, inventoryService);
        presenter.Initialize();

        isInitialized = true;
        // OnEnable fired before Start and skipped subscription because InputManager wasn't
        // ready. Now that everything is ready, subscribe to input for the first time.
        SubscribeInputEvents();
        Debug.Log("HotbarView: Initialized successfully");
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // New Input System – subscribe / unsubscribe
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private void OnEnable()
    {
        // On the very first enable, InputManager may not be ready yet and isInitialized is
        // still false.  Subscription is handled at the end of InitializeHotbarSystem() instead.
        // For all subsequent re-enables (e.g. after inventory open/close) we subscribe here.
        if (!isInitialized) return;
        SubscribeInputEvents();
    }

    private void OnDisable()
    {
        UnsubscribeInputEvents();
    }

    private void SubscribeInputEvents()
    {
        if (InputManager.Instance == null) return;

        // Hotbar slot keys 1-9 (stored delegates for proper unsubscription)
        for (int i = 0; i < 9; i++)
        {
            InputAction slotAction = InputManager.Instance.GetHotbarSlotAction(i);
            if (slotAction != null)
                slotAction.performed += _slotCallbacks[i];
        }

        // Scroll wheel
        if (enableScrollWheel)
            InputManager.Instance.ScrollItem.performed += OnScrollPerformed;

        // Use item (left click) — always subscribe; enableLeftClick is checked at fire time.
        InputManager.Instance.UseItem.performed += OnUseItemPerformed;
    }

    private void UnsubscribeInputEvents()
    {
        if (InputManager.Instance == null) return;

        for (int i = 0; i < 9; i++)
        {
            InputAction slotAction = InputManager.Instance.GetHotbarSlotAction(i);
            if (slotAction != null)
                slotAction.performed -= _slotCallbacks[i];
        }

        if (enableScrollWheel)
            InputManager.Instance.ScrollItem.performed -= OnScrollPerformed;

        // Always unsubscribe unconditionally to match the unconditional subscribe above.
        InputManager.Instance.UseItem.performed -= OnUseItemPerformed;
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Input callbacks
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private void OnHotbarSlotPressed(int slotIndex)
    {
        if (!isInitialized) return;
        OnSlotKeyPressed?.Invoke(slotIndex);
    }

    private void OnScrollPerformed(InputAction.CallbackContext ctx)
    {
        if (!isInitialized) return;
        float scrollValue = ctx.ReadValue<float>();
        if (scrollValue != 0f)
        {
            OnScrollInput?.Invoke(scrollValue);
        }
    }

    private void OnUseItemPerformed(InputAction.CallbackContext ctx)
    {
        if (!isInitialized) return;
        if (!enableLeftClick) return;   // suppressed by CropHarvestingView when targeting a crop
        OnUseItemInput?.Invoke();
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Unchanged methods
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private void CreateSlotUIs(int size)
    {
        foreach (Transform child in slotsContainer)
        {
            Destroy(child.gameObject);
        }

        slotUIs = new HotbarSlotUI[size];
        for (int i = 0; i < size; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotsContainer);
            slotObj.name = "HotbarSlot_" + i;

            HotbarSlotUI slotUI = slotObj.GetComponent<HotbarSlotUI>();
            if (slotUI != null)
            {
                slotUI.Initialize(i, this);
                slotUIs[i] = slotUI;
            }
        }
    }

    public Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePos = Mouse.current.position.ReadValue();
        mousePos.z = 10f;
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
        worldPos.z = 0;
        return worldPos;
    }

    public void UpdateSlotDisplay(int index, ItemModel item)
    {
        if (index >= 0 && index < slotUIs.Length)
        {
            slotUIs[index].UpdateDisplay(item);
        }
    }

    public void UpdateSelection(int selectedIndex)
    {
        for (int i = 0; i < slotUIs.Length; i++)
        {
            slotUIs[i].SetSelected(i == selectedIndex);
        }
    }

    public Color GetNormalColor() => normalColor;
    public Color GetSelectedColor() => selectedColor;
    public HotbarPresenter GetPresenter() => presenter;
    public ItemModel GetCurrentItem() => presenter?.GetCurrentItem();
    public bool IsInitialized() => isInitialized;

    private void OnDestroy()
    {
        presenter?.UnsubscribeEvents();
    }
}
