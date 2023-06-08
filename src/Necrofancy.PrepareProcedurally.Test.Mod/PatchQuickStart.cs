using HarmonyLib;
using UnityEngine;
using Verse;

// ReSharper disable all - monke patch

namespace Necrofancy.PrepareProcedurally.Test.Mod
{
    [HarmonyPatch(typeof(QuickStarter))]
    [HarmonyPatch(nameof(QuickStarter.CheckQuickStart))]
    static class PatchQuickStart
    {
        static bool Prefix(ref bool __result)
        {
            OutputGenerator.ClearFiles();
            
            foreach (var scenario in Situation.GenerateAll())
                OutputGenerator.GenerateOutput(scenario);

            if (GenCommandLine.CommandLineArgPassed("exitafterscenarios"))
                Application.Quit();
            return true;
        }
    }
}