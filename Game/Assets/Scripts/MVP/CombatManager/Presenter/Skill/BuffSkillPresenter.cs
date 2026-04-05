using System.Collections;
using CombatManager.Model;
using UnityEngine;

namespace CombatManager.Presenter
{
    /// <summary>
    /// Handles all buff-type skills (SkillCategory.Buff).
    /// Internal behavior is selected by SkillData.buffSubCategory.
    /// </summary>
    public class BuffSkillPresenter : SkillPatternPresenter
    {
        public static BuffSkillPresenter Instance { get; private set; }

        private SkillData currentSkillData;

        protected override void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            base.Awake();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void SetSkillData(SkillData skillData)
        {
            currentSkillData = skillData;

            if (skillData != null)
            {
                skillCooldown = skillData.cooldown;
                skillTier = skillData.diceTier;
                skillMultiplier = skillData.skillMultiplier;
                SyncModelFromSkillData();

                Debug.Log($"[BuffSkillPresenter] SkillData set: {skillData.skillName}");
            }
        }

        public SkillData GetCurrentSkillData() => currentSkillData;

        protected override SkillIndicatorData GetIndicatorData()
        {
            // Buff skills are self-cast for now.
            return null;
        }

        protected override bool ShouldUseDiceRollFlow() => false;

        protected override IEnumerator OnExecute(int finalDamage, Vector3 direction)
        {
            if (currentSkillData == null)
            {
                Debug.LogWarning("[BuffSkillPresenter] No SkillData assigned!");
                yield break;
            }

            ApplyBuffLogic(currentSkillData);
            yield return null;
        }

        protected override void OnStart() =>
            Debug.Log("[BuffSkillPresenter] Ready!");

        protected override void OnChargeStart() =>
            Debug.Log($"[BuffSkillPresenter] Charging: {currentSkillData?.skillName}");

        protected override void OnAttackStart() =>
            Debug.Log($"[BuffSkillPresenter] Casting: {currentSkillData?.skillName}");

        protected override void OnAttackEnd() =>
            Debug.Log($"[BuffSkillPresenter] Done: {currentSkillData?.skillName}");

        protected override void OnSkillCancelled() =>
            Debug.Log($"[BuffSkillPresenter] Cancelled: {currentSkillData?.skillName}");

        private void ApplyBuffLogic(SkillData skillData)
        {
            switch (skillData.buffSubCategory)
            {
                case BuffSkillSubCategory.InstantHeal:
                    ApplyInstantHeal(skillData);
                    break;
                case BuffSkillSubCategory.HealOverTime:
                    ApplyHealOverTime(skillData);
                    break;
                case BuffSkillSubCategory.StaminaRegen:
                    ApplyStaminaRegen(skillData);
                    break;
                case BuffSkillSubCategory.MoveSpeedPercent:
                    ApplyMoveSpeedPercent(skillData);
                    break;
                default:
                    Debug.LogWarning($"[BuffSkillPresenter] Unsupported Buff sub-category: {skillData.buffSubCategory} (Skill={skillData.skillName})");
                    break;
            }
        }

        private void ApplyInstantHeal(SkillData skillData)
        {
            var healthPresenter = PlayerHealthPresenter.FindLocal();
            if (healthPresenter == null)
            {
                Debug.LogWarning("[BuffSkillPresenter] InstantHeal failed: local PlayerHealthPresenter not found.");
                return;
            }

            int healAmount = Mathf.RoundToInt(Mathf.Max(0f, skillData.buffValue));
            if (healAmount <= 0)
            {
                Debug.LogWarning($"[BuffSkillPresenter] InstantHeal skipped: buffValue={skillData.buffValue}");
                return;
            }

            healthPresenter.ChangeHealth(healAmount);
            Debug.Log($"[BuffSkillPresenter] InstantHeal applied: +{healAmount} HP | Skill={skillData.skillName}");
        }

        private void ApplyHealOverTime(SkillData skillData)
        {
            float duration = Mathf.Max(0.1f, skillData.buffDuration);
            float tickInterval = Mathf.Max(0.1f, skillData.buffTickInterval);
            int healPerTick = Mathf.RoundToInt(Mathf.Max(0f, skillData.buffValue));

            if (healPerTick <= 0)
            {
                Debug.LogWarning($"[BuffSkillPresenter] HealOverTime skipped: buffValue={skillData.buffValue}");
                return;
            }

            StartCoroutine(HealOverTimeRoutine(skillData.skillName, healPerTick, duration, tickInterval));
        }

        private IEnumerator HealOverTimeRoutine(string skillName, int healPerTick, float duration, float tickInterval)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                var healthPresenter = PlayerHealthPresenter.FindLocal();
                if (healthPresenter != null)
                {
                    healthPresenter.ChangeHealth(healPerTick);
                }

                yield return new WaitForSeconds(tickInterval);
                elapsed += tickInterval;
            }

            Debug.Log($"[BuffSkillPresenter] HealOverTime completed: Skill={skillName}, healPerTick={healPerTick}, duration={duration}, tickInterval={tickInterval}");
        }

        private void ApplyStaminaRegen(SkillData skillData)
        {
            var staminaView = StaminaView.FindLocal();
            if (staminaView == null)
            {
                Debug.LogWarning("[BuffSkillPresenter] StaminaRegen failed: local StaminaView not found.");
                return;
            }

            float regenMultiplier = ConvertPercentOrMultiplier(skillData.buffValue);
            float duration = Mathf.Max(0.1f, skillData.buffDuration);

            if (regenMultiplier <= 1f)
            {
                Debug.LogWarning($"[BuffSkillPresenter] StaminaRegen skipped: buffValue={skillData.buffValue} -> multiplier={regenMultiplier}");
                return;
            }

            staminaView.ApplyConsumableEffects(0f, regenMultiplier, 0f, duration);
            Debug.Log($"[BuffSkillPresenter] StaminaRegen applied: x{regenMultiplier:F2} for {duration:F1}s | Skill={skillData.skillName}");
        }

        private void ApplyMoveSpeedPercent(SkillData skillData)
        {
            PlayerMovement movement = playerMovement;
            if (movement == null && playerTransform != null)
                movement = playerTransform.GetComponent<PlayerMovement>();

            if (movement == null)
            {
                Debug.LogWarning("[BuffSkillPresenter] MoveSpeedPercent failed: local PlayerMovement not found.");
                return;
            }

            float speedMultiplier = ConvertPercentOrMultiplier(skillData.buffValue);
            float duration = Mathf.Max(0.1f, skillData.buffDuration);

            if (speedMultiplier <= 1f)
            {
                Debug.LogWarning($"[BuffSkillPresenter] MoveSpeedPercent skipped: buffValue={skillData.buffValue} -> multiplier={speedMultiplier}");
                return;
            }

            movement.ApplyExternalSpeedBuff(speedMultiplier, duration);
            Debug.Log($"[BuffSkillPresenter] MoveSpeedPercent applied: x{speedMultiplier:F2} for {duration:F1}s | Skill={skillData.skillName}");
        }

        private static float ConvertPercentOrMultiplier(float rawValue)
        {
            // Accept either 1.2 (multiplier) or 20 (percent).
            if (rawValue <= 0f)
                return 1f;

            if (rawValue >= 0f && rawValue <= 5f)
                return rawValue;

            return 1f + (rawValue / 100f);
        }

        private void SyncModelFromSkillData()
        {
            model.skillCooldown = skillCooldown;
            model.skillTier = skillTier;
            model.skillMultiplier = skillMultiplier;
        }
    }
}
