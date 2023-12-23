#nullable enable
using System.Collections.Generic;
using System.Linq;
using Necrofancy.PrepareProcedurally.Solving.Skills;
using Necrofancy.PrepareProcedurally.Solving.Weighting;
using RimWorld;
using Verse;

namespace Necrofancy.PrepareProcedurally.Solving.Backgrounds
{
    public static class BackstorySolver
    {
        public static IReadOnlyList<BackgroundPossibility> TryToSolveWith(BalancingSituation situation, IntRange variation, IEnumerable<Pawn>? keepPawns = null)
        {
            const int age = 35;
            var specifier = new SelectBackstorySpecifically(situation.CategoryName);
            var currentBackgrounds = new List<BackgroundPossibility>(situation.Pawns);
            
            var weights = new Dictionary<SkillPassionSelection, int>();
            for (int i = currentBackgrounds.Count; i < situation.Pawns; i++)
            {
                // Evil static state here
                var existingPawn = ProcGen.StartingPawns[i];
                if (ProcGen.LockedPawns.Contains(existingPawn))
                {
                    currentBackgrounds.Add(StaticBackgroundPossibility(existingPawn));
                    continue;
                }
                
                foreach (var requirement in situation.SkillRequirements)
                {
                    int moddedByVariation = requirement.GetWeights(currentBackgrounds) * variation.RandomInRange;
                    weights[requirement] = moddedByVariation;
                }

                var skillWeightingSystem = new SpecificSkillWeights(weights);
                var bio = specifier.GetBestBio(skillWeightingSystem.Weight, ProcGen.TraitRequirements[i]);
                var skillRanges = EstimateHighRolling.PossibleSkillRangesOf(age, bio);
                currentBackgrounds.Add(new BackgroundPossibility(bio, skillRanges, true));
            }

            return currentBackgrounds;
        }

        private static BackgroundPossibility StaticBackgroundPossibility(Pawn pawn)
        {
            var bio = new BioPossibility(pawn.story.Childhood, pawn.story.Adulthood);
            var dict = new Dictionary<SkillDef, IntRange>();
            foreach (var skill in pawn.skills.skills)
            {
                dict[skill.def] = new IntRange(skill.levelInt, skill.levelInt);
            }

            return new BackgroundPossibility(bio, dict, false);
        }

        public static IReadOnlyList<SkillFinalizationResult?> FigureOutPassions(
            IReadOnlyList<BackgroundPossibility> bios,
            BalancingSituation situation)
        {
            // two lists; one that can be re-ordered at will to try locking in skills easier
            // and one that retains order to return results later.
            var pawnsInOriginalOrder = new PawnBuilder?[bios.Count];
            var list = new List<PawnBuilder?>(bios.Count);
            for (int i = 0; i < bios.Count; i++)
            {
                var possibility = bios[i];
                var builder = possibility.CanChange
                    ? new PawnBuilder(possibility.Background)
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
            {
                results.Add(builder?.Build() ?? null);
            }

            return results;
        }

        private static void LockInAsNeeded(SkillPassionSelection req, IReadOnlyList<PawnBuilder?> pawns)
        {
            int goal = req.Total;
            for (int i = 0; i < pawns.Count; i++)
            {
                if (pawns[i]?.LockIn(req, goal) == true)
                {
                    goal--;
                    if (goal == 0)
                        break;
                }
            }
        }

        private readonly struct SkillMaxDescending : IComparer<PawnBuilder>
        {
            private readonly SkillDef _def;

            public SkillMaxDescending(SkillDef def)
            {
                _def = def;
            }

            public int Compare(PawnBuilder x, PawnBuilder y)
            {
                if (ReferenceEquals(x, y)) return 0;
                if (ReferenceEquals(null, y)) return -1;
                if (ReferenceEquals(null, x)) return 1;

                int byPassion = x.PassionPoints.CompareTo(y.PassionPoints);
                int bySkillMax = y.MaxOf(_def).CompareTo(x.MaxOf(_def));

                return _def.usuallyDefinedInBackstories
                    ? bySkillMax != 0 ? bySkillMax : byPassion
                    : byPassion != 0
                        ? byPassion
                        : bySkillMax;
            }
        }
    }
}