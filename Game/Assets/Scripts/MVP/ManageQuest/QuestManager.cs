using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static IQuestService QuestService;

    private void Awake()
    {
        QuestService = new QuestService();
    }
}