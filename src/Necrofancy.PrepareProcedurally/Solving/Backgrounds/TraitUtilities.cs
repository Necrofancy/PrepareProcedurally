using System;
using System.Collections.Generic;
using System.Linq;
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

            if (HumanoidAlienRaceCompatibility.IsHumanoidAlienRacePawn(pawn))
            {
                requiredTraits.AddRange(HumanoidAlienRaceCompatibility.GetTraitRequirements(pawn));
            }

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
            
            if (HumanoidAlienRaceCompatibility.IsHumanoidAlienRacePawn(pawn))
            {
                requiredTraits.AddRange(HumanoidAlienRaceCompatibility.GetTraitRequirements(pawn));
            }
            
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

        internal static void AddForcedTraits(Pawn pawn, List<TraitRequirement> traits)
        {
            traits = HumanoidAlienRaceCompatibility.IsHumanoidAlienRacePawn(pawn)
                ? traits.Concat(HumanoidAlienRaceCompatibility.GetTraitRequirements(pawn)).ToList()
                : traits;
            
            foreach (var trait in traits)
            {
                if (pawn.story?.traits == null 
                    || pawn.story.traits.HasTrait(trait.def) 
                    && pawn.story.traits.DegreeOfTrait(trait.def) == trait.degree)
                {
                    return;
                }
                if (pawn.story.traits.HasTrait(trait.def))
                {
                    pawn.story.traits.allTraits.RemoveAll(tr => tr.def == trait.def);
                }
                else
                {
                    var source = pawn.story.traits.allTraits.Where(tr => !tr.ScenForced && !IsBackstoryTraitOfPawn(tr, pawn));
                    var conflictingTrait = source.FirstOrDefault(tr => trait.def.ConflictsWith(tr.def));
                    pawn.story.traits.allTraits.Remove(conflictingTrait);
                }
                
                pawn.story.traits.GainTrait(new Trait(trait.def, trait.degree ?? 0));
            }
            
            FixTraitOverflow(pawn);
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
                if (trait.sourceGene != null || trait.def.IsSexualityTrait() || trait.ScenForced)
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

            while (traitSlots > MaxAllowedTraits(pawn))
            {
                var traitToRemove = candidates.RandomElement();
                pawn.story.traits.RemoveTrait(traitToRemove);
                candidates.Remove(traitToRemove);
                traitSlots--;
            }
        }

        internal static int MaxAllowedTraits(Pawn pawn)
        {
            if (HumanoidAlienRaceCompatibility.IsHumanoidAlienRacePawn(pawn))
            {
                return MaxNonSexualityTraits + HumanoidAlienRaceCompatibility.GetMaxTraits(pawn);
            }

            return MaxNonSexualityTraits;
        }
        
        internal static bool IsBackstoryTraitOfPawn(Trait trait, Pawn pawn)
        {
            bool isChildHoodBackstory = pawn.story.Adulthood?.forcedTraits?.Any(x => BackstoryMatch(trait, x)) == true;
            bool isAdulthoodBackstory = pawn.story.Childhood?.forcedTraits?.Any(x => BackstoryMatch(trait, x)) == true;
            if (HumanoidAlienRaceCompatibility.IsHumanoidAlienRacePawn(pawn))
            {
                var requiredRaceTraits = HumanoidAlienRaceCompatibility.GetTraitRequirements(pawn);
                bool isRequiredRaceTrait = requiredRaceTraits.Any(x => x.def == trait.def);
                return isRequiredRaceTrait || isChildHoodBackstory || isAdulthoodBackstory;
            }
            else
            {
                return isChildHoodBackstory || isAdulthoodBackstory;
            }
        }

        private static bool BackstoryMatch(Trait traitInstance, BackstoryTrait backstoryTrait)
        {
            return backstoryTrait.def == traitInstance.def && backstoryTrait.degree == traitInstance.Degree;
        }
    }
}