using System;
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
        if (IsLocked(pawn))
        {
            Editor.LockedPawns.Remove(pawn);
            SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera();
        }
        else
        {
            Editor.LockedPawns.Add(pawn);
            SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera();
        }
    }

    protected override int Width => (int)Text.LineHeight;

    private static bool IsLocked(Pawn pawn)
    {
        return Editor.LockedPawns.Contains(pawn);
    }
}