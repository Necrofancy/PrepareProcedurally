using System;
using System.Collections.Generic;
using System.Linq;
using Necrofancy.PrepareProcedurally.Solving.Weighting;
using RimWorld;
using Verse;

namespace Necrofancy.PrepareProcedurally.Solving.Backgrounds
{
    public class SelectBackstorySpecifically
    {
        private readonly string _categoryName;
        
        private readonly List<TraitRequirement> _existingTraits;
        private readonly List<BackstoryDef> _childhood = new List<BackstoryDef>();
        private readonly List<BackstoryDef> _adulthood = new List<BackstoryDef>();

        private readonly HashSet<BackstoryDef> _alreadyUsed = new HashSet<BackstoryDef>();

        public SelectBackstorySpecifically(string categoryName)
        {
            _categoryName = categoryName;
        }
        
        public BioPossibility GetBestBio(WeightBackgroundAlgorithm weightingSystem, IReadOnlyList<TraitRequirement> requiredTraits)
        {
            (BioPossibility? Bio, float Ranking) bestSoFar = (default, float.MinValue);
            foreach (var poss in GetPawnPossibilities(weightingSystem, requiredTraits))
            {
                if (poss.Ranking >= bestSoFar.Ranking)
                    bestSoFar = poss;
            }

            var best = bestSoFar.Bio 
                       ?? throw new InvalidOperationException("No valid pawn possibilities were found.");

            _alreadyUsed.Add(best.Childhood);
            _alreadyUsed.Add(best.Adulthood);
            
            return best;
        }

        private IEnumerable<(BioPossibility Bio, float Ranking)> GetPawnPossibilities(WeightBackgroundAlgorithm weightingSystem, IReadOnlyList<TraitRequirement> requiredTraits)
        {
            _childhood.Clear();
            _adulthood.Clear();
            
            foreach (var backstory in DefDatabase<BackstoryDef>.AllDefsListForReading)
                if (backstory.shuffleable && backstory.spawnCategories.Contains(_categoryName))
                {
                    var list = backstory.slot == BackstorySlot.Childhood ? _childhood : _adulthood;
                    if (!_alreadyUsed.Contains(backstory))
                        list.Add(backstory);
                }

            foreach (var bio in SolidBioDatabase.allBios.Where(AllowedBio))
            {
                var possibility = new BioPossibility(bio);
                foreach (var trait in requiredTraits)
                {
                    possibility.Traits.Add(trait);
                }
                if (!_alreadyUsed.Contains(bio.childhood)
                    && !UnworkableTraitCombination(requiredTraits, bio.childhood, bio.adulthood))
                {
                    yield return (possibility, weightingSystem(possibility));
                }
            }
            
            foreach (var childhood in _childhood)
            foreach (var adulthood in _adulthood)
            {
                if (UnworkableTraitCombination(requiredTraits, childhood, adulthood)) 
                    continue;

                var requiredWork = childhood.requiredWorkTags | adulthood.requiredWorkTags;
                var disabledWork = childhood.workDisables | adulthood.workDisables;
                if ((requiredWork & disabledWork) != WorkTags.None)
                    continue;
                
                var possibility = new BioPossibility(childhood, adulthood);
                foreach (var trait in requiredTraits)
                {
                    possibility.Traits.Add(trait);
                }
                
                yield return (possibility, weightingSystem(possibility));
            }
        }

        private static bool UnworkableTraitCombination(IReadOnlyList<TraitRequirement> requiredTraits, BackstoryDef childhood, BackstoryDef adulthood)
        {
            int nonSexualityTraits = requiredTraits.Count(x => !x.def.IsSexualityTrait());
            bool validTraitCombo = true;
            foreach (var forcedTrait in childhood.forcedTraits ?? Enumerable.Empty<BackstoryTrait>())
            {
                if (!requiredTraits.AllowsTrait(forcedTrait.def))
                {
                    validTraitCombo = false;
                    break;
                }

                nonSexualityTraits++;
            }

            foreach (var forcedTrait in adulthood.forcedTraits ?? Enumerable.Empty<BackstoryTrait>())
            {
                if (!requiredTraits.AllowsTrait(forcedTrait.def))
                {
                    validTraitCombo = false;
                    break;
                }

                nonSexualityTraits++;
            }

            if (nonSexualityTraits > 3 || !validTraitCombo)
            {
                return true;
            }

            return false;
        }

        private bool AllowedBio(PawnBio bio) => bio.childhood.spawnCategories.Contains(_categoryName);
    }
}