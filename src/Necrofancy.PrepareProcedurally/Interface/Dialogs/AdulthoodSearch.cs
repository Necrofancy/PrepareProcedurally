#if SEARCH_MENU_AVAILABLE

using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Necrofancy.PrepareProcedurally.Interface.Dialogs;

public class AdulthoodSearch(Pawn referencePawn, List<BackstoryDef> backstories)
    : BackstorySearch(referencePawn, backstories)
{
    protected override void BackstorySelected(Pawn referencePawn, BackstoryDef element)
    {
        var index = StartingPawnUtility.PawnIndex(referencePawn);
        if (index == -1)
        {
            return;
        }
        
        Editor.SetAdulthoods[index] = element;
        Editor.LockedPawns.Remove(referencePawn);
        Editor.MakeDirty();
    }
}

#endif