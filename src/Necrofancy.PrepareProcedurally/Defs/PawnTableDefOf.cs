using RimWorld;

// ReSharper disable once UnusedType.Global
// ReSharper disable once InconsistentNaming

namespace Necrofancy.PrepareProcedurally.Defs;

[DefOf]
public static class PawnTableDefOf
{
    public static PawnTableDef PrepareProcedurallyResults;
        
    static PawnTableDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof (PawnTableDefOf));
}