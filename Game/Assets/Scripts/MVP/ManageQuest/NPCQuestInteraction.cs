using UnityEngine;

public class NPCQuestInteraction : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Header("References")]
    [SerializeField] private QuestDatabase questDatabase;
    [SerializeField] private QuestView questView;

    private IQuestService questService;
    private QuestPresenter presenter;

    private void Start()
    {
        questService = new QuestService();

        QuestModel quest = questDatabase.quests[0];

        presenter = new QuestPresenter(
            questView,
            questService,
            quest
        );
    }

    private void Update()
    {
        if (Input.GetKeyDown(interactKey))
        {
            if (questView.IsOpen())
            {
                questView.Toggle();
            }
            else
            {
                presenter.ShowQuest();
            }
        }
    }
}