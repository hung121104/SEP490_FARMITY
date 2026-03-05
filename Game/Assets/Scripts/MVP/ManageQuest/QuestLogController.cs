using UnityEngine;
using UnityEngine.UI;

public class QuestLogController : MonoBehaviour
{
    [SerializeField] private Button questButton;
    [SerializeField] private QuestLogView view;

    private QuestLogPresenter presenter;

    private void Start()
    {
        presenter = new QuestLogPresenter(view, QuestManager.QuestService);

        questButton.onClick.AddListener(OpenQuestLog);
    }

    private void OpenQuestLog()
    {
        presenter.OpenQuestLog();
    }
}