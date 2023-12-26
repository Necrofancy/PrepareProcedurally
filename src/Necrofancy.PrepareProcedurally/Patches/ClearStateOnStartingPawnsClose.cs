using HarmonyLib;
using Necrofancy.PrepareProcedurally.Solving;
using RimWorld;
using Verse;

// Resharper disable all

namespace Necrofancy.PrepareProcedurally.Patches
{
    /// <summary>
    /// After the <see cref="Page_ConfigureStartingPawns"/> closes, there should be no references to pawns in
    /// <see cref="ProcGen"/> that may or may not exist anymore so they can be garbage-collected. As well, any UI
    /// or dialogs should make absolute sure they are closed.
    /// </summary>
    // Window.DoNext is not public, so it has to be referenced explicitly
    [HarmonyPatch(typeof(Page_ConfigureStartingPawns), "DoNext")]
    public class ClearStateOnStartingPawnsClose
    {
        [HarmonyPostfix]
        public static void DoNext() 
        {
            ProcGen.ClearState();
            
            while (Find.WindowStack.WindowOfType<Interface.Dialogs.EditSpecificPawn>() is { } dialog)
            {
                dialog.Close(doCloseSound:false);
            }
            
            while (Find.WindowStack.WindowOfType<Interface.Pages.PrepareProcedurally>() is { } page)
            {
                page.Close(doCloseSound:false);
            }
        }
    }
}