using System;
using UnityEngine;

namespace CombatManager.Presenter
{
    /// <summary>
    /// Attach this to the spawned AoE VFX prefab and call OnImpactAnimationEvent from an animation event.
    /// It forwards timing control to the AoE presenter so damage is applied at the exact impact frame.
    /// </summary>
    public class AoESkillImpactEventRelay : MonoBehaviour
    {
        private Action onImpact;

        public void Initialize(Action onImpactCallback)
        {
            onImpact = onImpactCallback;
        }

        // Animation Event hook
        public void OnImpactAnimationEvent()
        {
            onImpact?.Invoke();
        }
    }
}
