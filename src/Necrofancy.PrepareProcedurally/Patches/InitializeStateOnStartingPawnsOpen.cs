using HarmonyLib;
using Necrofancy.PrepareProcedurally.Solving;
using RimWorld;

// Resharper disable all

namespace Necrofancy.PrepareProcedurally.Patches;

/// <summary>
/// Make sure that ProcGen and its dependencies are in a good state based on some of the following:
/// <para>* Starting Scenario (and anything from HAR races specifically)</para>
/// <para>* Starting Map Tile</para>
/// <para>* Ideologion</para>
/// All of these elements will determine starting default factors, so they'll need to be initialized when
/// the <see cref="Page_ConfigureStartingPawns"/> is opened for the first time.
/// </summary>
[HarmonyPatch(typeof(Page_ConfigureStartingPawns), nameof(Page_ConfigureStartingPawns.PostOpen))]
public class InitializeStateOnStartingPawnsOpen 
{
    [HarmonyPostfix]
    public static void PostOpen(Page_ConfigureStartingPawns __instance) 
    {
        Editor.SetCleanState();
    }
}