using HarmonyLib;
using Verse;

// ReSharper disable all - monke patch

namespace Necrofancy.PrepareProcedurally.Test.Mod
{
    public class RunTestsAsMod : Verse.Mod
    {
        public RunTestsAsMod(ModContentPack content) : base(content)
        {
            if (GenCommandLine.CommandLineArgPassed("runscenarios"))
            {
                new Harmony("Necrofancy.PrepareProcedurally.Test.Mod").PatchAll();
            }
        }
    }
}