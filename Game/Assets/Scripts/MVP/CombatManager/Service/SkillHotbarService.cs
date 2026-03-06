using UnityEngine;
using CombatManager.Model;

namespace CombatManager.Service
{
    /// <summary>
    /// Service for SkillHotbar.
    /// Mirrors SkillHotbarUI logic from CombatSystem (kept for legacy).
    /// </summary>
    public class SkillHotbarService : ISkillHotbarService
    {
        private SkillHotbarModel model;

        public SkillHotbarService(SkillHotbarModel model)
        {
            this.model = model;
        }

        public void Initialize()
        {
            model.isInitialized = true;
            Debug.Log("[SkillHotbarService] Initialized");
        }

        public bool IsInitialized() => model.isInitialized;

        /// <summary>
        /// Can only interact with hotbar when ManagementPanel is open.
        /// </summary>
        public bool CanInteract()
        {
            if (CombatManager.Presenter.SkillManagementPresenter.Instance == null)
                return false;
            return CombatManager.Presenter.SkillManagementPresenter.Instance.IsPanelOpen();
        }

        public void SetVisible(bool visible)
        {
            model.isVisible = visible;
        }

        public bool IsVisible() => model.isVisible;
    }
}