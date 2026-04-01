using System;
using System.Collections;

namespace CombatManager.Service
{
    public struct PlayerHealthSnapshot
    {
        public int currentHealth;
    }

    public interface IPlayerHealthSyncService
    {
        bool IsInitialized { get; }

        IEnumerator InitializeAndFetch(Action<PlayerHealthSnapshot> onLoaded, Action<string> onError = null);
        void SetRuntimeSnapshot(PlayerHealthSnapshot snapshot, bool markDirty);
        IEnumerator FlushNow(float timeoutSeconds = 5f, Action<bool> onCompleted = null);
        void ForceFlush();
    }
}
