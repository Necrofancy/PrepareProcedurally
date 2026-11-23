using Necrofancy.PrepareProcedurally.Editing;
using Necrofancy.PrepareProcedurally.Interface.Dialogs;
using Necrofancy.PrepareProcedurally.Solving.Backgrounds;

namespace Necrofancy.PrepareProcedurally.Solving.Weighting;

/// <summary>
/// Try to collect a specific skills, only caring about affects to influence passions.
/// </summary>
public class CollectSpecificPassions
{
    public CollectSpecificPassions(PawnSlot slot)
    {
        var desiredSkills = new Dictionary<SkillDef, int>();
        var requiredWork = WorkTags.None;
        var requiredSkills = new List<SkillDef>();

        foreach (var (skill, usability) in slot.CustomRequirements)
            switch (usability)
            {
                case UsabilityRequirement.Major:
                    desiredSkills[skill] = 20 ;
                    requiredWork |= skill.disablingWorkTags;
                    requiredSkills.Add(skill);
                    break;
                case UsabilityRequirement.Minor:
                    desiredSkills[skill] = 10;
                    requiredWork |= skill.disablingWorkTags;
                    requiredSkills.Add(skill);
                    break;
                case UsabilityRequirement.Usable:
                    desiredSkills[skill] = 5;
                    requiredWork |= skill.disablingWorkTags;
                    requiredSkills.Add(skill);
                    break;
                default:
                    desiredSkills[skill] = -5;
                    break;
            }

        foreach (var workType in DefDatabase<WorkTypeDef>.AllDefsListForReading)
            if (workType.relevantSkills.Any(requiredSkills.Contains))
                requiredWork |= workType.workTags;

        DesiredSkills = desiredSkills;
        RequiredWork = requiredWork;
    }

    private Dictionary<SkillDef, int> DesiredSkills { get; }
    private WorkTags RequiredWork { get; }

    public float Weight(BackstoryPossibility possibility)
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

#if RW1_4
            void AddToSum(KeyValuePair<SkillDef, int> gain)
            {
                if (gain.Key == skill)
                    sum += weight * gain.Value;
            }
#else
            void AddToSum(SkillGain gain)
            {
                if (gain.skill == skill)
                    sum += weight * gain.amount;
            }
#endif

            foreach (var gain in possibility.Childhood.skillGains)
                AddToSum(gain);

            foreach (var gain in possibility.Adulthood.skillGains)
                AddToSum(gain);

            foreach (var trait in possibility.Traits)
            {
                var traitData = trait.def.DataAtDegree(trait.degree ?? 0);
                if (traitData.skillGains is { } traitGains)
                {
                    foreach (var gain in traitGains)
                        AddToSum(gain);
                }
            }
        }

        return sum;
    }
}
