using System;
using System.Collections;

namespace CombatManager.Service
{
    public interface ISkillLoadoutSyncService
    {
        bool IsInitialized { get; }
        bool HasServerSnapshot { get; }

        IEnumerator InitializeAndFetch(
            int targetSlotCount,
            Action<string[]> onLoaded,
            Action<string> onError = null);

        void SetRuntimeSnapshot(string[] slotSkillIds, bool markDirty);
        IEnumerator FlushNow(float timeoutSeconds = 5f, Action<bool> onCompleted = null);
        void ForceFlush();
    }
}
