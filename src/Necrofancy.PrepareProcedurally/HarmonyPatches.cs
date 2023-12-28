using System;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

namespace Necrofancy.PrepareProcedurally;

[StaticConstructorOnStartup, UsedImplicitly]
public class HarmonyPatches 
{    
    private const string ButtonText = "Prepare Procedurally";
    
    static HarmonyPatches()
    {
        var harmony = new Harmony("Necrofancy.PrepareProcedurally");

        Type startingDialog = typeof(Page_ConfigureStartingPawns);
        
        // The mod needs to set up starting state based on ideology, starting map tile, and general scenario.
        SetEditorStateOnOpeningCreateCharactersDialog(startingDialog, harmony);
        // The only effects happen on interacting with mod-added UI. There are no external changes otherwise.
        // Add a button to open said UIs when creating characters.
        AddButtonToCreateCharactersDialog(startingDialog, harmony);
        // Clear editor state and make sure any dialogs are closed to further ensure no state changes happen mid-game.
        ClearEditorStateOnProceedingFromCreateCharactersDialog(startingDialog, harmony);
    }
    
    private static void SetEditorStateOnOpeningCreateCharactersDialog(Type startingDialog, Harmony harmony)
    {
        string postOpen = nameof(Page_ConfigureStartingPawns.PostOpen);
        MethodInfo postOpenMethod = AccessTools.Method(startingDialog, postOpen);
        MethodInfo setStateMethod = AccessTools.Method(typeof(HarmonyPatches), nameof(InitializeEditorState));
        HarmonyMethod setState = new HarmonyMethod(setStateMethod);
        harmony.Patch(postOpenMethod, postfix: setState);
    }
    
    private static void AddButtonToCreateCharactersDialog(Type startingDialog, Harmony harmony)
    {
        string doWindowContents = nameof(Page_ConfigureStartingPawns.DoWindowContents);
        MethodInfo onWindowUpdating = AccessTools.Method(startingDialog, doWindowContents);
        MethodInfo addButton = AccessTools.Method(typeof(HarmonyPatches), nameof(AddButtonToDialog));
        HarmonyMethod addButtonPatch = new HarmonyMethod(addButton);
        harmony.Patch(onWindowUpdating, postfix: addButtonPatch);
    }
    
    private static void ClearEditorStateOnProceedingFromCreateCharactersDialog(Type startingDialog, Harmony harmony)
    {
        const string doNext = "DoNext"; // not publicly available.
        MethodInfo doNextMethod = AccessTools.Method(startingDialog, doNext);
        MethodInfo clearStateMethod = AccessTools.Method(typeof(HarmonyPatches), nameof(ClearStateAndCloseWindows));
        HarmonyMethod clearState = new HarmonyMethod(clearStateMethod);
        harmony.Patch(doNextMethod, postfix: clearState);
    }

    private static void InitializeEditorState()
    {
        Editor.SetCleanState();
    }
    
    private static void AddButtonToDialog(Rect rect, Page_ConfigureStartingPawns __instance) 
    {
        var buttonRect = new Rect
        {
            x = (rect.x + rect.width) / 2 - 75F,
            y = rect.y - 50,
            width = 150,
            height = 38
        };
        
        if (Widgets.ButtonText(buttonRect, ButtonText)) 
        {
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
    
    public static void ClearStateAndCloseWindows() 
    {
        Editor.ClearState();
            
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