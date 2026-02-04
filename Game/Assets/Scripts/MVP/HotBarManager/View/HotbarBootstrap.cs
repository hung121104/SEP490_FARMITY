using UnityEngine;

public class HotbarBootstrap : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private int hotbarSize = 9;

    [Header("References")]
    [SerializeField] private HotbarView hotbarView;
    [SerializeField] private ServiceFactory serviceFactory;

    private HotbarModel model;
    private HotbarPresenter presenter;
    private IHotbarService service;

    private void Awake()
    {
        InitializeHotbarSystem();
    }

    private void InitializeHotbarSystem()
    {
        // Auto-find components if not assigned
        if (hotbarView == null) hotbarView = GetComponent<HotbarView>();
        if (serviceFactory == null) serviceFactory = FindObjectOfType<ServiceFactory>();

        // Create Model
        model = new HotbarModel(hotbarSize);

        // Create Service through factory
        service = CreateHotbarService();

        // Create Presenter
        presenter = new HotbarPresenter(model, hotbarView, service);
        presenter.Initialize();

        Debug.Log("✅ Hotbar MVP system initialized completely");
    }

    private IHotbarService CreateHotbarService()
    {
        if (serviceFactory != null)
        {
            return serviceFactory.CreateHotbarService();
        }
        else
        {
            Debug.LogWarning("⚠️ ServiceFactory not found, creating fallback service");
            var itemUsageService = new ItemUsageService();
            return new HotbarService(itemUsageService);
        }
    }

    private void OnDestroy()
    {
        presenter?.UnsubscribeEvents();
    }

    // Public API
    public HotbarModel GetModel() => model;
    public HotbarPresenter GetPresenter() => presenter;
    public HotbarView GetView() => hotbarView;
}
