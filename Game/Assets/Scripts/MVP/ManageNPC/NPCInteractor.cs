using UnityEngine;

public class NPCInteractor : MonoBehaviour
{
    [SerializeField] private NPCDialogueView dialogueView;
    [SerializeField] private NPCDialogueModel dialogueModel;

    [Header("Input Settings")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    private NPCDialoguePresenter presenter;
    private bool playerInRange;

    private void Awake()
    {
        var service = new NPCDialogueService(dialogueModel);
        presenter = new NPCDialoguePresenter(service, dialogueView);
    }

    private void Update()
    {
        if (!playerInRange) return;

        if (Input.GetKeyDown(interactKey))
        {
            if (dialogueView.IsTyping())
            {
                presenter.ShowFullCurrentText();
            }
            else
            {
                presenter.OnInteract();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("PlayerEntity")) return;

        playerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("PlayerEntity")) return;

        playerInRange = false;
    }
}
