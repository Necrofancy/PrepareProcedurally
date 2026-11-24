using HarmonyLib;
using JetBrains.Annotations;

namespace Necrofancy.PrepareProcedurally.HumanoidAlienRaces;

[StaticConstructorOnStartup, UsedImplicitly]
public class HarmonyPatches
{
    static HarmonyPatches()
    {
        Harmony harmony = new Harmony("Necrofancy.PrepareProcedurally.HumanoidAlienRaces");

        var startingUtility = typeof(StartingPawnUtility);

        const string DefaultStartingPawnRequestGetter = "DefaultStartingPawnRequest";
        var getter = AccessTools.PropertyGetter(startingUtility, DefaultStartingPawnRequestGetter);
        var intercept = AccessTools.Method(typeof(HarmonyPatches), nameof(EditPawnRequestBeforeGeneration));
        harmony.Patch(getter, postfix: new HarmonyMethod(intercept));
    }

    // ReSharper disable once InconsistentNaming
    private static void EditPawnRequestBeforeGeneration(ref PawnGenerationRequest __result)
    {
        RequestInterceptor.Instance.Intercept(ref __result);
    }
}
