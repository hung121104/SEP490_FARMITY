using UnityEngine;

public class HotbarView : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private int hotbarSize = 9;
    [SerializeField] private int inventoryHotbarStartIndex = 27; // 9 last slot  (27-35)

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
    private IInventoryService inventoryService;
    private bool isInitialized = false;

    // Events
    public System.Action<int> OnSlotKeyPressed;
    public System.Action<float> OnScrollInput;
    public System.Action OnUseItemInput;

    #region Initialization

    private void Awake()
    {
        InitializeHotbarSystem();
    }

    private void InitializeHotbarSystem()
    {
        // Get Inventory Service
        if (inventoryGameView == null)
        {
            inventoryGameView = FindFirstObjectByType<InventoryGameView>();
        }

        if (inventoryGameView == null)
        {
            Debug.LogError("❌ InventoryGameView not found! Hotbar requires Inventory system.");
            return;
        }

        inventoryService = inventoryGameView.GetInventoryService();
        InventoryModel inventoryModel = inventoryGameView.GetInventoryModel();

        // Create Model
        model = new HotbarModel(inventoryModel, inventoryHotbarStartIndex, hotbarSize);

        // Create Presenter
        presenter = new HotbarPresenter(model, this, inventoryService);

        // Initialize UI
        CreateSlotUIs(hotbarSize);
        presenter.Initialize();

        Debug.Log($"✅ Hotbar system initialized - Connected to Inventory slots {inventoryHotbarStartIndex}-{inventoryHotbarStartIndex + hotbarSize - 1}");
    }

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
            slotObj.name = $"HotbarSlot_{i}";

            HotbarSlotUI slotUI = slotObj.GetComponent<HotbarSlotUI>();
            if (slotUI != null)
            {
                slotUI.Initialize(i, this);
                slotUIs[i] = slotUI;
            }
        }

        Debug.Log($"✅ Created {slotUIs.Length} hotbar slots");
    }

    #endregion

    #region Input Handling

    private void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        HandleSlotSelection();
        HandleItemUsage();
    }

    private void HandleSlotSelection()
    {
        for (int i = 0; i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                OnSlotKeyPressed?.Invoke(i);
                return;
            }
        }

        if (enableScrollWheel)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0f)
            {
                OnScrollInput?.Invoke(scroll);
            }
        }
    }

    private void HandleItemUsage()
    {
        if (enableLeftClick && Input.GetMouseButtonDown(0))
        {
            OnUseItemInput?.Invoke();
        }
    }

    public Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 10f;
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
        worldPos.z = 0;
        return worldPos;
    }

    #endregion

    #region UI Updates

    public void UpdateSlotDisplay(int index, InventoryItem item)
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

    #endregion

    #region Public API

    public HotbarPresenter GetPresenter() => presenter;
    public InventoryItem GetCurrentItem() => presenter?.GetCurrentItem();

    #endregion

    private void OnDestroy()
    {
        presenter?.UnsubscribeEvents();
    }
}
