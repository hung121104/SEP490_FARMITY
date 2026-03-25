using System.Collections;


public interface INPCInteractorView
{
 
    void LockPlayer();

    void UnlockPlayer();

    void EnableHotbar(bool enable);

    void SetInventoryMenuRoot(bool active);
    void OpenInventory();
    void CloseInventory();
    void NotifyExternalAction();
    IInventoryService GetInventoryService();
    void StartPresenterCoroutine(IEnumerator coroutine);
}
