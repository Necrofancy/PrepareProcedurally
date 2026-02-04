using System.Collections.Generic;
using Necrofancy.PrepareProcedurally.Interface.Dialogs;
using Necrofancy.PrepareProcedurally.Solving.Weighting;
using RimWorld;
using Verse;

namespace Necrofancy.PrepareProcedurally;

public static class SelectedPawn
{
    public static Pawn Pawn { get; private set; }
    
    public static int Index { get; private set; }
    
    public static List<(SkillDef Skill, UsabilityRequirement Usability)> Requirements { get; private set; }
    
    public static void Select(Pawn pawn)
    {
        Editor.LockedPawns.Add(pawn);
        Pawn = pawn;
        Index = StartingPawnUtility.PawnIndex(pawn);
        Requirements = new List<(SkillDef Skill, UsabilityRequirement Usability)>();
        if (Editor.LastResults?.Count > Index - 1
            && Editor.LastResults[Index] is { } existing)
            foreach (var skill in DefDatabase<SkillDef>.AllDefsListForReading)
            {
                var result = existing.FinalRanges[skill];
                var usability = result.Passion switch
                {
                    Passion.Major => UsabilityRequirement.Major,
                    Passion.Minor => UsabilityRequirement.Minor,
                    _ => UsabilityRequirement.CanBeOff
                };
                Requirements.Add((skill, usability));
            }
        else
            foreach (var skill in pawn.skills.skills)
            {
                var req = skill.PermanentlyDisabled
                    ? UsabilityRequirement.CanBeOff
                    : skill.passion == Passion.Major
                        ? UsabilityRequirement.Major
                        : skill.passion == Passion.Minor
                            ? UsabilityRequirement.Minor
                            : UsabilityRequirement.Usable;
                Requirements.Add((skill.def, req));
            }
    }
    public static void Deselect()
    {
        Pawn = null;
        Index = -1;
        Requirements = null;
    }
    public static float GetPassionPoints()
    {
        var sum = 0.0f;
        foreach (var item in Requirements)
            switch (item.Usability)
            {
                case UsabilityRequirement.Major:
                    sum += 1.5f;
                    break;
                case UsabilityRequirement.Minor:
                    sum += 1.0f;
                    break;
            }

        return sum;
    }
    
    public static void Randomize()
    {
        var dict = new Dictionary<SkillDef, int>();
        var requiredSkills = new List<SkillDef>();
        var requiredWorkTags = WorkTags.None;

        var variation = new IntRange(10, (int)(Editor.SkillWeightVariation * 10));
        foreach (var (skill, usability) in Requirements)
            switch (usability)
            {
                case UsabilityRequirement.Major:
                    dict[skill] = 20 * variation.RandomInRange;
                    requiredWorkTags |= skill.disablingWorkTags;
                    requiredSkills.Add(skill);
                    break;
                case UsabilityRequirement.Minor:
                    dict[skill] = 10 * variation.RandomInRange;
                    requiredWorkTags |= skill.disablingWorkTags;
                    requiredSkills.Add(skill);
                    break;
                case UsabilityRequirement.Usable:
                    dict[skill] = 5 * variation.RandomInRange;
                    requiredWorkTags |= skill.disablingWorkTags;
                    requiredSkills.Add(skill);
                    break;
                default:
                    dict[skill] = -5 * variation.RandomInRange;
                    break;
            }

        foreach (var workType in DefDatabase<WorkTypeDef>.AllDefsListForReading)
            if (requiredSkills.Any(requiredSkills.Contains))
                requiredWorkTags |= workType.workTags;

        var collectSpecificPassions = new CollectSpecificPassions(dict, requiredWorkTags);

        Pawn = Compatibility.Layer.RandomizeSingularPawn(Pawn, collectSpecificPassions, Requirements);
    }
}