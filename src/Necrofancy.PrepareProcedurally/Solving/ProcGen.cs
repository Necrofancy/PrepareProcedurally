using RimWorld;
using Verse;

using static Necrofancy.PrepareProcedurally.Editor;

namespace Necrofancy.PrepareProcedurally.Solving
{
    public static class ProcGen
    {
        public static void Generate(BalancingSituation situation)
        {
            Compatibility.Layer.RandomizeForTeam(situation);

            Editor.Dirty = false;
        }

        public static void OnPawnChanged(Pawn pawn)
        {
            foreach (var window in Find.WindowStack.Windows)
            {
                switch (window)
                {
                    case Page_ConfigureStartingPawns startingPage:
                        startingPage.SelectPawn(pawn);
                        break;
                    case Interface.Pages.PrepareProcedurally _:
                        var index = StartingPawnUtility.PawnIndex(pawn);
                        StartingPawns[index] = pawn;
                        break;
                }
            }
        }

        public static void CleanUpOnError()
        {
            LockedPawns.Clear();
            var startingPawns = Find.GameInitData.startingAndOptionalPawns;
            for (var i = 0; i < startingPawns.Count; i++)
            {
                startingPawns[i] = StartingPawnUtility.RandomizeInPlace(startingPawns[i]);
                OnPawnChanged(startingPawns[i]);
            }
        }
    }
}