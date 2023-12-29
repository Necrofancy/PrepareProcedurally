using System;
using System.Collections.Generic;
using System.Linq;
using Necrofancy.PrepareProcedurally.Solving.Weighting;
using RimWorld;
using Verse;

namespace Necrofancy.PrepareProcedurally.Solving.Backgrounds;

public class SelectBackstorySpecifically
{
    private readonly List<string> categoryNames;

    private readonly List<BackstoryDef> childhoodStories = new();
    private readonly List<BackstoryDef> adulthoodStories = new();

    private readonly HashSet<BackstoryDef> alreadyUsed = new();

    public SelectBackstorySpecifically(List<string> spawnCategories)
    {
        categoryNames = spawnCategories;
        foreach (var pawn in Editor.LockedPawns)
        {
            var story = pawn.story;
            alreadyUsed.Add(story.Childhood);
            if (story.Adulthood != null)
                alreadyUsed.Add(story.Adulthood);
        }
    }

    public SelectBackstorySpecifically(string categoryName)
    {
        categoryNames = new List<string> { categoryName };
        foreach (var pawn in Editor.LockedPawns)
        {
            var story = pawn.story;
            alreadyUsed.Add(story.Childhood);
            if (story.Adulthood != null)
                alreadyUsed.Add(story.Adulthood);
        }
    }

    public void AlreadyUsedBackstory(BioPossibility possibility)
    {
        alreadyUsed.Add(possibility.Childhood);
        if (possibility.Adulthood is { } adulthood)
            alreadyUsed.Add(adulthood);
    }

    public BioPossibility GetBestBio(WeightBackgroundAlgorithm weightingSystem,
        IReadOnlyList<TraitRequirement> requiredTraits)
    {
        (BioPossibility? Bio, float Ranking) bestSoFar = (default, float.MinValue);
        foreach (var poss in GetPawnPossibilities(weightingSystem, requiredTraits))
            if (poss.Ranking >= bestSoFar.Ranking)
                bestSoFar = poss;

        var best = bestSoFar.Bio
                   ?? throw new InvalidOperationException("No valid pawn possibilities were found.");

        alreadyUsed.Add(best.Childhood);
        alreadyUsed.Add(best.Adulthood);

        return best;
    }

    private IEnumerable<(BioPossibility Bio, float Ranking)> GetPawnPossibilities(
        WeightBackgroundAlgorithm weightingSystem, IReadOnlyList<TraitRequirement> requiredTraits)
    {
        childhoodStories.Clear();
        adulthoodStories.Clear();

        foreach (var backstory in DefDatabase<BackstoryDef>.AllDefsListForReading)
            if (backstory.shuffleable && backstory.spawnCategories.Any(x => categoryNames.Contains(x)))
            {
                var list = backstory.slot == BackstorySlot.Childhood ? childhoodStories : adulthoodStories;
                if (!alreadyUsed.Contains(backstory))
                    list.Add(backstory);
            }

        foreach (var bio in SolidBioDatabase.allBios.Where(AllowedBio))
        {
            var possibility = new BioPossibility(bio);
            foreach (var trait in requiredTraits) possibility.Traits.Add(trait);
            if (!alreadyUsed.Contains(bio.childhood)
                && !UnworkableTraitCombination(requiredTraits, bio.childhood, bio.adulthood))
                yield return (possibility, weightingSystem(possibility));
        }

        foreach (var childhood in childhoodStories)
        foreach (var adulthood in adulthoodStories)
        {
            if (UnworkableTraitCombination(requiredTraits, childhood, adulthood))
                continue;

            var requiredWork = childhood.requiredWorkTags | adulthood.requiredWorkTags;
            var disabledWork = childhood.workDisables | adulthood.workDisables;
            if ((requiredWork & disabledWork) != WorkTags.None)
                continue;

            var possibility = new BioPossibility(childhood, adulthood);
            foreach (var trait in requiredTraits) possibility.Traits.Add(trait);

            yield return (possibility, weightingSystem(possibility));
        }
    }

    private static bool UnworkableTraitCombination(IReadOnlyList<TraitRequirement> requiredTraits,
        BackstoryDef childhood, BackstoryDef adulthood)
    {
        var nonSexualityTraits = requiredTraits.Count(x => !x.def.IsSexualityTrait());
        var validTraitCombo = true;
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

        if (nonSexualityTraits > 3 || !validTraitCombo) return true;

        return false;
    }

    private bool AllowedBio(PawnBio bio)
    {
        return bio.childhood.spawnCategories.Any(x => categoryNames.Contains(x));
    }
}