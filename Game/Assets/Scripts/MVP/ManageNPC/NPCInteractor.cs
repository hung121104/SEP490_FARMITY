using UnityEngine;
using System.Collections.Generic;

public class NPCInteractor : MonoBehaviour
{
    private PlayerMovement playerMovement;   
    [SerializeField] private NPCDialogueView dialogueView;
    [SerializeField] private NPCDialogueModel dialogueModel;
    [Header("Input Settings")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [Header("Option Keys")]
    [SerializeField]
    private List<KeyCode> optionKeys = new List<KeyCode>
    {
        KeyCode.Alpha1,
        KeyCode.Alpha2,
        KeyCode.Alpha3
    };
    private NPCDialoguePresenter presenter;
    private bool playerInRange;
    private void Awake()
    {
        INPCDialogueService service = new NPCDialogueService(dialogueModel);
        presenter = new NPCDialoguePresenter(service, dialogueView);
    }
    private void Update()
    {
        if (!playerInRange) return;
        // Dialogue not started yet
        if (!presenter.IsDialogueActive())
        {
            if (Input.GetKeyDown(interactKey))
            {
                if (playerMovement != null)
                    playerMovement.enabled = false;

                presenter.StartDialogue();
            }
            return;
        }
        // If text is typing → press E to skip
        if (dialogueView.IsTyping())
        {
            if (Input.GetKeyDown(interactKey))
            {
                dialogueView.ShowFullText(presenter.GetCurrentNode());
            }
            return;
        }
        // If current node has options
        if (dialogueView.IsShowingOptions())
        {
            HandleOptionInput();
            return;
        }
        // Linear dialogue
        if (Input.GetKeyDown(interactKey))
        {
            presenter.Continue();

            // If dialogue ended → unlock movement
            if (!presenter.IsDialogueActive())
            {
                UnlockPlayer();
            }
        }
    }
   private void HandleOptionInput()
    {
        var node = presenter.GetCurrentNode();
        if (node == null || node.options == null) return;

        for (int i = 0; i < node.options.Count; i++)
        {
            if (i < optionKeys.Count && Input.GetKeyDown(optionKeys[i]))
            {
                presenter.SelectOption(i);

                // If dialogue ended after selecting option
                if (!presenter.IsDialogueActive())
                {
                    UnlockPlayer();
                }

                break;
            }
        }
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("PlayerEntity")) return;

        playerInRange = true;

        // Get PlayerMovement from the player that entered
        playerMovement = other.GetComponent<PlayerMovement>();
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("PlayerEntity")) return;

        playerInRange = false;
        dialogueView.Hide();

        UnlockPlayer();
        playerMovement = null;
    }
    private void UnlockPlayer()
    {
        if (playerMovement != null)
            playerMovement.enabled = true;
    }
}
