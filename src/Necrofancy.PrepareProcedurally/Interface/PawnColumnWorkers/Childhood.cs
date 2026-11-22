using System.Linq;
using Necrofancy.PrepareProcedurally.Editing;
using Necrofancy.PrepareProcedurally.Interface.Dialogs;
using RimWorld;
using Verse;

// ReSharper disable once UnusedType.Global

namespace Necrofancy.PrepareProcedurally.Interface.PawnColumnWorkers;

public class Childhood : Backstory
{
    protected override BackstoryDef StoryFrom(Pawn pawn) => pawn.story.Childhood;

    protected override void SelectBackstory(Pawn pawn)
    {
        var slot = EditorSession.Require()[pawn];
        BackstorySelection.PossibleChildhoods(slot, () => OnceSelected(slot));    }

    protected override bool Locked(Pawn pawn)
    {
        var index = StartingPawnUtility.PawnIndex(pawn);
        return EditorSession.Require().Slots[index].ForcedChildhood is not null;
    }
}
