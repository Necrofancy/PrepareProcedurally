using System;
using UnityEngine;
using Verse;
using static Necrofancy.PrepareProcedurally.PrepareMod;

namespace Necrofancy.PrepareProcedurally.Interface.Dialogs;

public class PrepareSettingsDialog : Window
{
    public override Vector2 InitialSize => new(900f, 600f);

    public PrepareSettingsDialog()
    {
        doCloseX = true;
        doCloseButton = true;
        closeOnClickedOutside = true;
        absorbInputAroundWindow = true;
        forcePause = true;
    }

    public override void DoWindowContents(Rect inRect)
    {
        DrawModSettings(inRect);
    }

    public override void PreClose()
    {
        base.PreClose();
        PrepareMod.Settings.Write();
        Editor.AllowDirtying = PrepareMod.Settings.autoGenerate;
    }

    internal static void DrawModSettings(Rect rect)
    {
        rect.SplitHorizontally(50, out var preambleRect, out rect);
        Widgets.Label(preambleRect, "SettingsDescription".PpTranslate());

        var leftWidth = 400;
        rect.SplitVertically(leftWidth, out var textRect, out var otherSettings);

        textRect = textRect.ContractedBy(20);
        otherSettings = otherSettings.ContractedBy(20);

        SettingsUiUtility.ForSettings.DoWindowContents(textRect);


        DrawOtherSettings(otherSettings);
    }

    private static void DrawOtherSettings(Rect rect)
    {
        var rowHeight = SettingsUiUtility.RowHeight;
        float intValue;
        FloatRange allowedRange;

        Widgets.Label(rect, "WhenTeamRandomizationIsCalled".PpTranslate());
        rect.y += rowHeight;
        var menuSection = new Rect(rect.x, rect.y, rect.width, rowHeight * 6);
        Widgets.DrawMenuSection(menuSection);
        var inner = menuSection.GetInnerRect();

        // Allow generation at start of PrepareProcedurally window
        var generateOnWindow = new Rect(inner.x, inner.y, inner.width, rowHeight);
        Widgets.CheckboxLabeled(generateOnWindow, "AutoGenerateAtStart".PpTranslate(), ref Settings.preGenerate);
        TooltipHandler.TipRegion(generateOnWindow, "AutoGenerateAtStartTooltip".PpTranslate());

        // Allow generation on update for window
        var generateOnChange = new Rect(inner.x, inner.y + rowHeight, inner.width, rowHeight);
        Widgets.CheckboxLabeled(generateOnChange, "AutoGenerateOnChange".PpTranslate(), ref Settings.autoGenerate);
        TooltipHandler.TipRegion(generateOnChange, "AutoGenerateOnChangeTooltip".PpTranslate());

        // configure debounce for auto-generation
        if (Settings.autoGenerate)
        {
            var debounceRect = new Rect(inner.x, inner.y + rowHeight * 3, inner.width, rowHeight);
            intValue = Settings.debounceDelay;
            allowedRange = new FloatRange(0, 1000);
            Widgets.HorizontalSlider(debounceRect, ref intValue, allowedRange,
                label: "DebounceAutoUpdate".PpTranslate());
            TooltipHandler.TipRegion(debounceRect, "DebounceAutoUpdateTooltip".PpTranslate());
            Settings.debounceDelay = (int)intValue;
        }

        rect.y += rowHeight * 6.5f;

        Widgets.Label(rect, "StorySelection".PpTranslate());
        rect.y += rowHeight;
        menuSection = new Rect(rect.x, rect.y, rect.width, rowHeight * 5.5f);
        Widgets.DrawMenuSection(menuSection);
        inner = menuSection.GetInnerRect();

        var candidatePoolSizeRect = new Rect(inner.x, inner.y, inner.width, rowHeight);
        intValue = Settings.candidatePoolSize;
        allowedRange = new FloatRange(1, 100);
        Widgets.HorizontalSlider(candidatePoolSizeRect, ref intValue, allowedRange,
            label: "SelectFromTopXBackgrounds".PpTranslate());
        TooltipHandler.TipRegion(candidatePoolSizeRect, "SelectFromTopXBackgroundsToolTip".PpTranslate());
        Settings.candidatePoolSize = (int)Mathf.Round(intValue);

        var minimumRatioOfBest = new Rect(inner.x, inner.y + rowHeight * 3, inner.width, rowHeight);
        Widgets.HorizontalSlider(minimumRatioOfBest, ref Settings.minimumRatioOfBest, new FloatRange(0.1f, 1.0f),
            label: "MinimumRatioOfBest".PpTranslate());
        TooltipHandler.TipRegion(minimumRatioOfBest, "MinimumRatioOfBestTooltip".PpTranslate());

        rect.y += rowHeight * 6f;

        Widgets.Label(rect, "HiddenMechanics".PpTranslate());
        rect.y += rowHeight;
        menuSection = new Rect(rect.x, rect.y, rect.width, rowHeight * 3);
        Widgets.DrawMenuSection(menuSection);
        inner = menuSection.GetInnerRect();

        // Display relationships in the window
        var showCompatibility = new Rect(inner.x, inner.y, inner.width, rowHeight);
        Widgets.CheckboxLabeled(showCompatibility, "DisplayCompatibility".PpTranslate(),
            ref Settings.showCompatibility);
        TooltipHandler.TipRegion(showCompatibility, "DisplayCompatibilityTooltip".PpTranslate());
    }
}
