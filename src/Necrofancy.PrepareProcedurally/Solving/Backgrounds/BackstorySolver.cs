#nullable enable
using System.Collections.Generic;
using System.Linq;
using Necrofancy.PrepareProcedurally.Solving.Skills;
using Necrofancy.PrepareProcedurally.Solving.Weighting;
using RimWorld;
using Verse;

namespace Necrofancy.PrepareProcedurally.Solving.Backgrounds;

public static class BackstorySolver
{
    public static IReadOnlyList<BackgroundPossibility> TryToSolveWith(BalancingSituation situation,
        SimpleCurve ageRange, IntRange variation)
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

            var age = Rand.ByCurve(ageRange);

            var skillWeightingSystem = new SpecificSkillWeights(weights);
            var specifier = new SelectBackstorySpecifically(situation.CategoryName, Editor.GenderRequirements[i], currentBackgrounds);
            var bio = specifier.GetBestBio(skillWeightingSystem.Weight, Editor.TraitRequirements[i]);
            var skillRanges = EstimateRolling.PossibleSkillRangesOf(age, bio);
            currentBackgrounds.Add(new BackgroundPossibility(bio, skillRanges, age, true));
        }

        return currentBackgrounds;
    }

    internal static BackgroundPossibility StaticBackgroundPossibility(Pawn pawn)
    {
        var bio = new BioPossibility(pawn.story.Childhood, pawn.story.Adulthood);
        var dict = new Dictionary<SkillDef, IntRange>();
        foreach (var skill in pawn.skills.skills) dict[skill.def] = new IntRange(skill.levelInt, skill.levelInt);

        return new BackgroundPossibility(bio, dict, pawn.ageTracker.AgeBiologicalYearsFloat, false);
    }

    public static IReadOnlyList<SkillFinalizationResult?> FigureOutPassions(
        IReadOnlyList<BackgroundPossibility> bios,
        BalancingSituation situation)
    {
        // two lists; one that can be re-ordered at will to try locking in skills easier
        // and one that retains order to return results later.
        var pawnsInOriginalOrder = new PawnBuilder?[bios.Count];
        var list = new List<PawnBuilder?>(bios.Count);
        for (var i = 0; i < bios.Count; i++)
        {
            var possibility = bios[i];
            var builder = possibility.CanChange
                ? PawnBuilder.FromPossibleBio(possibility.Background, possibility.AssumedAge)
                : null;

            pawnsInOriginalOrder[i] = builder;
            list.Add(builder);
        }

        // run non-backstory-related skills last 
        var skillRequirements = situation.SkillRequirements
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

        public int Compare(PawnBuilder x, PawnBuilder y)
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