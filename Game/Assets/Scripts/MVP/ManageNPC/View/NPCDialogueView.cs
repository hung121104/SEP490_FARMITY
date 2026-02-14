using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;

public class NPCDialogueView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text npcNameText;
    [SerializeField] private TMP_Text dialogueText;

    [Header("Avatar")]
    [SerializeField] private Image avatarImage;
    [SerializeField] private TMP_Text continueHintText;

    [Header("Options")]
    [SerializeField] private Transform optionsContainer;
    [SerializeField] private TMP_Text optionPrefab;

    [Header("Typewriter")]
    [SerializeField] private float typingSpeed = 0.05f;

    private Coroutine typingCoroutine;
    private bool isTyping;

    private List<TMP_Text> currentOptions = new List<TMP_Text>();

    
    // SHOW NODE 
    
    public void ShowNode(string npcName, DialogueNode node, Sprite avatar)
    {
        dialoguePanel.SetActive(true);

        npcNameText.text = npcName;

        avatarImage.sprite = avatar;
        avatarImage.enabled = avatar != null;

        ClearOptions();
        continueHintText.gameObject.SetActive(false);

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeText(node));
    }

    private IEnumerator TypeText(DialogueNode node)
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char c in node.dialogueText)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;

        // show options or continue hint
        if (node.options != null && node.options.Count > 0)
        {
            ShowOptions(node);
        }
        else
        {
            continueHintText.gameObject.SetActive(true);
        }
    }

    private void ShowOptions(DialogueNode node)
    {
        for (int i = 0; i < node.options.Count; i++)
        {
            TMP_Text optionText = Instantiate(optionPrefab, optionsContainer);
            optionText.text = $"{i + 1}. {node.options[i].optionText}";
            currentOptions.Add(optionText);
        }
    }

    public bool IsShowingOptions()
    {
        return currentOptions.Count > 0;
    }

    public bool IsTyping()
    {
        return isTyping;
    }

    public void ShowFullText(DialogueNode node)
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        dialogueText.text = node.dialogueText;
        isTyping = false;

        if (node.options != null && node.options.Count > 0)
        {
            ShowOptions(node);
        }
        else
        {
            continueHintText.gameObject.SetActive(true);
        }
    }

    public void Hide()
    {
        dialoguePanel.SetActive(false);
        continueHintText.gameObject.SetActive(false);
        ClearOptions();
    }

    private void ClearOptions()
    {
        foreach (var option in currentOptions)
        {
            Destroy(option.gameObject);
        }

        currentOptions.Clear();
    }
}
