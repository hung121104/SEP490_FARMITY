using System;
using System.Collections;

namespace CombatManager.Service
{
    public struct PlayerProgressionSnapshot
    {
        public int level;
        public int currentExp;
        public int expToNextLevel;
        public int baseStrength;
        public int baseVitality;
    }

    public interface IPlayerProgressionSyncService
    {
        bool IsInitialized { get; }

        IEnumerator InitializeAndFetch(Action<PlayerProgressionSnapshot> onLoaded, Action<string> onError = null);
        void SetRuntimeSnapshot(PlayerProgressionSnapshot snapshot, bool markDirty);
        IEnumerator FlushNow(float timeoutSeconds = 5f, Action<bool> onCompleted = null);
        void ForceFlush();
    }
}
