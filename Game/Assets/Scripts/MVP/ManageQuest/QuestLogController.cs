using UnityEngine;
using UnityEngine.UI;

public class QuestLogController : MonoBehaviour
{
    [SerializeField] private Button questButton;
    [SerializeField] private QuestLogView view;

    private QuestLogPresenter presenter;

    private void Awake()
    {
        // Initialize in Awake so Subscribe() in OnEnable can safely reference presenter
        presenter = new QuestLogPresenter(view, QuestManager.QuestService);
        questButton.onClick.AddListener(() => presenter.OpenQuestLog());
    }

    private void OnEnable()  => presenter?.Subscribe();
    private void OnDisable() => presenter?.Unsubscribe();
}