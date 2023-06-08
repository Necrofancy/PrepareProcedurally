using HarmonyLib;
using Verse;

namespace Necrofancy.PrepareProcedurally.Patches 
{
    [StaticConstructorOnStartup]
    public class HarmonyPatcher 
    {
        static HarmonyPatcher() => new Harmony("Necrofancy.PrepareProcedurally").PatchAll();
    }
}