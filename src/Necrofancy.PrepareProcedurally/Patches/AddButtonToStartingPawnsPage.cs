using System;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Necrofancy.PrepareProcedurally.Patches 
{
    // ReSharper disable once UnusedType.Global
    [HarmonyPatch(typeof(Page_ConfigureStartingPawns), nameof(Page_ConfigureStartingPawns.DoWindowContents))]
    public class AddButtonToStartingPawnsPage 
    {
        private const string ButtonText = "Prepare Procedurally";
        private const int ButtonY = -50;

        private static readonly Vector2 ButtonDimensions = new Vector2(150, 38);

        [HarmonyPostfix]
        // ReSharper disable once InconsistentNaming - necessary for Harmony patch
        public static void Postfix(Rect rect, Page_ConfigureStartingPawns __instance) 
        {
            if (Widgets.ButtonText(new Rect((rect.x + rect.width) / 2 - ButtonDimensions.x / 2, rect.y + ButtonY,
                    ButtonDimensions.x, ButtonDimensions.y), ButtonText)) {
                try
                {
                    var gen = new Interface.Pages.PrepareProcedurally();
                    Find.WindowStack.Add(gen);
                } 
                catch (Exception e) 
                {
                    Log.Error(e.ToString());
                }
            }
        }
    }
}