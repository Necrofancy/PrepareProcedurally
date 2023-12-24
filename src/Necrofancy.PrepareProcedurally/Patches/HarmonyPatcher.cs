using HarmonyLib;
using Verse;

namespace Necrofancy.PrepareProcedurally.Patches 
{
    [StaticConstructorOnStartup]
    // ReSharper disable once UnusedType.Global
    public class HarmonyPatcher 
    {
        static HarmonyPatcher() => new Harmony("Necrofancy.PrepareProcedurally").PatchAll();
    }
}