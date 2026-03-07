using UnityEngine;

[CreateAssetMenu(fileName = "QuestDatabase", menuName = "Game/Quest Database")]
public class QuestDatabase : ScriptableObject
{
    public QuestModel[] quests;
}