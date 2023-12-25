using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Necrofancy.PrepareProcedurally.Solving.Backgrounds
{
    internal static class TraitUtilities
    {
        private static readonly int MaxNonSexualityTraits = 3;
        
        internal static List<TraitRequirement> RequiredTraitsForUnlockedPawn(Pawn pawn)
        {
            var requiredTraits = new List<TraitRequirement>();

            var index = StartingPawnUtility.PawnIndex(pawn);
            if (index < ProcGen.TraitRequirements.Count && ProcGen.TraitRequirements[index] is { } traits)
            {
                requiredTraits.AddRange(traits);
            }

            return requiredTraits;
        }

        internal static List<TraitRequirement> RequiredTraitsForLockedPawn(Pawn pawn)
        {
            var requiredTraits = new List<TraitRequirement>();
            foreach (var trait in pawn.story.traits.allTraits)
            {
                if (IsBackstoryTraitOfPawn(trait, pawn))
                {
                    requiredTraits.Add(new TraitRequirement { def = trait.def, degree = trait.Degree });
                }
            }

            var index = StartingPawnUtility.PawnIndex(pawn);
            if (index < ProcGen.TraitRequirements.Count && ProcGen.TraitRequirements[index] is { } traits)
            {
                requiredTraits.AddRange(traits);
            }

            return requiredTraits;
        }

        internal static IEnumerable<TraitRequirement> GetAvailableTraits(List<TraitRequirement> neededTraits)
        {
            var nonSexualityCount = neededTraits.Count(x => !x.def.IsSexualityTrait());
            foreach (var possibleTrait in DefDatabase<TraitDef>.AllDefsListForReading)
            {
                if ((possibleTrait.IsSexualityTrait() || nonSexualityCount < MaxNonSexualityTraits) 
                    && neededTraits.AllowsTrait(possibleTrait))
                {
                    foreach (var data in possibleTrait.degreeDatas)
                    {
                        yield return new TraitRequirement
                        {
                            def = possibleTrait, 
                            degree = data.degree
                        };
                    }
                }
            }
        }

        /// <summary>
        /// If too many traits are forced, then pawn generation will randomly include a fourth or fifth trait.
        /// Kill those.
        /// </summary>
        internal static void FixTraitOverflow(Pawn pawn)
        {
            var neededTraits = RequiredTraitsForLockedPawn(pawn);
            var traitSlots = 0;
            var candidates = new List<Trait>();
            foreach (var trait in pawn.story.traits.allTraits)
            {
                if (trait.sourceGene != null || trait.def.IsSexualityTrait())
                {
                    continue;
                }

                traitSlots++;

                if (neededTraits.Any(x => x.def == trait.def && x.degree == trait.Degree))
                {
                    continue;
                }
                
                candidates.Add(trait);
            }

            while (traitSlots > MaxNonSexualityTraits)
            {
                var traitToRemove = candidates.RandomElement();
                pawn.story.traits.RemoveTrait(traitToRemove);
                candidates.Remove(traitToRemove);
                traitSlots--;
            }
        }
        
        internal static bool IsBackstoryTraitOfPawn(Trait trait, Pawn pawn)
        {
            return pawn.story.Adulthood?.forcedTraits?.Any(x => BackstoryMatch(trait, x)) == true
                   || pawn.story.Childhood?.forcedTraits?.Any(x => BackstoryMatch(trait, x)) == true;
        }

        private static bool BackstoryMatch(Trait traitInstance, BackstoryTrait backstoryTrait)
        {
            return backstoryTrait.def == traitInstance.def && backstoryTrait.degree == traitInstance.Degree;
        }
    }
}