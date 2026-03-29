using UnityEngine;

[CreateAssetMenu(fileName = "QuestDatabase", menuName = "Game/Quest Database")]
[System.Obsolete("QuestDatabase is superseded by QuestCatalogService (runtime backend data). Do not use.")]
public class QuestDatabase : ScriptableObject
{
    public QuestModel[] quests;

    public QuestModel GetQuest(string questId)
    {
        foreach (var quest in quests)
        {
            if (quest.questId == questId)
                return quest;
        }

        return null;
    }

    // Get quest index by questId, return -1 if not found
    public int GetQuestIndex(string questId)
    {
        for (int i = 0; i < quests.Length; i++)
        {
            if (quests[i].questId == questId)
                return i;
        }

        return -1;
    }
}