using UnityEngine;

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

    // Events
    public System.Action<int> OnSlotKeyPressed;
    public System.Action<float> OnScrollInput;
    public System.Action OnUseItemInput;

    private void Awake()
    {
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
        Debug.Log("HotbarView: Initialized successfully");
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
            slotObj.name = "HotbarSlot_" + i;

            HotbarSlotUI slotUI = slotObj.GetComponent<HotbarSlotUI>();
            if (slotUI != null)
            {
                slotUI.Initialize(i, this);
                slotUIs[i] = slotUI;
            }
        }
    }

    private void Update()
    {
        if (!isInitialized) return;

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
    public HotbarPresenter GetPresenter() => presenter;
    public InventoryItem GetCurrentItem() => presenter?.GetCurrentItem();
    public bool IsInitialized() => isInitialized;

    private void OnDestroy()
    {
        presenter?.UnsubscribeEvents();
    }
}
