using System;
using Necrofancy.PrepareProcedurally.Editing;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

// ReSharper disable once UnusedType.Global

namespace Necrofancy.PrepareProcedurally.Interface.PawnColumnWorkers;

public class LockForSelection : PawnColumnWorker_Icon
{
    private const string LockedDesc = "Necrofancy.PrepareProcedurally.LockedPawnTooltip";
    private const string UnlockedDesc = "Necrofancy.PrepareProcedurally.UnlockedPawnTooltip";

    private static EditorContext Editor => EditorSession.Require();

    protected override Texture2D GetIconFor(Pawn pawn)
    {
        return IsLocked(pawn) ? LazyTexture.Locked.Value : LazyTexture.Unlocked.Value;
    }

    protected override string GetIconTip(Pawn pawn)
    {
        return IsLocked(pawn) ? LockedDesc.Translate() : UnlockedDesc.Translate();
    }

    protected override void ClickedIcon(Pawn pawn)
    {
        var index = StartingPawnUtility.PawnIndex(pawn);
        if (IsLocked(pawn))
        {
            Editor[index].Locked = false;
            SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera();
        }
        else
        {
            Editor[index].Locked = true;
            SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera();
        }
    }

    protected override int Width => (int)Text.LineHeight;

    private static bool IsLocked(Pawn pawn) => Editor[pawn].Locked;
}
