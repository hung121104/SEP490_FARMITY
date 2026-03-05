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
    [Header("Gameplay Systems")]
    [SerializeField] private MonoBehaviour hotbarScript;
    [Header("Relationship")]
    [SerializeField] private NPCRelationshipModel relationshipModel;
    [Header("Gift System")]
    [SerializeField] private GiftDatabaseSO giftDatabase;
    [Header("Inventory")]
    [SerializeField] private InventoryGameView inventoryGameView;
    [SerializeField] private InventoryView inventoryView;
    [SerializeField] private GameObject inventoryMenuRoot;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Quest System")]
    [SerializeField] private QuestView questView;
    [SerializeField] private QuestDatabase questDatabase;
    [SerializeField] private int questIndex;
    [SerializeField] private QuestLogController questLogController;

    private IQuestService questService;
    private QuestPresenter questPresenter;
    private GiftPresenter giftPresenter;
    private DialogueNode interactionNode;
    private NPCState currentState = NPCState.Idle;
    private NPCDialoguePresenter presenter;
    private bool playerInRange;
    private bool blockInteractOnce;
    private enum NPCState
    {
        Idle,
        InteractionMenu,
        Dialogue,
        Gift,
        Quest,
        SimpleDialogue
    }
    private void Awake()
    {
        // Dialogue Service
        INPCDialogueService service = new NPCDialogueService(dialogueModel);

        // Quest Service
        questService = QuestManager.QuestService;

        if (questDatabase != null &&
    questDatabase.quests.Length > 0 &&
    questIndex < questDatabase.quests.Length)
        {
            questPresenter = new QuestPresenter(
                questView,
                questService,
                questDatabase.quests[questIndex],
                dialogueModel.npcName,
                dialogueModel.avatar
            );
        }
    

        // Dialogue Presenter
        presenter = new NPCDialoguePresenter(
            service,
            dialogueView,
            questPresenter
        );

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
            if (blockInteractOnce)
            {
                blockInteractOnce = false;
                return;
            }

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
        // GIFT STATE
        // =====================
        if (currentState == NPCState.Gift)
        {
            giftPresenter?.Update();
            return;
        }

        // =====================
        // SIMPLE DIALOGUE
        // =====================
        if (currentState == NPCState.SimpleDialogue)
        {
            if (Input.GetKeyDown(interactKey))
            {
                dialogueView.Hide();
                UnlockPlayer();
                currentState = NPCState.Idle;
                blockInteractOnce = true;
            }

            return;
        }

        // =====================
        // DIALOGUE STATE
        // =====================
        if (currentState == NPCState.Dialogue)
        {
            HandleDialogueUpdate();
            return;
        }

        // =====================
        // QUEST STATE
        // =====================
        if (currentState == NPCState.Quest)
        {
            HandleOptionInput();
            return;
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
                else if (i == 1) // GIFT
                {
                    dialogueView.Hide();
                    currentState = NPCState.Gift;
                    StartGiftMode();
                }
                else if (i == 2) // QUEST
                {
                    dialogueView.Hide();

                    QuestModel quest = questDatabase.quests[questIndex];

                    // QUEST CHƯA NHẬN
                    if (!questService.HasQuest(quest.questId))
                    {
                        currentState = NPCState.Quest;
                        questPresenter?.ShowQuest();
                    }

                    // QUEST ĐANG LÀM
                    else if (questService.IsQuestActive(quest.questId))
                    {
                        ShowSimpleDialogue(
                            "Thanks for your help. Please come back when you're finished."
                        );
                    }

                    // QUEST HOÀN THÀNH
                    else if (questService.IsQuestCompleted(quest.questId))
                    {
                        ShowSimpleDialogue(
                            "Great work! Thank you for completing the quest."
                        );
                    }
                }

                break;
                }
            }
        }



    private void StartGiftMode()
    {
        if (inventoryGameView == null || giftDatabase == null || inventoryView == null)
        {
            Debug.LogError("Missing reference in Gift setup!");
            currentState = NPCState.Idle;
            return;
        }

        if (hotbarScript != null)
            hotbarScript.enabled = false;

        if (inventoryMenuRoot != null)
            inventoryMenuRoot.SetActive(true);

        inventoryGameView.OpenInventory();

        IGiftService giftService = new GiftService(giftDatabase);
        var inventoryService = inventoryGameView.GetInventoryService();

        giftPresenter = new GiftPresenter(
            giftService,
            inventoryService,
            inventoryView,
            dialogueView,
            relationshipModel,
            dialogueModel   
        );

        giftPresenter.OnGiftFinished += ExitGiftMode;

        giftPresenter.OnRequestCloseInventory += () =>
        {
            inventoryGameView.CloseInventory();

            if (inventoryMenuRoot != null)
                inventoryMenuRoot.SetActive(false);
        };

        giftPresenter.StartGiftMode();
    }
    private void ExitGiftMode()
    {
        giftPresenter.StopGiftMode();
        giftPresenter.OnGiftFinished -= ExitGiftMode;

        inventoryGameView.CloseInventory();

        if (hotbarScript != null)
            hotbarScript.enabled = true;

        UnlockPlayer();
        currentState = NPCState.Idle;
        blockInteractOnce = true;
        dialogueView.Hide();
    }
    private void HandleDialogueUpdate()
    {
        if (!presenter.IsDialogueActive())
        {
            dialogueView.Hide();

            if (hotbarScript != null)
                hotbarScript.enabled = true;

            UnlockPlayer();

            currentState = NPCState.Idle;
            blockInteractOnce = true;   

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
        }
    }
    private void CreateInteractionNode()
        {
            interactionNode = new DialogueNode();
            interactionNode.dialogueText = "What do you want to do?";

        interactionNode.options = new List<DialogueOption>
{
    new DialogueOption { optionText = "Talk", nextNodeIndex = -1 },
    new DialogueOption { optionText = "Send Gift", nextNodeIndex = -1 },
    new DialogueOption { optionText = "Quest", nextNodeIndex = -1 }
    };
    }
    private void ShowInteractionMenu()
    {
        if (playerMovement != null)
            playerMovement.enabled = false;

        if (hotbarScript != null)
            hotbarScript.enabled = false;

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

        // =====================
        // QUEST OPTIONS
        // =====================
        if (currentState == NPCState.Quest)
        {
            if (Input.GetKeyDown(optionKeys[0])) // Accept
            {
                questPresenter.AcceptQuest();

                dialogueView.Hide();
                UnlockPlayer();
                currentState = NPCState.Idle;
            }
            else if (Input.GetKeyDown(optionKeys[1])) // Back
            {
                ShowInteractionMenu();
            }

            return;
        }

        // =====================
        // NORMAL DIALOGUE
        // =====================
        if (node == null || node.options == null) return;

        for (int i = 0; i < node.options.Count; i++)
        {
            if (i < optionKeys.Count && Input.GetKeyDown(optionKeys[i]))
            {
                presenter.SelectOption(i);

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

        if (hotbarScript != null)
            hotbarScript.enabled = true;

        UnlockPlayer();
        playerMovement = null;
        currentState = NPCState.Idle;
    }
    private void ShowSimpleDialogue(string message)
    {
        DialogueNode node = new DialogueNode();
        node.dialogueText = message;
        node.options = null;

        dialogueView.ShowNode(
            dialogueModel.npcName,
            node,
            dialogueModel.avatar
        );

        currentState = NPCState.SimpleDialogue;
    }
    private void UnlockPlayer()
        {
            if (playerMovement != null)
                playerMovement.enabled = true;
        }
    }
