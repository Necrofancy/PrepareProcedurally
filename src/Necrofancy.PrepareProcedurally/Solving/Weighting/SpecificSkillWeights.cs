using System;
using System.Collections.Generic;
using System.Linq;
using Necrofancy.PrepareProcedurally.Solving.Backgrounds;
using Verse;

namespace Necrofancy.PrepareProcedurally.Solving.Weighting
{
    /// <summary>
    /// To try to balance passions and skill levels specifically, try fulfilling specific weights
    /// of Major passions, Minor Passions, and usable skills all at once.
    /// </summary>
    public class SpecificSkillWeights
    {
        public SpecificSkillWeights(IReadOnlyDictionary<SkillPassionSelection, int> requirements)
        {
            Requirements = requirements;
        }

        public IReadOnlyDictionary<SkillPassionSelection, int> Requirements { get; }

        public float Weight(BioPossibility possibility)
        {
            var disables = possibility.Childhood.workDisables | possibility.Adulthood.workDisables;
            var workDisables = possibility.Childhood.DisabledWorkTypes.Concat(possibility.Adulthood.DisabledWorkTypes).Distinct().ToList();
            float weightProgressCount = 0;
            foreach ((var requirement, int weight) in Requirements)
            {
                var skill = requirement.Skill;
                int added = 0;
                if (possibility.Childhood.skillGains.TryGetValue(skill, out int change))
                    added += change;
                if (possibility.Adulthood.skillGains.TryGetValue(skill, out change))
                    added += change;

                if (skill.IsDisabled(disables, workDisables))
                {
                    weightProgressCount -= weight * 4;
                    continue;
                }
                if (added <= 0)
                {
                    continue;
                }

                float weightMultiplier = Math.Min(added, 1.0f) * weight; 
                
                weightProgressCount += weightMultiplier;
            }

            return weightProgressCount;
        }
    }
}