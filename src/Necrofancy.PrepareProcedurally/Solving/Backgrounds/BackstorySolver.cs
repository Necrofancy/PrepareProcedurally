#nullable enable
using Necrofancy.PrepareProcedurally.Interface.Dialogs;
using Necrofancy.PrepareProcedurally.Solving.Skills;
using Necrofancy.PrepareProcedurally.Solving.Weighting;

namespace Necrofancy.PrepareProcedurally.Solving.Backgrounds;

public static class BackstorySolver
{
    public static IReadOnlyList<BackgroundPossibility> TryToSolveWith(TeamRebalance rebalance)
    {
        var categoryFilters = rebalance.RuleSet.RaceRules.GetBackstoryCategoryFilters();
        var currentBackgrounds = new List<BackgroundPossibility>(rebalance.Editor.Slots.Count);
        var finalBackgrounds = new BackgroundPossibility[rebalance.Editor.Slots.Count];
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
            var specifier = new SelectBackstorySpecifically(categoryFilters, slot.Gender, currentBackgrounds);
            if (slot.ForcedChildhood is not null)
                specifier.ForceChildhood(slot.ForcedChildhood);

            if (slot.ForcedAdulthood is not null)
                specifier.ForceAdulthood(slot.ForcedAdulthood);

            var bio = specifier.GetBestBio(skillWeightingSystem.Weight, slot.RequiredTraits);
            var skillRanges = EstimateRolling.PossibleSkillRangesOf(bio);
            var determinedBackground = new BackgroundPossibility(bio, skillRanges, true);
            currentBackgrounds.Add(determinedBackground);
            finalBackgrounds[slot.Index] = determinedBackground;
        }

        return finalBackgrounds;
    }

    public static IReadOnlyList<BackgroundPossibility> TryToSolveWith(
        BalancingSituation situation,
        SimpleCurve ageRange,
        IntRange variation)
    {
        var currentBackgrounds = new List<BackgroundPossibility>(situation.Pawns);

        var weights = new Dictionary<SkillPassionSelection, int>();
        for (var i = currentBackgrounds.Count; i < situation.Pawns; i++)
        {
            // Evil static state here
            var existingPawn = Editor.StartingPawns[i];
            if (Editor.LockedPawns.Contains(existingPawn))
            {
                currentBackgrounds.Add(StaticBackgroundPossibility(existingPawn));
                continue;
            }

            foreach (var requirement in situation.SkillRequirements)
            {
                var moddedByVariation = requirement.GetWeights(currentBackgrounds) * variation.RandomInRange;
                weights[requirement] = moddedByVariation;
            }

            var skillWeightingSystem = new SpecificSkillWeights(weights);
            var specifier = new SelectBackstorySpecifically(situation.CategoryFilters, Editor.GenderRequirements[i],
                currentBackgrounds);
            if (Editor.SetChildhoods[i] is not null)
            {
                specifier.ForceChildhood(Editor.SetChildhoods[i]);
            }

            if (Editor.SetAdulthoods[i] is not null)
            {
                specifier.ForceAdulthood(Editor.SetAdulthoods[i]);
            }

            var bio = specifier.GetBestBio(skillWeightingSystem.Weight, Editor.TraitRequirements[i]);
            var skillRanges = EstimateRolling.PossibleSkillRangesOf(bio);
            currentBackgrounds.Add(new BackgroundPossibility(bio, skillRanges, true));
        }

        return currentBackgrounds;
    }

    public static BackgroundPossibility StaticBackgroundPossibility(Pawn pawn)
    {
        var bio = new BioPossibility(pawn.story.Childhood, pawn.story.Adulthood);
        var dict = new Dictionary<SkillDef, IntRange>();
        foreach (var skill in pawn.skills.skills) dict[skill.def] = new IntRange(skill.levelInt, skill.levelInt);

        return new BackgroundPossibility(bio, dict, false);
    }

    public static SkillFinalizationResult FigureOutPassions(PawnCustomize customization, BioPossibility background)
    {
        var builder = PawnBuilder.FromPossibleBio(background);
        foreach (var (skill, requirement) in customization.Requirements)
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
        IReadOnlyList<BackgroundPossibility> bios,
        IReadOnlyList<SkillPassionSelection> teamRequirements)
    {
        // two lists; one that can be re-ordered at will to try locking in skills easier
        // and one that retains order to return results later.
        var pawnsInOriginalOrder = new PawnBuilder?[bios.Count];
        var list = new List<PawnBuilder?>(bios.Count);
        for (var i = 0; i < bios.Count; i++)
        {
            var possibility = bios[i];
            var builder = possibility.CanChange
                ? PawnBuilder.FromPossibleBio(possibility.Background)
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
