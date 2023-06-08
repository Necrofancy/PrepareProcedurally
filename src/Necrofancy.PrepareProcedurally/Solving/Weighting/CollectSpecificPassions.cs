using System;
using System.Collections.Generic;
using Necrofancy.PrepareProcedurally.Solving.Backgrounds;
using RimWorld;
using UnityEngine;
using Verse;

namespace Necrofancy.PrepareProcedurally.Solving.Weighting
{
    /// <summary>
    /// Try to collect a specific skills, only caring about affects to influence passions.
    /// </summary>
    public class CollectSpecificPassions
    {
        public CollectSpecificPassions(Dictionary<SkillDef, int> desiredSkills, WorkTags requiredWork)
        {
            DesiredSkills = desiredSkills;
            RequiredWork = requiredWork;
        }

        private Dictionary<SkillDef, int> DesiredSkills { get; }
        private WorkTags RequiredWork { get; }

        public float Weight(BioPossibility possibility)
        {
            var disabledWork = WorkTags.None;
            disabledWork |= possibility.Childhood.workDisables;
            disabledWork |= possibility.Adulthood.workDisables;
            foreach (var possibilityTrait in possibility.Traits)
            {
                disabledWork |= possibilityTrait.def.disabledWorkTags;
            }
            
            if ((disabledWork & RequiredWork) != WorkTags.None)
            {
                return float.MinValue;
            }
            
            float sum = 0.0f;

            var alreadyConsideredSkills = new List<SkillDef>(3);
            foreach (var trait in possibility.Traits)
            {
                if (trait.def.forcedPassions != null)
                {
                    foreach (var mustBePassion in trait.def.forcedPassions)
                    {
                        if (DesiredSkills.TryGetValue(mustBePassion, out int weight))
                        {
                            sum += weight;
                            alreadyConsideredSkills.Add(mustBePassion);
                        }
                    }
                }
                
                // Assuming there's no crazy trait that both forces a passion and conflicts.
                if (trait.def.conflictingPassions != null)
                {
                    foreach (var cannotBePassion in trait.def.conflictingPassions)
                    {
                        if (DesiredSkills.TryGetValue(cannotBePassion, out int weight))
                        {
                            // terrible!
                            sum -= weight * 10;
                            alreadyConsideredSkills.Add(cannotBePassion);
                        }
                    }
                }
            }

            foreach ((var skill, int weight) in DesiredSkills)
            {
                if (alreadyConsideredSkills.Contains(skill))
                {
                    continue;
                }

                if (possibility.Childhood.skillGains.TryGetValue(skill, out int bonus))
                {
                    sum += weight * bonus;
                }
                if (possibility.Adulthood.skillGains.TryGetValue(skill, out bonus))
                {
                    sum += weight * bonus;
                }

                foreach (var trait in possibility.Traits)
                {
                    var traitData = trait.def.DataAtDegree(trait.degree ?? 0);
                    if (traitData.skillGains?.TryGetValue(skill, out bonus) == true)
                    {
                        sum += weight * bonus;
                    }
                }
            }

            return sum;
        }
    }
}