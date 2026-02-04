using UnityEngine;

public class HotbarView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Transform slotsContainer;

    [Header("Visual Settings")]
    [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.7f);
    [SerializeField] private Color selectedColor = Color.white;

    [Header("Input Settings")]
    [SerializeField] private bool enableScrollWheel = true;
    [SerializeField] private bool enableLeftClick = true;
    [SerializeField] private bool enableRightClick = false;

    private HotbarSlotUI[] slotUIs;

    // Events for Presenter to subscribe
    public System.Action<int> OnSlotKeyPressed;
    public System.Action<float> OnScrollInput;
    public System.Action OnUseItemInput;
    public System.Action OnUseItemAlternateInput;

    #region Unity Lifecycle

    private void Update()
    {
        HandleInput();
    }

    #endregion

    #region Input Handling - From Player/Game Screen

    private void HandleInput()
    {
        HandleSlotSelection();
        HandleItemUsage();
    }

    private void HandleSlotSelection()
    {
        // Number keys 1-9 for slot selection
        for (int i = 0; i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                OnSlotKeyPressed?.Invoke(i);
                return;
            }
        }

        // Mouse scroll wheel for slot navigation
        if (enableScrollWheel)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0f)
            {
                OnScrollInput?.Invoke(1f); // Previous slot
            }
            else if (scroll < 0f)
            {
                OnScrollInput?.Invoke(-1f); // Next slot
            }
        }
    }

    private void HandleItemUsage()
    {
        // Primary use (left click)
        if (enableLeftClick && Input.GetMouseButtonDown(0))
        {
            OnUseItemInput?.Invoke();
        }

        // Secondary use (right click)
        if (enableRightClick && Input.GetMouseButtonDown(1))
        {
            OnUseItemAlternateInput?.Invoke();
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

    #region UI Rendering - Called by Presenter

    public void Initialize(int hotbarSize)
    {
        CreateSlotUIs(hotbarSize);
    }

    private void CreateSlotUIs(int size)
    {
        // Clear existing slot UI elements
        foreach (Transform child in slotsContainer)
        {
            Destroy(child.gameObject);
        }

        // Create new slot UI elements
        slotUIs = new HotbarSlotUI[size];
        for (int i = 0; i < size; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotsContainer);
            slotObj.name = $"Slot_{i}";

            HotbarSlotUI slotUI = slotObj.GetComponent<HotbarSlotUI>();
            if (slotUI != null)
            {
                slotUI.Initialize(i, this);
                slotUIs[i] = slotUI;
            }
            else
            {
                Debug.LogError($"❌ SlotPrefab missing HotbarSlotUI component!");
            }
        }

        Debug.Log($"✅ Created {slotUIs.Length} hotbar slots");
    }

    public void UpdateSlotDisplay(int index, HotbarSlot slot)
    {
        if (index >= 0 && index < slotUIs.Length)
        {
            slotUIs[index].UpdateDisplay(slot);
        }
    }

    public void UpdateSelection(int selectedIndex)
    {
        for (int i = 0; i < slotUIs.Length; i++)
        {
            slotUIs[i].SetSelected(i == selectedIndex);
        }
    }

    public void RefreshAll(HotbarSlot[] slots)
    {
        for (int i = 0; i < slotUIs.Length && i < slots.Length; i++)
        {
            UpdateSlotDisplay(i, slots[i]);
        }
    }

    public Color GetNormalColor() => normalColor;
    public Color GetSelectedColor() => selectedColor;

    #endregion

    #region Public API - For debugging/external access

    public void SetInputEnabled(bool enabled)
    {
        enableLeftClick = enabled;
        enableScrollWheel = enabled;
        enableRightClick = enabled;
    }

    #endregion
}
