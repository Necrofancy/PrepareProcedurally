using System;
using HarmonyLib;
using Necrofancy.PrepareProcedurally.Editor;
using RimWorld;
using UnityEngine;
using Verse;

namespace Necrofancy.PrepareProcedurally.Patches 
{
    // ReSharper disable once UnusedType.Global
    [HarmonyPatch(typeof(Page_ConfigureStartingPawns), nameof(Page_ConfigureStartingPawns.DoWindowContents))]
    public class AddButtonToStartingPawnsPage 
    {
        private const string buttonText = "Prepare Procedurally";
        private const int buttonY = -50;

        private static Vector2 buttonDimensions = new Vector2(150, 38);

        [HarmonyPostfix]
        public static void Postfix(Rect rect, Page_ConfigureStartingPawns __instance) 
        {
            if (Widgets.ButtonText(new Rect((rect.x + rect.width) / 2 - buttonDimensions.x / 2, rect.y + buttonY,
                    buttonDimensions.x, buttonDimensions.y), buttonText)) {
                try
                {
                    var gen = new Interface.Pages.PrepareProcedurally();
                    Find.WindowStack.Add(gen);
                } catch (Exception e) 
                {
                    Log.Error(e.ToString());
                }
            }
        }
    }
}