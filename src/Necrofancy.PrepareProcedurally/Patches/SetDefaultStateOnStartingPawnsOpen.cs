using HarmonyLib;
using Necrofancy.PrepareProcedurally.Editor;
using RimWorld;

namespace Necrofancy.PrepareProcedurally.Patches
{
    // ReSharper disable once UnusedType.Global
    [HarmonyPatch(typeof(Page_ConfigureStartingPawns), nameof(Page_ConfigureStartingPawns.PostOpen))]
    public class SetDefaultStateOnStartingPawnsOpen 
    {
        [HarmonyPostfix]
        public static void PostOpen(Page_ConfigureStartingPawns __instance) 
        {
            Interface.Pages.PrepareProcedurally.SetDefaultState();
        }
    }
}