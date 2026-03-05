using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class QuestLogView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Transform questListContainer;
    [SerializeField] private GameObject questItemPrefab;

    public void TogglePanel()
    {
        panelRoot.SetActive(!panelRoot.activeSelf);
    }

    public void ShowQuestList(List<QuestModel> quests)
    {
        // Clear old entries
        foreach (Transform child in questListContainer)
            Destroy(child.gameObject);

        foreach (var quest in quests)
        {
            GameObject entry = Instantiate(questItemPrefab, questListContainer);

            TMP_Text questNameText =
                entry.transform.Find("QuestNameText").GetComponent<TMP_Text>();

            TMP_Text objectiveText =
                entry.transform.Find("ObjectiveList/ObjectiveText").GetComponent<TMP_Text>();

            questNameText.text = quest.questName;
            objectiveText.text = quest.description;
        }
    }
}