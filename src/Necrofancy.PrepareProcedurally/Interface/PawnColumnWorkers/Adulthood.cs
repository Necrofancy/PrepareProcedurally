using System.Collections.Generic;
using System.Linq;
using Necrofancy.PrepareProcedurally.Editing;
using Necrofancy.PrepareProcedurally.Interface.Dialogs;
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
        var slot = EditorSession.Require()[pawn];
        BackstorySelection.PossibleAdulthoods(slot, () => OnceSelected(slot));
    }

    protected override bool Locked(Pawn pawn)
    {
        var index = StartingPawnUtility.PawnIndex(pawn);
        return EditorSession.Require().Slots[index].ForcedAdulthood is not null;
    }
}
