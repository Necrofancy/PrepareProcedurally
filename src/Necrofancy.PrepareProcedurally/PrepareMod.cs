using Necrofancy.PrepareProcedurally.Interface;
using UnityEngine;
using Verse;

namespace Necrofancy.PrepareProcedurally;

public class PrepareMod : Mod
{
    internal static PrepareModSettings Settings;
    public PrepareMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<PrepareModSettings>();
    }
    public override void DoSettingsWindowContents(Rect rect)
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

    private void DrawOtherSettings(Rect rect)
    {
        var rowHeight = SettingsUiUtility.RowHeight;
        
        Widgets.Label(rect, "WhenTeamRandomizationIsCalled".PpTranslate());
        rect.y += rowHeight;
        var menuSection = new Rect(rect.x, rect.y, rect.width, rowHeight * 4);
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
        
        rect.y += rowHeight * 5;
        
        Widgets.Label(rect, "HiddenMechanics".PpTranslate());
        rect.y += rowHeight;
        menuSection = new Rect(rect.x, rect.y, rect.width, rowHeight * 3);
        Widgets.DrawMenuSection(menuSection);
        inner = menuSection.GetInnerRect();
        
        // Display relationships in the window
        var showCompatibility = new Rect(inner.x, inner.y, inner.width, rowHeight);
        Widgets.CheckboxLabeled(showCompatibility, "DisplayCompatibility".PpTranslate(), ref Settings.showCompatibility);
        TooltipHandler.TipRegion(showCompatibility, "DisplayCompatibilityTooltip".PpTranslate());
    }

    public override string SettingsCategory()
    {
        return "Button".PpTranslate();
    }
}