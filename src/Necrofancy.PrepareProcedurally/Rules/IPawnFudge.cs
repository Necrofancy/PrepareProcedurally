using Necrofancy.PrepareProcedurally.Editing;
using Necrofancy.PrepareProcedurally.Solving.StateEdits;

namespace Necrofancy.PrepareProcedurally.Rules;

public interface IPawnFudge
{
    IEnumerable<Hack> TeamwideGeneratorFudges(EditorContext context);

    void FudgePawnGenerationInTeam(Fudging fudging);

    void FudgeSoloPawnGeneration(Fudging fudging);
}
