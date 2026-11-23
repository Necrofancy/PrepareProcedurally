using Necrofancy.PrepareProcedurally.Solving.Backgrounds;

namespace Necrofancy.PrepareProcedurally.Solving.Skills;

public class PawnBuilder : SkillRangeBuilder<SkillDef>
{
    public PawnBuilder(
        Dictionary<SkillDef, IntRange> skillRanges,
        HashSet<SkillDef> forcedPassions,
        HashSet<SkillDef> disallowedPassions,
        float maxPassionPoints) : base(skillRanges, forcedPassions,
        disallowedPassions, maxPassionPoints)
    {
    }

    public static PawnBuilder FromPossibleBio(BackstoryPossibility backstory, float maxPassionPoints)
    {
        var skills = GetSkillRanges(backstory);
        var disabledPassions = new HashSet<SkillDef>();
        var forcedPassions = new HashSet<SkillDef>();
        disabledPassions.AddRange(SkillDisables.GetSkillsDisabled(backstory.Childhood, backstory.Adulthood));
        foreach (var trait in backstory.Traits)
        {
            if (trait.def.forcedPassions is { } forced)
                forcedPassions.AddRange(forced);
            if (trait.def.conflictingPassions is { } conflicting)
                disabledPassions.AddRange(conflicting);
        }

        return new PawnBuilder(skills, forcedPassions, disabledPassions, maxPassionPoints);
    }

    public static PawnBuilder ForPawn(Pawn pawn, float maxPassionPoints)
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

        return new PawnBuilder(skills, forcedPassions, disabledPassions, maxPassionPoints);
    }

    protected override IReadOnlyList<SkillDef> GetAllSkillDefinitions()
    {
        return DefDatabase<SkillDef>.AllDefsListForReading;
    }


    private static Dictionary<SkillDef, IntRange> GetSkillRanges(BackstoryPossibility backstory)
    {
        var skills = new Dictionary<SkillDef, IntRange>();
        foreach (var skill in DefDatabase<SkillDef>.AllDefs)
        {
            var min = EstimateRolling.StaticAgelessRoll(in backstory, skill, 0f);
            var max = EstimateRolling.StaticAgelessRoll(in backstory, skill, .98f);
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
