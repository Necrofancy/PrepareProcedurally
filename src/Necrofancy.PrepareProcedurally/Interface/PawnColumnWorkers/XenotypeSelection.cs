using RimWorld;
using Verse;

// ReSharper disable once UnusedType.Global

namespace Necrofancy.PrepareProcedurally.Interface.PawnColumnWorkers
{
    public class XenotypeSelection : PawnColumnWorker_Xenotype
    {
        protected override void ClickedIcon(Pawn pawn)
        {
            // TODO: Steal Selectable Window From Pawn Page
        }
        
        public override int GetMinWidth(PawnTable table) => 50;
    }
}