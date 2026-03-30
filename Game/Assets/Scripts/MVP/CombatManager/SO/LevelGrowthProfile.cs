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
    }

    [CreateAssetMenu(fileName = "LevelGrowthProfile", menuName = "Combat/Level Growth Profile")]
    public class LevelGrowthProfile : ScriptableObject
    {
        [Header("Fallback Defaults")]
        public int baseStrength = 10;
        public int baseVitality = 10;

        [Header("Legacy Linear Growth")]
        public int strengthPerLevel = 0;
        public int vitalityPerLevel = 0;

        [Header("Per-Level Table")]
        public List<LevelGrowthEntry> perLevel = new List<LevelGrowthEntry>();

        public void Evaluate(int level, out int strength, out int vitality)
        {
            int safeLevel = Mathf.Max(1, level);

            if (TryGetFromTable(safeLevel, out strength, out vitality))
                return;

            int levelDelta = Mathf.Max(0, safeLevel - 1);
            strength = baseStrength + (strengthPerLevel * levelDelta);
            vitality = baseVitality + (vitalityPerLevel * levelDelta);
        }

        private bool TryGetFromTable(int level, out int strength, out int vitality)
        {
            strength = 0;
            vitality = 0;

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
            };

            strength = resolved.strength;
            vitality = resolved.vitality;
            return true;
        }
    }
}
