using System;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

namespace Necrofancy.PrepareProcedurally.Interface.PawnColumnWorkers;

[UsedImplicitly]
public class Race : PawnColumnWorker_Text
{
    protected override TextAnchor Anchor => TextAnchor.MiddleCenter;

    protected override string GetTextFor(Pawn pawn)
    {
        return pawn.def.LabelCap;
    }

    public override int GetMinWidth(PawnTable table)
    {
        float maxWidth = 0;
        foreach (var pawn in table.PawnsListForReading)
        {
            var label = GetTextFor(pawn);
            var width = Text.CalcSize(label).x + 8f;
            maxWidth = Math.Max(maxWidth, width);
        }

        return (int)maxWidth;
    }
}