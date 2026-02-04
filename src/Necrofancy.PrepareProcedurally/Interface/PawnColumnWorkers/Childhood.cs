using RimWorld;
using Verse;

// ReSharper disable once UnusedType.Global

namespace Necrofancy.PrepareProcedurally.Interface.PawnColumnWorkers;

public class Childhood : Backstory
{
    protected override BackstoryDef StoryFrom(Pawn pawn)
    {
        return pawn.story.Childhood;
    }
    protected override void SelectBackstory(Pawn pawn)
    {
        BackstorySelection.PossibleChildhoods(pawn, () => OnceSelected(pawn));
    }
    
    protected override bool Locked(Pawn pawn)
    {
        var index = StartingPawnUtility.PawnIndex(pawn);
        return Editor.SetChildhoods[index] is not null;
    }
}