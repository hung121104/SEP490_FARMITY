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
    [Header("Relationship")]
    [SerializeField] private NPCRelationshipModel relationshipModel;
    private DialogueNode interactionNode;
    private NPCState currentState = NPCState.Idle;
    private NPCDialoguePresenter presenter;
    private bool playerInRange;
    private enum NPCState
    {
        Idle,
        InteractionMenu,
        Dialogue
    }
    private void Awake()
    {
        INPCDialogueService service = new NPCDialogueService(dialogueModel);
        presenter = new NPCDialoguePresenter(service, dialogueView);
        CreateInteractionNode();
    }
    private void Update()
    {
        if (!playerInRange) return;

        // =====================
        // IDLE → Press E to open menu
        // =====================
        if (currentState == NPCState.Idle)
        {
            if (Input.GetKeyDown(interactKey))
            {
                ShowInteractionMenu();
            }
            return;
        }

        // =====================
        // INTERACTION MENU
        // =====================
        if (currentState == NPCState.InteractionMenu)
        {
            HandleInteractionMenuInput();
            return;
        }

        // =====================
        // DIALOGUE STATE 
        // =====================
        if (currentState == NPCState.Dialogue)
        {
            HandleDialogueUpdate();
        }
        
    }
    private void HandleInteractionMenuInput()
    {
        for (int i = 0; i < interactionNode.options.Count; i++)
        {
            if (i < optionKeys.Count && Input.GetKeyDown(optionKeys[i]))
            {
                if (i == 0) // talk
                {
                    dialogueView.Hide();
                    currentState = NPCState.Dialogue;
                    presenter.StartDialogue();
                }
                else if (i == 1) // send gift
                {
                    Debug.Log("Gift mode (chưa làm)");
                    dialogueView.Hide();
                    UnlockPlayer();
                    currentState = NPCState.Idle;
                }

                break;
            }
        }
    }
    private void HandleDialogueUpdate()
    {
        if (!presenter.IsDialogueActive())
        {
            dialogueView.Hide();
            UnlockPlayer();
            currentState = NPCState.Idle;
            return;
        }

        if (dialogueView.IsTyping())
        {
            if (Input.GetKeyDown(interactKey))
            {
                dialogueView.ShowFullText(presenter.GetCurrentNode());
            }
            return;
        }

        if (dialogueView.IsShowingOptions())
        {
            HandleOptionInput();
            return;
        }

        if (Input.GetKeyDown(interactKey))
        {
            presenter.Continue();

            if (!presenter.IsDialogueActive())
            {
                UnlockPlayer();
                currentState = NPCState.Idle;
            }
        }
    }
    private void CreateInteractionNode()
    {
        interactionNode = new DialogueNode();
        interactionNode.dialogueText = "What do you want to do?";

        interactionNode.options = new List<DialogueOption>
    {
        new DialogueOption { optionText = "Talk", nextNodeIndex = -1 },
        new DialogueOption { optionText = "Send Gift", nextNodeIndex = -1 }
    };
    }
    private void ShowInteractionMenu()
    {
        if (playerMovement != null)
            playerMovement.enabled = false;

        currentState = NPCState.InteractionMenu;

        dialogueView.ShowNode(
            dialogueModel.npcName,
            interactionNode,
            dialogueModel.avatar
        );
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
        if (dialogueView != null)
            dialogueView.Hide();

        UnlockPlayer();
        playerMovement = null;
        currentState = NPCState.Idle;
    }
    private void UnlockPlayer()
    {
        if (playerMovement != null)
            playerMovement.enabled = true;
    }
}
