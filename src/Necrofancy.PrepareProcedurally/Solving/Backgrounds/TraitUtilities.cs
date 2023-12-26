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

        /// <summary>
        /// If too many traits are requested, then pawn generation will randomly include a fourth or fifth trait.
        /// "Requested" traits are not similar to _forced_ traits - the goal is to simulate rolling them specifically.
        ///
        /// To accomplish that, remove any randomly-rolled traits until the pawn is back to their max number of traits.
        /// </summary>
        internal static void FixTraitOverflow(Pawn pawn)
        {
            var neededTraits = RequiredTraitsForLockedPawn(pawn);
            var traitSlots = 0;
            var candidates = new List<Trait>();
            foreach (var trait in pawn.story.traits.allTraits)
            {
                // Biotech-forced traits won't count towards the limit.
                // Post-1.4, sexuality traits are rolled separately from the 1~3 trait count
                // Scenario-forced traits are weird, and having enough of them will absolutely put you over 3 traits.
                
                // So best to ignore all of these when seeing what traits 'need' to be cut for normal pawn generation.
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