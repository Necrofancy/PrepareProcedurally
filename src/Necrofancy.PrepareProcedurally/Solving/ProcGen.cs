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

        var empty = new List<TraitDef>();

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
            if (!(finalSkills[i] is { } finalization))
                continue;

            using (TemporarilyChange.ScenarioBannedTraits(empty))
            using (TemporarilyChange.PlayerFactionMelaninRange(MelaninRange))
            using (TemporarilyChange.SetAgeInRequest(backstory.AssumedAge, i))
            using (TemporarilyChange.GenderInRequest(backstory.Background.Gender, i))
            {
                pawnList[i] = StartingPawnUtility.RandomizeInPlace(pawnList[i]);
            }

            PostPawnGenerationChanges.ApplyBackstoryTo(backstory.Background, pawnList[i]);
            traits.ApplyRequestedTraitsTo(pawnList[i]);
            finalization.ApplySimulatedSkillsTo(pawnList[i]);
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
        var specifier = new SelectBackstorySpecifically(backstoryCategory);
        var bio = specifier.GetBestBio(collector.Weight, TraitRequirements[index]);
        var traits = bio.Traits;
        var empty = new List<TraitDef>();

        var builder = new PawnBuilder(bio);
        foreach (var (skill, usability) in reqs.OrderBy(x => x.Usability).ThenByDescending(x => x.Skill.listOrder))
            if (usability == UsabilityRequirement.Major)
                builder.TryLockInPassion(skill, Passion.Major);
            else if (usability == UsabilityRequirement.Minor)
                builder.TryLockInPassion(skill, Passion.Minor);

        var addBackToLocked = false;
        if (LockedPawns.Contains(pawn))
        {
            LockedPawns.Remove(pawn);
            addBackToLocked = true;
        }

        using (TemporarilyChange.ScenarioBannedTraits(empty))
        using (TemporarilyChange.PlayerFactionMelaninRange(MelaninRange))
        using (TemporarilyChange.SetAgeInRequest(age, index))
        using (TemporarilyChange.GenderInRequest(bio.Gender, index))
        {
            pawn = StartingPawnUtility.RandomizeInPlace(pawn);
            OnPawnChanged(pawn);
        }

        PostPawnGenerationChanges.ApplyBackstoryTo(bio, pawn);
        builder.Build().ApplySimulatedSkillsTo(pawn);
        traits.ApplyRequestedTraitsTo(pawn);

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