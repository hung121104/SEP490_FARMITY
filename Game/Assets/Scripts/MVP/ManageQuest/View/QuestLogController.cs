using UnityEngine;
using UnityEngine.UI;

public class QuestLogController : MonoBehaviour
{
    [SerializeField] private Button questButton;
    [SerializeField] private QuestLogView view;

    private QuestLogPresenter presenter;

    private void Start()
    {
        // Start() runs after all Awake() — QuestManager.Instance is guaranteed non-null here.
        presenter = new QuestLogPresenter(QuestManager.Instance.QuestService);

        // View subscribes to Presenter event — Presenter never calls View directly.
        presenter.OnItemsRefreshed += view.ShowQuestList;

        // View calls Presenter (and handles its own toggle).
        questButton.onClick.AddListener(() =>
        {
            view.TogglePanel();       // View owns its own toggle
            presenter.OpenQuestLog(); // View calls Presenter to refresh data
        });

        // OnEnable fired before Start (presenter was null then), so subscribe manually now.
        presenter.Subscribe();
    }

    private void OnEnable()  => presenter?.Subscribe();
    private void OnDisable() => presenter?.Unsubscribe();

    private void OnDestroy()
    {
        if (presenter != null)
            presenter.OnItemsRefreshed -= view.ShowQuestList;
    }
}