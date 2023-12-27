using RimWorld;
using UnityEngine;
using Verse;

// ReSharper disable once UnusedType.Global

namespace Necrofancy.PrepareProcedurally.Interface.PawnColumnWorkers;

public class IdeologicalRole : PawnColumnWorker
{
    public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
    {
        // TODO: Selectable Icon of Ideological Roles
    }

    public override int GetMinWidth(PawnTable table) => 50;
}