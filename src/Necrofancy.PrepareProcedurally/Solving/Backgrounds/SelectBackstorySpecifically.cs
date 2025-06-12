using System;
using System.Collections.Generic;
using System.Linq;
using Necrofancy.PrepareProcedurally.Solving.Weighting;
using RimWorld;
using Verse;

namespace Necrofancy.PrepareProcedurally.Solving.Backgrounds;

public class SelectBackstorySpecifically
{
    private readonly List<BackstoryCategoryFilter> categoryFilters;
    private readonly GenderPossibility genderRequirement;

    private readonly HashSet<BackstoryDef> alreadyUsed = new();
    private readonly List<TraitRequirement> alreadyForcedTraits = new();

    public SelectBackstorySpecifically(List<BackstoryCategoryFilter> spawnCategories, GenderPossibility genderRequirement,
        IEnumerable<BackgroundPossibility> possibilities)
    {
        categoryFilters = spawnCategories;
        this.genderRequirement = genderRequirement;
        foreach (var pawn in Editor.LockedPawns)
        {
            AddToAlreadyUsed(pawn);
        }

        foreach (var pawn in possibilities)
        {
            AddToAlreadyUsed(pawn.Background);
        }
    }

    public SelectBackstorySpecifically(List<BackstoryCategoryFilter> categoryFilters, GenderPossibility genderRequirement,
        Pawn pawn) : this(categoryFilters, genderRequirement, Enumerable.Empty<BackgroundPossibility>())
    {
        AddToAlreadyUsed(pawn.story.Childhood);
        if (pawn.story.Adulthood is { } adulthood)
        {
            AddToAlreadyUsed(adulthood);
        }
    }

    public SelectBackstorySpecifically(BackstoryCategoryFilter backstoryFilter, GenderPossibility genderRequirement, IEnumerable<BackgroundPossibility> possibilities)
        : this([backstoryFilter], genderRequirement, possibilities)
    {
        // no special category.
    }

    public SelectBackstorySpecifically(BackstoryCategoryFilter categoryName, GenderPossibility genderRequirement, Pawn pawn)
    {
        categoryFilters = [categoryName];
        this.genderRequirement = genderRequirement;
        alreadyUsed.Add(pawn.story.Childhood);
        if (pawn.story.Adulthood is { } adulthood)
        {
            alreadyUsed.Add(adulthood);
        }
        
        foreach (var lockedPawn in Editor.LockedPawns)
        {
            var story = lockedPawn.story;
            alreadyUsed.Add(story.Childhood);
            if (story.Adulthood != null)
                alreadyUsed.Add(story.Adulthood);
        }
    }

    public BioPossibility GetBestBio(WeightBackgroundAlgorithm weightingSystem,
        IReadOnlyList<TraitRequirement> requiredTraits)
    {
        (BioPossibility? Bio, float Ranking) bestSoFar = (default, float.MinValue);
        foreach (var poss in GetPawnPossibilities(weightingSystem, requiredTraits))
        {
            if (poss.Ranking >= bestSoFar.Ranking)
                bestSoFar = poss;
        }

        var best = bestSoFar.Bio
                   ?? throw new InvalidOperationException("No valid pawn possibilities were found.");

        alreadyUsed.Add(best.Childhood);
        alreadyUsed.Add(best.Adulthood);

        return best;
    }

    private IEnumerable<(BioPossibility Bio, float Ranking)> GetPawnPossibilities(
        WeightBackgroundAlgorithm weightingSystem, IReadOnlyList<TraitRequirement> requiredTraits)
    {
        float PunishReusedSkills(BioPossibility possibility)
        {
            var ranking = weightingSystem(possibility);
            foreach (var newlyForced in possibility.Traits)
            foreach (var alreadyForced in this.alreadyForcedTraits)
            {
                if (newlyForced.def == alreadyForced.def && newlyForced.degree == alreadyForced.degree)
                {
                    ranking *= 0.66f;
                }
            }

            return ranking;
        }
        
        foreach (var bio in SolidBioDatabase.allBios.Where(AllowedBio))
        {
            var possibility = new BioPossibility(bio);
            foreach (var trait in requiredTraits) possibility.Traits.Add(trait);
            if (!alreadyUsed.Contains(bio.childhood)
                && !UnworkableTraitCombination(requiredTraits, bio.childhood, bio.adulthood))
                yield return (possibility, PunishReusedSkills(possibility));
        }

        foreach (var category in this.categoryFilters)
        {
            List<BackstoryDef> childhoods = new();
            List<BackstoryDef> adulthoods = new();

            foreach (var backstory in DefDatabase<BackstoryDef>.AllDefsListForReading)
            {
                if (backstory.shuffleable && category.Matches(backstory) && !alreadyUsed.Contains(backstory))
                {
                    var list = backstory.slot == BackstorySlot.Childhood ? childhoods : adulthoods;
                    list.Add(backstory);
                }
            }

            foreach (var childhood in childhoods)
            foreach (var adulthood in adulthoods)
            {
                if (UnworkableTraitCombination(requiredTraits, childhood, adulthood))
                    continue;

                var requiredWork = childhood.requiredWorkTags | adulthood.requiredWorkTags;
                var disabledWork = childhood.workDisables | adulthood.workDisables;
                if ((requiredWork & disabledWork) != WorkTags.None)
                    continue;

                var possibility = new BioPossibility(childhood, adulthood);
                foreach (var trait in requiredTraits) possibility.Traits.Add(trait);

                yield return (possibility, PunishReusedSkills(possibility));
            }
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
    
    private void AddToAlreadyUsed(BioPossibility possibility)
    {
        AddToAlreadyUsed(possibility.Childhood);
        if (possibility.Adulthood is { } adulthood)
            AddToAlreadyUsed(adulthood);
    }
    
    private void AddToAlreadyUsed(Pawn pawn)
    {
        AddToAlreadyUsed(pawn.story.Childhood);
        if (pawn.story.Adulthood != null)
            AddToAlreadyUsed(pawn.story.Adulthood);
    }

    public void AddToAlreadyUsed(BackstoryDef def)
    {
        if (alreadyUsed.Add(def) && def.forcedTraits is List<BackstoryTrait> requirements)
        {
            foreach (var requirement in requirements)
            {
                alreadyForcedTraits.Add(new TraitRequirement{def = requirement.def, degree = requirement.degree});
            }
        }
    }
    
    private bool AllowedBio(PawnBio bio)
    {
        var genderMatch = (genderRequirement == GenderPossibility.Either || genderRequirement == bio.gender);
        if (!genderMatch)
        {
            return false;
        }

        return categoryFilters.Any(x => x.Matches(bio));
    }
}