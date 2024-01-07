using System;
using System.Collections.Generic;
using HarmonyLib;
using Necrofancy.PrepareProcedurally.HumanoidAlienRaces.Solving;
using Verse;

namespace Necrofancy.PrepareProcedurally.HumanoidAlienRaces;

[StaticConstructorOnStartup]
public class HarmonyPatches
{
    static HarmonyPatches()
    {
        Harmony harmony = new Harmony("Necrofancy.PrepareProcedurally.HumanoidAlienRaces");
        
        var startingUtility = typeof(StartingPawnUtility);

        const string startingUtilityField = "DefaultStartingPawnRequest";
        var getter = AccessTools.PropertyGetter(startingUtility, startingUtilityField);
        var meddleWithOutput = AccessTools.Method(typeof(HarmonyPatches), nameof(EditPawnRequestAfterGeneration));
        harmony.Patch(getter, postfix: new HarmonyMethod(meddleWithOutput));
    }

    // ReSharper disable once InconsistentNaming
    private static void EditPawnRequestAfterGeneration(ref PawnGenerationRequest __result)
    {
        PawnGenerationRequestTransforms.ApplyChangesTo(ref __result);
    }
}