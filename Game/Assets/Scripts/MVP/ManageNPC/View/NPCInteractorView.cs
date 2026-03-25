    using System.Collections.Generic;
    using UnityEngine;

/// <summary>
/// Thin MonoBehaviour View for NPC interaction.
/// Owns Unity lifecycle and serialized references only.
/// All interaction logic lives in NPCInteractionPresenter (pure C#).
/// Implements INPCInteractorView so the Presenter can call back through an interface.
/// </summary>
public class NPCInteractorView : MonoBehaviour, INPCInteractorView
{
    [SerializeField] private NPCDialogueView dialogueView;
    [SerializeField] private NPCDialogueModel dialogueModel;

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

    // ─── Private state ───
    private NPCInteractionPresenter presenter;
    private PlayerMovement playerMovement;

    // ─────────────────────────────────────────────────────────────────
    // Unity lifecycle
    // ─────────────────────────────────────────────────────────────────

    private void Start()
    {
        presenter = new NPCInteractionPresenter(
            this,
            dialogueView,
            dialogueModel,
            questView,
            relationshipModel,
            giftDatabase,
            inventoryGameView,
            hotbarScript
        );
        presenter.Initialize();
    }

    private void Update() => presenter?.Update();

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("PlayerEntity")) return;
        playerMovement = other.GetComponent<PlayerMovement>();
        presenter?.OnPlayerEnter(playerMovement);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("PlayerEntity")) return;
        presenter?.OnPlayerExit();
        playerMovement = null;
    }

    // ─────────────────────────────────────────────────────────────────
    // INPCInteractorView — Unity callbacks for the Presenter
    // ─────────────────────────────────────────────────────────────────

    public void LockPlayer()
    {
        if (playerMovement != null) playerMovement.enabled = false;
    }

    public void UnlockPlayer()
    {
        if (playerMovement != null) playerMovement.enabled = true;
    }

    public void EnableHotbar(bool enable)
    {
        if (hotbarScript != null) hotbarScript.enabled = enable;
    }

    public void SetInventoryMenuRoot(bool active)
    {
        if (inventoryMenuRoot != null) inventoryMenuRoot.SetActive(active);
    }

    public void OpenInventory()  => inventoryGameView?.OpenInventory();
    public void CloseInventory() => inventoryGameView?.CloseInventory();
    public void NotifyExternalAction() => inventoryGameView?.NotifyExternalAction();

    public IInventoryService GetInventoryService() => inventoryGameView?.GetInventoryService();

    public void StartPresenterCoroutine(System.Collections.IEnumerator coroutine)
        => StartCoroutine(coroutine);
}
