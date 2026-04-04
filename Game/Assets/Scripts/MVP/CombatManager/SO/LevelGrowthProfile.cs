using System;
using System.Collections.Generic;
using UnityEngine;

namespace CombatManager.SO
{
    [Serializable]
    public struct LevelGrowthEntry
    {
        [Min(1)] public int level;
        public int strength;
        public int vitality;
        public int endurance;
    }

    [CreateAssetMenu(fileName = "LevelGrowthProfile", menuName = "Combat/Level Growth Profile")]
    public class LevelGrowthProfile : ScriptableObject
    {
        [Header("Fallback Defaults")]
        public int baseStrength = 10;
        public int baseVitality = 10;
        public int baseEndurance = 10;

        [Header("Legacy Linear Growth")]
        public int strengthPerLevel = 0;
        public int vitalityPerLevel = 0;
        public int endurancePerLevel = 0;

        [Header("Per-Level Table")]
        public List<LevelGrowthEntry> perLevel = new List<LevelGrowthEntry>();

        public void Evaluate(int level, out int strength, out int vitality, out int endurance)
        {
            int safeLevel = Mathf.Max(1, level);

            if (TryGetFromTable(safeLevel, out strength, out vitality, out endurance))
                return;

            int levelDelta = Mathf.Max(0, safeLevel - 1);
            strength = baseStrength + (strengthPerLevel * levelDelta);
            vitality = baseVitality + (vitalityPerLevel * levelDelta);
            endurance = baseEndurance + (endurancePerLevel * levelDelta);
        }

        private bool TryGetFromTable(int level, out int strength, out int vitality, out int endurance)
        {
            strength = 0;
            vitality = 0;
            endurance = 0;

            if (perLevel == null || perLevel.Count == 0)
                return false;

            LevelGrowthEntry? exact = null;
            LevelGrowthEntry? nearest = null;

            for (int i = 0; i < perLevel.Count; i++)
            {
                LevelGrowthEntry entry = perLevel[i];
                if (entry.level <= 0)
                    continue;

                if (entry.level == level)
                {
                    exact = entry;
                    break;
                }

                if (entry.level <= level)
                {
                    if (!nearest.HasValue || entry.level > nearest.Value.level)
                        nearest = entry;
                }
                else if (!nearest.HasValue)
                {
                    nearest = entry;
                }
            }

            LevelGrowthEntry resolved = exact ?? nearest ?? new LevelGrowthEntry
            {
                level = 1,
                strength = baseStrength,
                vitality = baseVitality,
                endurance = baseEndurance,
            };

            strength = resolved.strength;
            vitality = resolved.vitality;
            if (resolved.endurance > 0)
            {
                endurance = resolved.endurance;
            }
            else
            {
                int levelDelta = Mathf.Max(0, level - 1);
                endurance = baseEndurance + (endurancePerLevel * levelDelta);
            }
            return true;
        }
    }
}
