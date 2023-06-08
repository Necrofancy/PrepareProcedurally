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
        
        private readonly List<BackstoryDef> _childhood = new List<BackstoryDef>();
        private readonly List<BackstoryDef> _adulthood = new List<BackstoryDef>();

        private readonly HashSet<BackstoryDef> _alreadyUsed = new HashSet<BackstoryDef>();

        public SelectBackstorySpecifically(string categoryName)
        {
            _categoryName = categoryName;
        }
        
        public BioPossibility GetBestBio(WeightBackgroundAlgorithm weightingSystem)
        {
            (BioPossibility? Bio, float Ranking) bestSoFar = (default, float.MinValue);
            foreach (var poss in GetPawnPossibilities(weightingSystem))
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

        private IEnumerable<(BioPossibility Bio, float Ranking)> GetPawnPossibilities(WeightBackgroundAlgorithm weightingSystem)
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
                if (!_alreadyUsed.Contains(bio.childhood))
                    yield return (possibility, weightingSystem(possibility));
            }
            
            foreach (var childhood in _childhood)
            foreach (var adulthood in _adulthood)
            {
                var requiredWork = childhood.requiredWorkTags | adulthood.requiredWorkTags;
                var disabledWork = childhood.workDisables | adulthood.workDisables;
                if ((requiredWork & disabledWork) != WorkTags.None)
                    continue;
                
                var possibility = new BioPossibility(childhood, adulthood);
                yield return (possibility, weightingSystem(possibility));
            }
        }

        private bool AllowedBio(PawnBio bio) => bio.childhood.spawnCategories.Contains(_categoryName);
    }
}