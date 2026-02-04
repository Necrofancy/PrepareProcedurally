using RimWorld;
using Verse;

// ReSharper disable once UnusedType.Global

namespace Necrofancy.PrepareProcedurally.Interface.PawnColumnWorkers;

public class Adulthood : Backstory
{
    protected override BackstoryDef StoryFrom(Pawn pawn)
    {
        return pawn.story.Adulthood;
    }
    protected override void SelectBackstory(Pawn pawn)
    {
        BackstorySelection.PossibleAdulthoods(pawn, () => OnceSelected(pawn));
    }

    protected override bool Locked(Pawn pawn)
    {
        var index = StartingPawnUtility.PawnIndex(pawn);
        return Editor.SetAdulthoods[index] is not null;
    }
}