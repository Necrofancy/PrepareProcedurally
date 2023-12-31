﻿using System;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

// ReSharper disable once UnusedType.Global

namespace Necrofancy.PrepareProcedurally.Interface.PawnColumnWorkers;

public class LockForSelection : PawnColumnWorker_Icon
{
    private static readonly Lazy<Texture2D> UnlockedTex = LazyLoad("UI/Overlays/LockedMonochrome");
    private static readonly Lazy<Texture2D> LockedTex = LazyLoad("UI/Overlays/Locked");

    private const string LockedDesc = "Necrofancy.PrepareProcedurally.LockedPawnTooltip";
    private const string UnlockedDesc = "Necrofancy.PrepareProcedurally.UnlockedPawnTooltip";

    protected override Texture2D GetIconFor(Pawn pawn)
    {
        return IsLocked(pawn) ? LockedTex.Value : UnlockedTex.Value;
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

    /// <summary>
    /// Lazy load any related textures to avoid having something try resolving off the UI thread at start.
    /// </summary>
    private static Lazy<Texture2D> LazyLoad(string constString)
    {
        return new Lazy<Texture2D>(() => ContentFinder<Texture2D>.Get(constString));
    }
}