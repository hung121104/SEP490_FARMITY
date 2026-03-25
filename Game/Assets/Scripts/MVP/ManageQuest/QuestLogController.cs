using UnityEngine;
using UnityEngine.UI;

public class QuestLogController : MonoBehaviour
{
    [SerializeField] private Button questButton;
    [SerializeField] private QuestLogView view;

    private QuestLogPresenter presenter;

    private void Start()
    {
        // Start() runs after all Awake() — QuestManager.QuestService is guaranteed non-null here.
        presenter = new QuestLogPresenter(view, QuestManager.QuestService);
        questButton.onClick.AddListener(() => presenter.OpenQuestLog());
        // OnEnable fired before Start (presenter was null then), so subscribe manually now.
        presenter.Subscribe();
    }

    private void OnEnable()  => presenter?.Subscribe();
    private void OnDisable() => presenter?.Unsubscribe();
}