namespace Necrofancy.PrepareProcedurally.Interface.PawnColumnWorkers;

[UsedImplicitly]
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
