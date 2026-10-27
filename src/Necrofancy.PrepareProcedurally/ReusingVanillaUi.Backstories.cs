using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using HarmonyLib;
using Necrofancy.PrepareProcedurally.Interface;
using RimWorld;
using UnityEngine;
using Verse;

#pragma warning disable CS8321 // Local function is declared but never used

// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedParameter.Global

namespace Necrofancy.PrepareProcedurally;

public static partial class ReusingVanillaUi
{
#if RW1_5_OR_GREATER
    private const string DoLeftSectionName = "DoLeftSection";
    private const int IconPadding = 3;
    private const int IconSize = 16;

    private static MethodInfo backstoryDrawer;
    private static bool backstoryRowsHooked;
    private static bool backstoryDrawerSwapped;
    private static bool leftSectionPatched;
    private static bool leftSectionSwapped;
    
    private static BackstorySlot? pendingBackstorySlot;
    private static BackstoryDef pendingBackstory;
    private static Rect? buttonSelectionForBackstories;
    
    internal static bool BackstoryButtonsInCard => leftSectionPatched && leftSectionSwapped;

    private static MethodInfo GetBackstoryMethod =>
        AccessTools.Method(typeof(Pawn_StoryTracker), nameof(Pawn_StoryTracker.GetBackstory), [typeof(BackstorySlot)]);
    
    internal static void CreateBackstoryButtonPatches(Harmony harmony)
    {
        try
        {
            var vanillaLeftSection = AccessTools.Method(typeof(CharacterCardUtility), DoLeftSectionName);
            if (vanillaLeftSection is null
                || !ParametersMatch(vanillaLeftSection, typeof(Rect), typeof(Rect), typeof(Pawn)))
            {
                Logging.Warn($"Could not find CharacterCardUtility.{DoLeftSectionName}(Rect, Rect, Pawn); backstory buttons will use the overlay instead.");
                return;
            }

            var getBackstory = GetBackstoryMethod;
            if (getBackstory is null)
            {
                Logging.Warn("Could not find Pawn_StoryTracker.GetBackstory(BackstorySlot); backstory buttons will use the overlay instead.");
                return;
            }

            var leftSectionInstructions = PatchProcessor.GetOriginalInstructions(vanillaLeftSection);
            var drawsRowsDirectly = leftSectionInstructions.Any(instruction => instruction.Calls(getBackstory));

            if (!drawsRowsDirectly)
            {
                backstoryDrawer = leftSectionInstructions
                    .Where(instruction => instruction.opcode == OpCodes.Ldftn)
                    .Select(instruction => instruction.operand as MethodInfo)
                    .FirstOrDefault(lambda => IsLambdaCallingMethod(lambda, getBackstory));

                if (backstoryDrawer is null
                    || backstoryDrawer.IsStatic
                    || backstoryDrawer.ReturnType != typeof(void)
                    || !ParametersMatch(backstoryDrawer, typeof(Rect)))
                {
                    Logging.Warn($"Could not find the backstory section drawer in {DoLeftSectionName}; backstory buttons will use the overlay instead.");
                    return;
                }

                Logging.Debug($"Reverse patching {backstoryDrawer.DeclaringType?.Name}.{backstoryDrawer.Name} into {nameof(ReusingVanillaUi)}.{nameof(DrawBackstorySection)}");
                var drawerStub = AccessTools.Method(typeof(ReusingVanillaUi), nameof(DrawBackstorySection));
                harmony.CreateReversePatcher(backstoryDrawer, new HarmonyMethod(drawerStub)).Patch();
            }

            Logging.Debug($"Reverse patching {nameof(CharacterCardUtility)}.{DoLeftSectionName} into {nameof(ReusingVanillaUi)}.{nameof(DoLeftSection)}");
            var leftSectionStub = AccessTools.Method(typeof(ReusingVanillaUi), nameof(DoLeftSection));
            harmony.CreateReversePatcher(vanillaLeftSection, new HarmonyMethod(leftSectionStub)).Patch();

            leftSectionPatched = backstoryRowsHooked && (drawsRowsDirectly || backstoryDrawerSwapped);
            if (!leftSectionPatched)
            {
                Logging.Warn("Could not hook the backstory rows of the character card; backstory buttons will use the overlay instead.");
            }
        }
        catch (Exception e)
        {
            leftSectionPatched = false;
            Logging.Warn($"Failed to add backstory buttons to the character card, using the overlay instead: {e}");
        }
    }
    
