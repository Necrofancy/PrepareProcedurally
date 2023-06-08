using RimWorld;

namespace Necrofancy.PrepareProcedurally.Defs
{
    [DefOf]
    public class BySetupOf
    {
        public static BySetup Basic;

        static BySetupOf() => DefOfHelper.EnsureInitializedInCtor(typeof (BySetupOf));
    }
}