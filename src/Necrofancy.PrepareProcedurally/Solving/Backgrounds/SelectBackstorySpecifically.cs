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

    private BackstoryDef forcedChildhood;
    private BackstoryDef forcedAdulthood;
    private PawnBio forcedBio;

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
    
    

    public IReadOnlyList<BackstoryOption> PossibleChildhoods(IReadOnlyList<TraitRequirement> requiredTraits, BackstoryDef adulthood = null)
    {
        var possibleChildhoods = new List<BackstoryOption>();

        foreach (var category in this.categoryFilters)
        {
            foreach (var backstory in DefDatabase<BackstoryDef>.AllDefsListForReading)
            {
                bool matches = 
                    backstory.shuffleable 
                    && category.Matches(backstory) 
                    && backstory.slot == BackstorySlot.Childhood;
                
                if (matches)
                {
                    possibleChildhoods.Add(new BackstoryOption(backstory));
                }
            }
        }
        
        if (adulthood?.shuffleable == true)
        {
            // no solid bios are possible.
            return possibleChildhoods;
        }

        foreach (var bio in SolidBioDatabase.allBios.Where(AllowedBio))
        {
            var possibility = new BioPossibility(bio);
            foreach (var trait in requiredTraits) possibility.Traits.Add(trait);
            if (!alreadyUsed.Contains(bio.childhood)
                && !UnworkableTraitCombination(requiredTraits, bio.childhood, bio.adulthood))
                possibleChildhoods.Add(new BackstoryOption(bio.childhood, bio.gender, bio));
        }

        return possibleChildhoods;
    }
    
    public IReadOnlyList<BackstoryOption> PossibleAdulthoods(IReadOnlyList<TraitRequirement> requiredTraits, BackstoryDef childhood = null)
    {
        var possibleAdulthoods = new List<BackstoryOption>();
        
        foreach (var category in this.categoryFilters)
        {
            foreach (var backstory in DefDatabase<BackstoryDef>.AllDefsListForReading)
            {
                bool matches = 
                    backstory.shuffleable 
                    && category.Matches(backstory) 
                    && backstory.slot == BackstorySlot.Adulthood;
                
                if (matches)
                {
                    possibleAdulthoods.Add(new BackstoryOption(backstory));
                }
            }
        }

        if (childhood?.shuffleable == true)
        {
            // no solid bios are possible.
            return possibleAdulthoods;
        }
        
        foreach (var bio in SolidBioDatabase.allBios.Where(AllowedBio))
        {
            var possibility = new BioPossibility(bio);
            foreach (var trait in requiredTraits) possibility.Traits.Add(trait);
            if (!alreadyUsed.Contains(bio.childhood)
                && !UnworkableTraitCombination(requiredTraits, bio.childhood, bio.adulthood))
                possibleAdulthoods.Add(new BackstoryOption(bio.adulthood, bio.gender, bio));
        }

        return possibleAdulthoods;
    }

    public void ForceChildhood(BackstoryDef childhood)
    {
        if (childhood.shuffleable)
        {
            forcedChildhood = childhood;
        }
        else
        {
            var bio = SolidBioDatabase.allBios.First(x => x.childhood == childhood);
            ForceBio(bio);
        }
    }

    public void ForceAdulthood(BackstoryDef adulthood)
    {
        if (adulthood.shuffleable)
        {
            forcedAdulthood = adulthood;
        }
        else
        {
            var bio = SolidBioDatabase.allBios.First(x => x.adulthood == adulthood);
            ForceBio(bio);
        }
    }

    public void ForceBio(PawnBio bio)
    {
        this.forcedBio = bio;
    }
        
    private IEnumerable<(BioPossibility Bio, float Ranking)> GetPawnPossibilities(
        WeightBackgroundAlgorithm weightingSystem, IReadOnlyList<TraitRequirement> requiredTraits)
    {
        if (forcedBio is not null)
        {
            var possibility = new BioPossibility(forcedBio);
            foreach (var trait in requiredTraits) possibility.Traits.Add(trait);
            yield return (possibility, PunishReusedSkills(possibility));
            yield break;
        }
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

        if (forcedChildhood is null && forcedAdulthood is null)
        {
            foreach (var bio in SolidBioDatabase.allBios.Where(AllowedBio))
            {
                var possibility = new BioPossibility(bio);
                foreach (var trait in requiredTraits) possibility.Traits.Add(trait);
                if (!alreadyUsed.Contains(bio.childhood)
                    && !UnworkableTraitCombination(requiredTraits, bio.childhood, bio.adulthood))
                    yield return (possibility, PunishReusedSkills(possibility));
            }
        }

        foreach (var category in categoryFilters)
        {
            List<BackstoryDef> childhoods = forcedChildhood is null ? [] : [forcedChildhood];
            List<BackstoryDef> adulthoods = forcedAdulthood is null ? [] : [forcedAdulthood];

            foreach (var backstory in DefDatabase<BackstoryDef>.AllDefsListForReading)
            {
                if (backstory.shuffleable && category.Matches(backstory) && !alreadyUsed.Contains(backstory))
                {
                    if (backstory.slot == BackstorySlot.Childhood && forcedChildhood is null)
                    {
                        childhoods.Add(backstory);
                    } 
                    else if (backstory.slot == BackstorySlot.Adulthood && forcedAdulthood is null)
                    {
                        adulthoods.Add(backstory);
                    }
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