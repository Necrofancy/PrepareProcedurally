using RimWorld;
using UnityEngine;
using Verse;

// ReSharper disable once UnusedType.Global

namespace Necrofancy.PrepareProcedurally.Interface.PawnColumnWorkers
{
    public class WorkPotential : PawnColumnWorker
    {
        public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
        {
            // TODO figure out how the heck the labor table works...
        }

        public override int GetMinWidth(PawnTable table)
        {
            return 300;
        }
    }
}