using HarmonyLib;
using Verse;

// Resharper disable all

namespace Necrofancy.PrepareProcedurally
{
    [StaticConstructorOnStartup]
    public class HarmonyPatcher 
    {
        static HarmonyPatcher() => new Harmony("Necrofancy.PrepareProcedurally").PatchAll();
    }
}