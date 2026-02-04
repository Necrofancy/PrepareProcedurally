using System;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

// ReSharper disable UnusedParameter.Local
// ReSharper disable InconsistentNaming

namespace Necrofancy.PrepareProcedurally;

[StaticConstructorOnStartup, UsedImplicitly]
public class HarmonyPatches
{
    static HarmonyPatches()
    {
        var harmony = new Harmony("Necrofancy.PrepareProcedurally");
        #if DEBUG
        Harmony.DEBUG = true;
        #endif
        var startingDialog = typeof(Page_ConfigureStartingPawns);
        
        // The mod needs to set up starting state based on ideology, starting map tile, and general scenario.
        SetEditorStateOnOpeningCreateCharactersDialog(startingDialog, harmony);
        // The only effects happen on interacting with mod-added UI. There are no external changes otherwise.
        // Add a button to open said UIs when creating characters.
        AddButtonToCreateCharactersDialog(startingDialog, harmony);
        // Clear editor state and make sure any dialogs are closed to further ensure no state changes happen mid-game.
        ClearEditorStateOnProceedingFromCreateCharactersDialog(startingDialog, harmony);
        CreateReversePatchesOfVanillaUi(harmony);
        
        var assembly = Assembly.GetExecutingAssembly();
        Logging.Info($"release v{assembly.GetName().Version} patches loaded");
    }

    private static void SetEditorStateOnOpeningCreateCharactersDialog(Type startingDialog, Harmony harmony)
    {
        var postOpen = nameof(Page_ConfigureStartingPawns.PostOpen);
        var postOpenMethod = AccessTools.Method(startingDialog, postOpen);
        var setStateMethod = AccessTools.Method(typeof(HarmonyPatches), nameof(InitializeEditorState));
        var setState = new HarmonyMethod(setStateMethod);
        harmony.Patch(postOpenMethod, postfix: setState);
    }

    private static void AddButtonToCreateCharactersDialog(Type startingDialog, Harmony harmony)
    {
        var doWindowContents = nameof(Page_ConfigureStartingPawns.DoWindowContents);
        var onWindowUpdating = AccessTools.Method(startingDialog, doWindowContents);
        var addButton = AccessTools.Method(typeof(HarmonyPatches), nameof(AddButtonToDialog));
        var addButtonPatch = new HarmonyMethod(addButton);
        harmony.Patch(onWindowUpdating, postfix: addButtonPatch);
    }

    private static void ClearEditorStateOnProceedingFromCreateCharactersDialog(Type startingDialog, Harmony harmony)
    {
        const string doNext = "DoNext"; // not publicly available.
        var doNextMethod = AccessTools.Method(startingDialog, doNext);
        var clearStateMethod = AccessTools.Method(typeof(HarmonyPatches), nameof(ClearStateAndCloseWindows));
        var clearState = new HarmonyMethod(clearStateMethod);
        harmony.Patch(doNextMethod, postfix: clearState);
    }
    
    private static void CreateReversePatchesOfVanillaUi(Harmony harmony)
    {
        var baseType = typeof(ReusingVanillaUi);
        void ReversePatch(Type rimworldClass, string functionName, Type[] parameters = null)
        {
            Logging.Debug($"Reverse patching {rimworldClass.Name}.{functionName} into {baseType.Name}.{functionName}");
            var source = AccessTools.Method(rimworldClass, functionName, parameters);
            var destination = AccessTools.Method(baseType, functionName, parameters);
            var method = new HarmonyMethod(destination);
            harmony.CreateReversePatcher(source, method).Patch();
            Logging.Debug($"End reverse patching.");
        }

        // transpilers are colocated within the ReusingVanillaUi methods.
        // there's no way to directly locate them here and have Harmony resolve them.
        ReversePatch(typeof(SkillUI), nameof(ReusingVanillaUi.DrawSkillsOf));
        ReversePatch(typeof(CharacterCardUtility), nameof(CharacterCardUtility.DrawCharacterCard));
        ReversePatch(typeof(StartingPawnUtility), nameof(StartingPawnUtility.DrawPortraitArea));
    }

    private static void InitializeEditorState()
    {
        Editor.SetCleanState();
    }

    private static void AddButtonToDialog(Rect rect, Page_ConfigureStartingPawns __instance)
    {
        var horizontalPlacement = (rect.x + rect.width) / 2 - 75F;
        var verticalPlacement = 0;
        var width = 150;
        var height = 38;
        if (ModsConfig.IsActive("lakuna.preparemoderately"))
        {
            horizontalPlacement += 175;
        }
        
        string buttonText = "Necrofancy.PrepareProcedurally.Button".Translate();
        var buttonRect = new Rect
        {
            x = horizontalPlacement,
            y = verticalPlacement,
            width = width,
            height = height
        };

        if (Widgets.ButtonText(buttonRect, buttonText))
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
    
    public static void ClearStateAndCloseWindows()
    {
        Editor.ClearState();

        while (Find.WindowStack.WindowOfType<Interface.Dialogs.EditSpecificPawn>() is { } dialog) dialog.Close(false);

        while (Find.WindowStack.WindowOfType<Interface.Pages.PrepareProcedurally>() is { } page) page.Close(false);
    }
}