    internal static void DoLeftSection(Rect rect, Rect leftRect, Pawn pawn)
    {
        IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            StartTranspile();
            var codes = instructions.ToList();

            if (backstoryDrawer is null)
            {
                // This version draws the rows in DoLeftSection itself.
                HookBackstoryRows(codes);
            }
            else
            {
                var hookedDrawer = AccessMethod(typeof(ReusingVanillaUi), nameof(DrawBackstorySection));
                for (var i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode != OpCodes.Ldftn || !Equals(codes[i].operand, backstoryDrawer))
                        continue;

                    if (!IsUncachedRectDelegate(codes, i))
                    {
                        Logging.Warn($"Backstory drawer delegate in {DoLeftSectionName} is not built the expected way; leaving it alone.");
                        continue;
                    }

                    // The closure instance already on the stack becomes the first argument of the static copy,
                    // the same way a delegate over an extension method is built.
                    Logging.Debug("Swapping backstory section drawer for hooked copy");
                    codes[i].operand = hookedDrawer;
                    backstoryDrawerSwapped = true;
                }
            }

            EndTranspile();
            return codes;
        }
    }
    
    internal static void DrawBackstorySection(object closure, Rect sectionRect)
    {
        IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            StartTranspile();
            var codes = instructions.ToList();
            HookBackstoryRows(codes);
            EndTranspile();
            return codes;
        }
    }
    
    private static void HookBackstoryRows(List<CodeInstruction> codes)
    {
        var getBackstory = GetBackstoryMethod;
        var track = AccessMethod(typeof(ReusingVanillaUi), nameof(TrackBackstory));
        var tooltipHooks = Replacements();
        var drawButton = AccessMethod(typeof(ReusingVanillaUi), nameof(DrawButtonInstead));
        var tracked = false;
        var buttonDrawn = false;

        foreach (var instruction in codes)
        {
            if (instruction.Calls(getBackstory))
            {
                instruction.opcode = OpCodes.Call;
                instruction.operand = track;
                tracked = true;
            }
            else if (instruction.opcode == OpCodes.Call
                && instruction.operand is MethodInfo method
                && tooltipHooks.TryGetValue(method, out var replacement))
            {
                Logging.Debug($"Backstory: Call {method.FullDescription()} -> {replacement.FullDescription()}");
                instruction.operand = replacement;
                buttonDrawn |= replacement == drawButton;
            }
        }

        backstoryRowsHooked = tracked && buttonDrawn;
    }
    
    private static Dictionary<MethodInfo, MethodInfo> Replacements()
    {
        var hooks = new Dictionary<MethodInfo, MethodInfo>();
        void Add(Type type, string name, Type[] parameters, string hookName)
        {
            var original = AccessTools.Method(type, name, parameters);
            if (original is null)
                Logging.Warn($"Could not find original method {type}.{name}({string.Join(", ", parameters.Select(p => p.Name))})");
            var hook = AccessTools.Method(typeof(ReusingVanillaUi), hookName, parameters);
            if (hook is null)
                Logging.Warn($"Could not find replacement {hookName}({string.Join(", ", parameters.Select(p => p.Name))})");

            if (original is not null && hook is not null)
                hooks[original] = hook;
        }
        
        Add(typeof(GUI), nameof(GUI.DrawTexture), [typeof(Rect), typeof(Texture)], nameof(DrawButtonInstead));
        Add(typeof(Widgets), nameof(Widgets.DrawHighlight), [typeof(Rect)], nameof(HighlightUnlessButton));

        // tooltip needs Mouse.IsOver as well as tooltip signal overwritten as it checks twice
        Add(typeof(Mouse), nameof(Mouse.IsOver), [typeof(Rect)], nameof(IsOverRow));
        Add(typeof(TooltipHandler), nameof(TooltipHandler.TipRegion), [typeof(Rect), typeof(Func<string>), typeof(int)], nameof(TipWithTextGetter));
        Add(typeof(TooltipHandler), nameof(TooltipHandler.TipRegion), [typeof(Rect), typeof(TipSignal)], nameof(TipWithTipSignal));
        
        return hooks;
    }
    
    private static BackstoryDef TrackBackstory(Pawn_StoryTracker story, BackstorySlot slot)
    {
        var backstory = story.GetBackstory(slot);
        pendingBackstorySlot = slot;
        pendingBackstory = backstory;
        return backstory;
    }

    private static void HighlightUnlessButton(Rect rect)
    {
        if (rect != buttonSelectionForBackstories)
            Widgets.DrawHighlight(rect);
    }
    
    private static void DrawButtonInstead(Rect rect, Texture texture)
    {
        if (!DrawBackstoryButton(rect))
        {
            GUI.DrawTexture(rect, texture);
        }
    }
    
    private static bool IsOverRow(Rect rect)
    {
        return Mouse.IsOver(ButtonRectFor(rect));
    }

    private static void TipWithTipSignal(Rect rect, TipSignal tip)
    {
        TooltipHandler.TipRegion(ButtonRectFor(rect), tip);
    }

    private static void TipWithTextGetter(Rect rect, Func<string> textGetter, int uniqueId)
    {
        TooltipHandler.TipRegion(ButtonRectFor(rect), textGetter, uniqueId);
    }
    
    private static Rect ButtonRectFor(Rect rect)
    {
        return rect == buttonSelectionForBackstories ? GetEnlargedButtonRect(rect) : rect;
    }
    
    private static bool DrawBackstoryButton(Rect boxRect)
    {
        if (pendingBackstorySlot is not { } slot || pendingBackstory is not { } backstory)
            return false;
        
        pendingBackstorySlot = null;
        pendingBackstory = null;

        if (PrepareMod.Settings.AlienRacesEnabled || SelectedPawn.Pawn is null)
            return false;

        buttonSelectionForBackstories = boxRect;

        var index = SelectedPawn.Index;
        var locked = slot == BackstorySlot.Childhood
            ? Editor.SetChildhoods[index] is not null
            : Editor.SetAdulthoods[index] is not null;

        var buttonRect = GetEnlargedButtonRect(boxRect);

        var color = GUI.color;
        var anchor = Text.Anchor;
        GUI.color = Color.white;

        var clicked = Widgets.ButtonText(buttonRect, string.Empty);

        Text.Anchor = TextAnchor.MiddleCenter;
        var title = backstory.TitleCapFor(SelectedPawn.Pawn.gender);
        Widgets.Label(boxRect, title.Truncate(boxRect.width));

        if (locked)
        {
            var iconRect = new Rect(buttonRect.x + IconPadding, boxRect.y + IconPadding, IconSize, IconSize);
            GUI.DrawTexture(iconRect, LazyTexture.Locked.Value);
        }

        Text.Anchor = anchor;
        GUI.color = color;

        TooltipHandler.TipRegion(buttonRect, "ClickToSelect".PpTranslate());
        
        if (clicked)
            BackstorySelection.OpenForSelectedPawn(slot);

        return true;
    }

    private static Rect GetEnlargedButtonRect(Rect boxRect)
    {
        var buttonWithIconPosition = boxRect.position with { x = boxRect.xMin - IconSize - IconPadding * 2 };
        return boxRect with { width = 180f, position = buttonWithIconPosition };
    }

    private static bool IsUncachedRectDelegate(List<CodeInstruction> codes, int ldftnIndex)
    {
        if (ldftnIndex + 1 >= codes.Count)
            return false;

        if (codes[ldftnIndex + 1].opcode != OpCodes.Newobj
            || codes[ldftnIndex + 1].operand is not ConstructorInfo { DeclaringType: { } delegateType }
            || !typeof(Delegate).IsAssignableFrom(delegateType))
            return false;

        var invoke = delegateType.GetMethod("Invoke");
        if (invoke is null || invoke.ReturnType != typeof(void) || !ParametersMatch(invoke, typeof(Rect)))
            return false;

        var cachedStatically = ldftnIndex + 3 < codes.Count
                               && codes[ldftnIndex + 2].opcode == OpCodes.Dup
                               && codes[ldftnIndex + 3].opcode == OpCodes.Stsfld;
        return !cachedStatically;
    }

    private static bool ParametersMatch(MethodBase method, params Type[] expected)
    {
        var parameters = method.GetParameters();
        if (parameters.Length != expected.Length)
            return false;

        for (var i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].ParameterType != expected[i])
                return false;
        }

        return true;
    }
#else
    internal static bool BackstoryButtonsInCard => false;
#endif
}