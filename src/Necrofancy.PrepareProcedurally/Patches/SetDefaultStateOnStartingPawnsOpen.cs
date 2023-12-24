using HarmonyLib;
using RimWorld;

namespace Necrofancy.PrepareProcedurally.Patches
{
    // ReSharper disable once UnusedType.Global
    [HarmonyPatch(typeof(Page_ConfigureStartingPawns), nameof(Page_ConfigureStartingPawns.PostOpen))]
    public class SetDefaultStateOnStartingPawnsOpen 
    {
        [HarmonyPostfix]
        // ReSharper disable once UnusedMember.Global
        // ReSharper disable once InconsistentNaming
        public static void PostOpen(Page_ConfigureStartingPawns __instance) 
        {
            Interface.Pages.PrepareProcedurally.SetDefaultState();
        }
    }
}