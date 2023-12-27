using RimWorld;

// ReSharper disable All

namespace Necrofancy.PrepareProcedurally.Defs;

[DefOf]
public class BySetupOf
{
    public static BySetup Basic;

    static BySetupOf() => DefOfHelper.EnsureInitializedInCtor(typeof (BySetupOf));
}