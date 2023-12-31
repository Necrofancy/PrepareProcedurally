using System.Collections.Generic;
using Necrofancy.PrepareProcedurally.Solving.Backgrounds;
using RimWorld;
using Verse;

namespace Necrofancy.PrepareProcedurally.Solving.Skills;

public class PawnBuilder : SkillRangeBuilder<SkillDef>
{
    public PawnBuilder(Dictionary<SkillDef, IntRange> skillRanges, HashSet<SkillDef> forcedPassions,
        HashSet<SkillDef> disallowedPassions, float maxPassionPoints) : base(skillRanges, forcedPassions,
        disallowedPassions, maxPassionPoints)
    {
    }

    public static PawnBuilder FromPossibleBio(BioPossibility bio, float age)
    {
        var skills = GetSkillRanges(bio, age);
        var disabledPassions = new HashSet<SkillDef>();
        var forcedPassions = new HashSet<SkillDef>();
        disabledPassions.AddRange(SkillDisables.GetSkillsDisabled(bio.Childhood, bio.Adulthood));
        foreach (var trait in bio.Traits)
        {
            if (trait.def.forcedPassions is { } forced)
                forcedPassions.AddRange(forced);
            if (trait.def.conflictingPassions is { } conflicting)
                disabledPassions.AddRange(conflicting);
        }

        return new PawnBuilder(skills, forcedPassions, disabledPassions, Editor.MaxPassionPoints);
    }

    public static PawnBuilder ForPawn(Pawn pawn)
    {
        var skills = GetSkillRanges(pawn);
        var disabledPassions = new HashSet<SkillDef>();
        var forcedPassions = new HashSet<SkillDef>();

        BackstoryDef childhood = pawn.story.Childhood, adulthood = pawn.story.Adulthood;
        disabledPassions.AddRange(SkillDisables.GetSkillsDisabled(childhood, adulthood));
        foreach (var trait in pawn.story.traits.allTraits)
        {
            if (trait.def.forcedPassions is { } forced)
                forcedPassions.AddRange(forced);
            if (trait.def.conflictingPassions is { } conflicting)
                disabledPassions.AddRange(conflicting);
        }

        return new PawnBuilder(skills, forcedPassions, disabledPassions, Editor.MaxPassionPoints);
    }

    protected override IReadOnlyList<SkillDef> GetAllSkillDefinitions()
    {
        return DefDatabase<SkillDef>.AllDefsListForReading;
    }


    private static Dictionary<SkillDef, IntRange> GetSkillRanges(BioPossibility bio, float age)
    {
        var skills = new Dictionary<SkillDef, IntRange>();
        foreach (var skill in DefDatabase<SkillDef>.AllDefs)
        {
            var min = EstimateRolling.StaticRoll(in bio, age, skill, 0f);
            var max = EstimateRolling.StaticRoll(in bio, age, skill, .98f);
            skills[skill] = new IntRange(min, max);
        }

        return skills;
    }

    private static Dictionary<SkillDef, IntRange> GetSkillRanges(Pawn pawn)
    {
        var skills = new Dictionary<SkillDef, IntRange>();
        foreach (var skill in DefDatabase<SkillDef>.AllDefs)
        {
            var min = EstimateRolling.StaticRoll(pawn, skill, 0f);
            var max = EstimateRolling.StaticRoll(pawn, skill, .98f);
            skills[skill] = new IntRange(min, max);
        }

        return skills;
    }
}