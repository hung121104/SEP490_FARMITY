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
        // Clear old UI
        foreach (Transform child in questListContainer)
            Destroy(child.gameObject);

        foreach (var quest in quests)
        {
            GameObject entry = Instantiate(questItemPrefab, questListContainer);

            TMP_Text questNameText =
                entry.transform.Find("QuestNameText").GetComponent<TMP_Text>();

            Transform objectiveList =
                entry.transform.Find("ObjectiveList");

            TMP_Text objectiveTemplate =
                objectiveList.GetChild(0).GetComponent<TMP_Text>();

            questNameText.text = quest.questName;

            // hide  template
            objectiveTemplate.gameObject.SetActive(false);

            foreach (var obj in quest.objectives)
            {
                TMP_Text objective =
                    Instantiate(objectiveTemplate, objectiveList);

                objective.gameObject.SetActive(true);

                objective.text =
                    obj.description + " " +
                    obj.currentAmount + "/" +
                    obj.requiredAmount;
            }
        }
    }
}