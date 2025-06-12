using System.Collections.Generic;
using System.Linq;
using Necrofancy.PrepareProcedurally.Interface.Dialogs;
using Necrofancy.PrepareProcedurally.Solving;
using Necrofancy.PrepareProcedurally.Solving.Backgrounds;
using Necrofancy.PrepareProcedurally.Solving.Skills;
using Necrofancy.PrepareProcedurally.Solving.StateEdits;
using Necrofancy.PrepareProcedurally.Solving.Weighting;
using RimWorld;
using Verse;

namespace Necrofancy.PrepareProcedurally.HumanoidAlienRaces.Solving;

using static Editor;

/// <summary>
/// Procedural generation rules specific to Humanoid Alien Races.
/// </summary>
public static class AlienProcGen
{
    public static Pawn RandomizeSingularPawn(Pawn pawn, CollectSpecificPassions collector,
        List<(SkillDef Skill, UsabilityRequirement Usability)> reqs)
    {
        var index = StartingPawnUtility.PawnIndex(pawn);

        var addBackToLocked = false;
        if (LockedPawns.Contains(pawn))
        {
            LockedPawns.Remove(pawn);
            addBackToLocked = true;
        }

        string pawnChildhoods = null, pawnAdulthoods = null;
        
        using (TemporarilyChange.PlayerFactionMelaninRange(MelaninRange))
        using (TemporarilyChange.AgeOnAllRelevantRaceProperties(RaceAgeRanges))
        {
            var foundBackstoryToWorkWith = false;
            while (!foundBackstoryToWorkWith)
            {
                PawnGenerationRequestTransforms.SetBasedOnEditorSettings(index);
                pawn = StartingPawnUtility.RandomizeInPlace(pawn);
                ProcGen.OnPawnChanged(pawn);

                if (pawn.story.Childhood.spawnCategories.Any() &&
                    pawn.story.Adulthood?.spawnCategories.Any() == true)
                {
                    pawnChildhoods = pawn.story.Childhood.spawnCategories.First();
                    pawnAdulthoods = pawn.story.Adulthood.spawnCategories.First();
                    foundBackstoryToWorkWith = true;
                }
            }
        }

        var backstoryFilter = new BackstoryCategoryFilter
        {
            categoriesChildhood = [pawnChildhoods],
            categoriesAdulthood = [pawnAdulthoods]
        };
        var specifier = new SelectBackstorySpecifically(backstoryFilter, GenderRequirements[index], pawn);
        var bio = specifier.GetBestBio(collector.Weight, TraitRequirements[index]);
        var traits = bio.Traits;
        AlienSpecificPostPawnGenerationChanges.ApplyBackstoryTo(bio, pawn);
        traits.ApplyRequestedTraitsTo(pawn);
        if (!AllowBadHeDiffs)
            PostPawnGenerationChanges.RemoveBadHeDiffs(pawn);

        var builder = PawnBuilder.ForPawn(pawn);
        foreach (var (skill, usability) in reqs.OrderBy(x => x.Usability).ThenByDescending(x => x.Skill.listOrder))
            if (usability == UsabilityRequirement.Major)
                builder.TryLockInPassion(skill, Passion.Major);
            else if (usability == UsabilityRequirement.Minor)
                builder.TryLockInPassion(skill, Passion.Minor);

        builder.Build().result.ApplySimulatedSkillsTo(pawn);

        if (addBackToLocked) LockedPawns.Add(pawn);

        return pawn;
    }

    public static void RandomizeTeam(BalancingSituation situation)
    {
        // it's actually impossible to try balancing up-front because I don't know what backstories are available
        // so let's generate the pawns first to figure out their race and figure it out from there.
        List<AlienCategories> categories = new(StartingPawns.Count);
        
        using (TemporarilyChange.PlayerFactionMelaninRange(MelaninRange))
        using (TemporarilyChange.AgeOnAllRelevantRaceProperties(RaceAgeRanges))
        {
            for (var pawnIndex = 0; pawnIndex < StartingPawns.Count; pawnIndex++)
            {
                var pawn = StartingPawns[pawnIndex];
                if (LockedPawns.Contains(pawn))
                {
                    categories.Add(default);
                    continue;
                }

                var foundBackstoryToWorkWith = false;
                while (!foundBackstoryToWorkWith)
                {
                    PawnGenerationRequestTransforms.SetBasedOnEditorSettings(pawnIndex);                    
                    pawn = StartingPawnUtility.RandomizeInPlace(pawn);
                    ProcGen.OnPawnChanged(pawn);

                    if (pawn.story.Childhood.spawnCategories.Any() &&
                        pawn.story.Adulthood?.spawnCategories.Any() == true)
                    {
                        var childhoodCategory = pawn.story.Childhood.spawnCategories.First();
                        var adulthoodCategory = pawn.story.Adulthood.spawnCategories.First();
                        categories.Add(new AlienCategories(childhoodCategory, adulthoodCategory));
                        foundBackstoryToWorkWith = true;
                    }
                }
            }
        }

        AlienBalancingSituation alienSituation = new(categories, StartingPawns.Count, situation.SkillRequirements);

        var variation = new IntRange(10, (int)(SkillWeightVariation * 10));
        var backgrounds = AlienBackstorySolver.TryToSolveWith(alienSituation, variation);
        var finalSkills = BackstorySolver.FigureOutPassions(backgrounds, situation);

        for (var pawnIndex = 0; pawnIndex < StartingPawns.Count; pawnIndex++)
        {
            var backstory = backgrounds[pawnIndex];
            var traits = backstory.Background.Traits;

            if (finalSkills[pawnIndex] is not { } finalization)
                continue;

            var pawn = StartingPawns[pawnIndex];

            AlienSpecificPostPawnGenerationChanges.ApplyBackstoryTo(backstory.Background, pawn);
            traits.ApplyRequestedTraitsTo(pawn);
            if (!AllowBadHeDiffs)
                PostPawnGenerationChanges.RemoveBadHeDiffs(pawn);

            var finalizationWithTraits = PawnBuilder.ForPawn(pawn);
            foreach (var (skill, requirement) in finalization.FinalRanges)
                finalizationWithTraits.TryLockInPassion(skill, requirement.Passion);

            finalizationWithTraits.Build().result.ApplySimulatedSkillsTo(pawn);

            ProcGen.OnPawnChanged(pawn);
        }
    }
}