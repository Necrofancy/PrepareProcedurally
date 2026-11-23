#nullable enable
using Necrofancy.PrepareProcedurally.Interface.Dialogs;
using Necrofancy.PrepareProcedurally.Solving.Skills;
using Necrofancy.PrepareProcedurally.Solving.Weighting;

namespace Necrofancy.PrepareProcedurally.Solving.Backgrounds;

public static class BackstorySolver
{
    public static IReadOnlyList<PawnPossibility> TryToSolveWith(TeamRebalance rebalance)
    {
        var currentBackgrounds = new List<PawnPossibility>(rebalance.Editor.Slots.Count);
        var finalBackgrounds = new PawnPossibility[rebalance.Editor.Slots.Count];
        foreach (var slot in rebalance.GetLockedSlots())
        {
            var existingBackground = StaticBackgroundPossibility(slot.Pawn);
            currentBackgrounds.Add(existingBackground);
            finalBackgrounds[slot.Index] = existingBackground;
        }

        var weights = new Dictionary<SkillPassionSelection, int>();
        foreach (var slot in rebalance.GetUnlockedSlots())
        {
            foreach (var requirement in rebalance.Editor.AllPassions)
                weights[requirement] = requirement.GetWeights(currentBackgrounds);

            var skillWeightingSystem = new SpecificSkillWeights(weights);
            var parameters = BackstorySelectionParameters.ForTeamRebalance(rebalance, slot, skillWeightingSystem.Weight, currentBackgrounds);
            var backstoryPossibility = BackstoryPicking.TopWeighted(parameters);
            var skillRanges = EstimateRolling.PossibleSkillRangesOf(backstoryPossibility);
            var determinedBackground = new PawnPossibility(backstoryPossibility, skillRanges, true);
            currentBackgrounds.Add(determinedBackground);
            finalBackgrounds[slot.Index] = determinedBackground;
        }

        return finalBackgrounds;
    }

    public static PawnPossibility StaticBackgroundPossibility(Pawn pawn)
    {
        var bio = new BackstoryPossibility(pawn.story.Childhood, pawn.story.Adulthood, pawn.def);
        var dict = new Dictionary<SkillDef, IntRange>();
        foreach (var skill in pawn.skills.skills) dict[skill.def] = new IntRange(skill.levelInt, skill.levelInt);

        return new PawnPossibility(bio, dict, false);
    }

    public static SkillFinalizationResult FigureOutPassions(
        PawnCustomize customization,
        BackstoryPossibility background,
        float maxPassionPoints)
    {
        var builder = PawnBuilder.FromPossibleBio(background, maxPassionPoints);
        foreach (var (skill, requirement) in customization.Slot.CustomRequirements)
        {
            var passion = requirement switch
            {
                UsabilityRequirement.Major => Passion.Major,
                UsabilityRequirement.Minor => Passion.Minor,
                _ => Passion.None
            };

            builder.TryLockInPassion(skill, passion);
        }

        var (skills, exhaustedPoints) = builder.Build();
        return new SkillFinalizationResult(skills, exhaustedPoints);
    }

    public static IReadOnlyList<SkillFinalizationResult?> FigureOutPassions(
        IReadOnlyList<PawnPossibility> bios,
        IReadOnlyList<SkillPassionSelection> teamRequirements,
        float maxPassionPoints)
    {
        // two lists; one that can be re-ordered at will to try locking in skills easier
        // and one that retains order to return results later.
        var pawnsInOriginalOrder = new PawnBuilder?[bios.Count];
        var list = new List<PawnBuilder?>(bios.Count);
        for (var i = 0; i < bios.Count; i++)
        {
            var possibility = bios[i];
            var builder = possibility.CanChange
                ? PawnBuilder.FromPossibleBio(possibility.Background, maxPassionPoints)
                : null;

            pawnsInOriginalOrder[i] = builder;
            list.Add(builder);
        }

        // run non-backstory-related skills last
        var skillRequirements = teamRequirements
            .OrderByDescending(x => x.Skill.usuallyDefinedInBackstories)
            .ThenByDescending(x => x.major)
            .ThenByDescending(x => x.minor)
            .ThenByDescending(x => x.usable);
        foreach (var req in skillRequirements)
        {
            var skillsDescending = new SkillMaxDescending(req.Skill);
            list.Sort(skillsDescending);
            LockInAsNeeded(req, list);
        }

        var results = new List<SkillFinalizationResult?>(bios.Count);
        foreach (var builder in pawnsInOriginalOrder)
            if (builder is null)
            {
                results.Add(null);
            }
            else
            {
                var (skills, exhaustedPoints) = builder.Build();
                results.Add(new SkillFinalizationResult(skills, exhaustedPoints));
            }

        return results;
    }

    internal static void LockInAsNeeded(SkillPassionSelection req, IReadOnlyList<PawnBuilder?> pawns)
    {
        var goal = req.Total;
        foreach (var pawnBuilder in pawns)
            if (pawnBuilder?.LockIn(req.Skill, req.Total, req.major, req.minor, goal) == true)
            {
                goal--;
                if (goal == 0)
                    break;
            }
    }

    internal readonly struct SkillMaxDescending : IComparer<PawnBuilder>
    {
        private readonly SkillDef def;

        public SkillMaxDescending(SkillDef def)
        {
            this.def = def;
        }

        public int Compare(PawnBuilder? x, PawnBuilder? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (ReferenceEquals(null, y)) return -1;
            if (ReferenceEquals(null, x)) return 1;

            var byPassion = x.PassionPoints.CompareTo(y.PassionPoints);
            var bySkillMax = y.MaxOf(def).CompareTo(x.MaxOf(def));

            return def.usuallyDefinedInBackstories
                ? bySkillMax != 0 ? bySkillMax : byPassion
                : byPassion != 0
                    ? byPassion
                    : bySkillMax;
        }
    }
}
