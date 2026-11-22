using Necrofancy.PrepareProcedurally.Editing;
using Necrofancy.PrepareProcedurally.Solving;
using Necrofancy.PrepareProcedurally.Solving.Skills;
using Necrofancy.PrepareProcedurally.Solving.StateEdits;

namespace Necrofancy.PrepareProcedurally.Rules;

public sealed class VanillaFudge : Fudge
{
    public override IEnumerable<Hack> TeamwideGeneratorFudges(EditorContext context)
    {
        foreach (var raceAgeHack in HackFactory.AgePropertiesForEachRace(context.PawnKindAdultAges))
            yield return raceAgeHack;

        yield return HackFactory.SelectedMelaninRange(context.MelaninRange);
    }

    protected override IEnumerable<Hack> PawnSpecificFudges(Fudging fudging)
    {
        yield return HackFactory.MakeBackstoryShuffleable(fudging.ChosenPossibility.Background.Childhood);
        yield return HackFactory.MakeBackstoryShuffleable(fudging.ChosenPossibility.Background.Adulthood);
        yield return HackFactory.AdjustBackstorySpawnCategory(fudging.ChosenPossibility.Background.Childhood);
        yield return HackFactory.AdjustBackstorySpawnCategory(fudging.ChosenPossibility.Background.Adulthood);
        foreach (var requestHack in HackFactory.ForAdjustingPawnBackstory(fudging, fudging.ChosenPossibility))
            yield return requestHack;
    }

    protected override void PrePawnGeneration(Fudging context)
    {
        WorldPawnDestruction.DestroyRelationships(context.Slot.Pawn);
    }

    protected override void PostPawnGeneration(Fudging fudging)
    {
        Logging.AssertDefMatch(fudging.ChosenPossibility.Background.Childhood, fudging.Slot.Pawn.story.Childhood);
        Logging.AssertDefMatch(fudging.ChosenPossibility.Background.Adulthood, fudging.Slot.Pawn.story.Adulthood);

        var pawn = fudging.Slot.Pawn;
        if (fudging.SolidBio is not null)
            pawn.Name = fudging.SolidBio.name;
        if (!fudging.Slot.Context.AllowInjuries)
            PostPawnGenerationChanges.RemoveBadHeDiffs(pawn);
        fudging.Slot.CosmeticLock?.ApplyToPawn(fudging.Slot.Pawn);

        var finalSkillBuilder = PawnBuilder.ForPawn(fudging.Slot.Pawn);
        foreach (var (skill, requirement) in fudging.Skills.FinalRanges)
            finalSkillBuilder.TryLockInPassion(skill, requirement.Passion);

        finalSkillBuilder.Build().result.ApplySimulatedSkillsTo(fudging.Slot.Pawn);

        foreach (var window in Find.WindowStack.Windows)
        {
            switch (window)
            {
                case Page_ConfigureStartingPawns startingPage:
                    startingPage.SelectPawn(pawn);
                    break;
                case Interface.Pages.PrepareProcedurally _:
                    Editor.StartingPawns[fudging.Slot.Index] = pawn;
                    break;
            }
        }
    }
}
