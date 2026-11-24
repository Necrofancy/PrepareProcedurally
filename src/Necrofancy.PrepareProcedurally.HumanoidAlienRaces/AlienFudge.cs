using Necrofancy.PrepareProcedurally.Editing;
using Necrofancy.PrepareProcedurally.Rules;
using Necrofancy.PrepareProcedurally.Solving.StateEdits;

namespace Necrofancy.PrepareProcedurally.HumanoidAlienRaces;

public class AlienFudge : Fudge
{
    public override IEnumerable<Hack> TeamwideGeneratorFudges(EditorContext context)
    {
        yield break;
    }

    protected override IEnumerable<Hack> PawnSpecificFudges(Fudging fudging)
    {
        yield break;
    }
}
