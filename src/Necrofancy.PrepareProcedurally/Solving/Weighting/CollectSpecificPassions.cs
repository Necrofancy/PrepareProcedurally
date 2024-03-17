using System.Collections.Generic;
using Necrofancy.PrepareProcedurally.Solving.Backgrounds;
using RimWorld;
using Verse;

namespace Necrofancy.PrepareProcedurally.Solving.Weighting;

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
            
        var sum = 0.0f;

        var alreadyConsideredSkills = new List<SkillDef>(3);
        foreach (var trait in possibility.Traits)
        {
            if (trait.def.forcedPassions != null)
            {
                foreach (var mustBePassion in trait.def.forcedPassions)
                {
                    if (DesiredSkills.TryGetValue(mustBePassion, out var weight))
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
                    if (DesiredSkills.TryGetValue(cannotBePassion, out var weight))
                    {
                        // terrible!
                        sum -= weight * 10;
                        alreadyConsideredSkills.Add(cannotBePassion);
                    }
                }
            }
        }

        foreach ((var skill, var weight) in DesiredSkills)
        {
            if (alreadyConsideredSkills.Contains(skill))
            {
                continue;
            }

            foreach (var gain in possibility.Childhood.skillGains)
            {
                if (gain.skill == skill)
                    sum += weight * gain.amount;
            }
            
            foreach (var gain in possibility.Adulthood.skillGains)
            {
                if (gain.skill == skill)
                    sum += weight * gain.amount;
            }

            foreach (var trait in possibility.Traits)
            {
                var traitData = trait.def.DataAtDegree(trait.degree ?? 0);
                if (traitData.skillGains is {} traitGains)
                {
                    foreach (var gain in traitGains)
                    {
                        if (gain.skill == skill)
                            sum += weight * gain.amount;
                    }

                }
            }
        }

        return sum;
    }
}