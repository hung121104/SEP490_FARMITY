using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class NPCDialogueView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text npcNameText;
    [SerializeField] private TMP_Text dialogueText;

    [Header("Avatar")]
    [SerializeField] private Image avatarImage;
    [SerializeField] private TMP_Text continueHintText;

    [Header("Typewriter")]
    [SerializeField] private float typingSpeed = 0.05f;

    private Coroutine typingCoroutine;
    private bool isTyping;

    public void Show(string npcName, string text, Sprite avatar)
    {
        dialoguePanel.SetActive(true);
        npcNameText.text = npcName;

        avatarImage.sprite = avatar;
        avatarImage.enabled = avatar != null;

        continueHintText.gameObject.SetActive(false);

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeText(text));
    }

    private IEnumerator TypeText(string text)
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char c in text)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
        continueHintText.gameObject.SetActive(true);
    }

    public bool IsTyping()
    {
        return isTyping;
    }

    public void ShowFullText(string fullText)
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        dialogueText.text = fullText;
        isTyping = false;
        continueHintText.gameObject.SetActive(true);
    }

    public void Hide()
    {
        dialoguePanel.SetActive(false);
        continueHintText.gameObject.SetActive(false);
    }
}
