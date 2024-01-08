using System.Collections.Generic;
using System.Linq;
using Necrofancy.PrepareProcedurally.Interface.Dialogs;
using Necrofancy.PrepareProcedurally.Solving.Backgrounds;
using Necrofancy.PrepareProcedurally.Solving.Skills;
using Necrofancy.PrepareProcedurally.Solving.StateEdits;
using Necrofancy.PrepareProcedurally.Solving.Weighting;
using RimWorld;
using Verse;
using static Necrofancy.PrepareProcedurally.Editor;

namespace Necrofancy.PrepareProcedurally.Solving;

/// <summary>
/// Procedural generation for vanilla pawns.
/// </summary>
public static class ProcGen
{
    public static void RandomizeForTeam(BalancingSituation situation)
    {
        var pawnList = Find.GameInitData.startingAndOptionalPawns;
        var pawnCount = Find.GameInitData.startingPawnCount;

        var generationCurve = Faction.OfPlayer.def.basicMemberKind.race.race.ageGenerationCurve;
        var ageCurve = EstimateRolling.SubSampleCurve(generationCurve, AgeRange);

        var variation = new IntRange(10, (int)(SkillWeightVariation * 10));
        var backgrounds = BackstorySolver.TryToSolveWith(situation, ageCurve, variation);
        var finalSkills = BackstorySolver.FigureOutPassions(backgrounds, situation);

        LastResults = finalSkills;
        for (var i = 0; i < pawnCount; i++)
        {
            var backstory = backgrounds[i];
            var traits = backstory.Background.Traits;
            List<TraitDef> traitsToBan = new();
            if (!(finalSkills[i] is { } finalization))
                continue;

            foreach (var trait in TraitsThatDisablePassions)
                if (trait.conflictingPassions.Any(x => finalization.FinalRanges[x].Passion > Passion.None))
                    traitsToBan.Add(trait);

            Pawn pawn;

            var age = backstory.AssumedAge;
            
            var gender = backstory.Background.Gender != GenderPossibility.Either
                ? backstory.Background.Gender
                : GenderRequirements[i];
            
            using (TemporarilyChange.SetSpecificRequest(i, age, gender, AllowBadHeDiffs, AllowRelationships))
            using (TemporarilyChange.ScenarioBannedTraits(traitsToBan))
            using (TemporarilyChange.PlayerFactionMelaninRange(MelaninRange))
            {
                pawn = StartingPawnUtility.RandomizeInPlace(pawnList[i]);
            }

            PostPawnGenerationChanges.ApplyBackstoryTo(backstory.Background, pawn);
            traits.ApplyRequestedTraitsTo(pawn);

            var finalizationWithTraits = PawnBuilder.ForPawn(pawn);
            foreach (var (skill, requirement) in finalization.FinalRanges)
                finalizationWithTraits.TryLockInPassion(skill, requirement.Passion);

            finalizationWithTraits.Build().result.ApplySimulatedSkillsTo(pawn);

            OnPawnChanged(pawnList[i]);
        }
    }

    public static Pawn RandomizeSingularPawn(Pawn pawn, CollectSpecificPassions collector,
        List<(SkillDef Skill, UsabilityRequirement Usability)> reqs)
    {
        var generationCurve = Faction.OfPlayer.def.basicMemberKind.race.race.ageGenerationCurve;
        var ageCurve = EstimateRolling.SubSampleCurve(generationCurve, AgeRange);
        var age = Rand.ByCurve(ageCurve);

        var index = StartingPawnUtility.PawnIndex(pawn);
        var backstoryCategory = Faction.OfPlayer.def.backstoryFilters.First().categories.First();
        var specifier = new SelectBackstorySpecifically(backstoryCategory, GenderRequirements[index]);
        var bio = specifier.GetBestBio(collector.Weight, TraitRequirements[index]);
        var traits = bio.Traits;

        List<TraitDef> traitsToBan = new();
        foreach (var trait in TraitsThatDisablePassions)
        foreach (var (skill, _) in reqs.Where(x => x.Usability >= UsabilityRequirement.Minor))
            if (trait.conflictingPassions.Contains(skill))
                traitsToBan.Add(trait);

        var addBackToLocked = false;
        if (LockedPawns.Contains(pawn))
        {
            LockedPawns.Remove(pawn);
            addBackToLocked = true;
        }

        var gender = bio.Gender != GenderPossibility.Either
            ? bio.Gender
            : GenderRequirements[index];

        using (TemporarilyChange.ScenarioBannedTraits(traitsToBan))
        using (TemporarilyChange.PlayerFactionMelaninRange(MelaninRange))
        using (TemporarilyChange.SetSpecificRequest(index, age, gender, AllowBadHeDiffs, AllowRelationships))
        {
            pawn = StartingPawnUtility.RandomizeInPlace(pawn);
            OnPawnChanged(pawn);
        }

        PostPawnGenerationChanges.ApplyBackstoryTo(bio, pawn);
        traits.ApplyRequestedTraitsTo(pawn);

        var finalPawnBuild = PawnBuilder.ForPawn(pawn);
        foreach (var (skill, requirement) in reqs)
        {
            var passion = requirement switch
            {
                UsabilityRequirement.Major => Passion.Major,
                UsabilityRequirement.Minor => Passion.Minor,
                _ => Passion.None
            };

            finalPawnBuild.TryLockInPassion(skill, passion);
        }

        finalPawnBuild.Build().result.ApplySimulatedSkillsTo(pawn);

        if (addBackToLocked) LockedPawns.Add(pawn);

        return pawn;
    }

    public static void OnPawnChanged(Pawn pawn)
    {
        foreach (var window in Find.WindowStack.Windows)
            switch (window)
            {
                case Page_ConfigureStartingPawns startingPage:
                    startingPage.SelectPawn(pawn);
                    break;
                case Interface.Pages.PrepareProcedurally _:
                    var index = StartingPawnUtility.PawnIndex(pawn);
                    StartingPawns[index] = pawn;
                    break;
            }
    }

    public static void CleanUpOnError()
    {
        LockedPawns.Clear();
        var startingPawns = Find.GameInitData.startingAndOptionalPawns;
        for (var i = 0; i < startingPawns.Count; i++)
        {
            startingPawns[i] = StartingPawnUtility.RandomizeInPlace(startingPawns[i]);
            OnPawnChanged(startingPawns[i]);
        }
    }
}