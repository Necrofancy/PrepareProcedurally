using Necrofancy.PrepareProcedurally.Editing;
using Necrofancy.PrepareProcedurally.Solving.StateEdits;

namespace Necrofancy.PrepareProcedurally.Rules;

public abstract class Fudge : IPawnFudge
{
    public abstract IEnumerable<Hack> TeamwideGeneratorFudges(EditorContext context);

    public void FudgePawnGenerationInTeam(Fudging fudging)
    {
        PrePawnGeneration(fudging);

        using (EditSequence.Apply(PawnSpecificFudges(fudging)))
            Generate(fudging);

        if (DetectPawnGenerationFailure(fudging))
            return;

        PostPawnGeneration(fudging);
    }

    public void FudgeSoloPawnGeneration(Fudging fudging)
    {
        using (EditSequence.Apply(TeamwideGeneratorFudges(fudging.Slot.Context)))
            FudgePawnGenerationInTeam(fudging);
    }

    protected abstract IEnumerable<Hack> PawnSpecificFudges(Fudging fudging);

    protected virtual void PrePawnGeneration(Fudging fudging) { }

    protected virtual void Generate(Fudging fudging) => StartingPawnUtility.RandomizeInPlace(fudging.Slot.Pawn);

    protected virtual bool DetectPawnGenerationFailure(Fudging fudging)
    {
        // I swear this was not meant to read like minced-oath censoring of swears; it just ended up that way
        if (fudging.Slot.Pawn is not null)
            return false;

        Logging.Error("Pawn generated null; this is likely to a pawn being so crippled they spawned downed");
        var rerolled = StartingPawnUtility.NewGeneratedStartingPawn(fudging.Slot.Index);
        Find.GameInitData.startingAndOptionalPawns[fudging.Slot.Index] = rerolled;
        return true;
    }

    protected virtual void PostPawnGeneration(Fudging fudging) { }
}
