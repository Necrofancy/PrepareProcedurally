using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Necrofancy.PrepareProcedurally.Interface.Dialogs;

public enum UsabilityRequirement
{
    CanBeOff,
    Usable,
    Minor,
    Major
}

public class EditSpecificPawn : Window
{
    // DrawCharacterCard with a defined Randomize button -must- be this size or bad things happen.
    private const float CardSizeX = 837.5f;
    private const float CardSizeY = 520;
    private const float WindowMargin = 34f;
    private const float YPadding = 30f;

    private readonly int pawnIndex;
    
    private const float BackstoryX = 83f;
    private const float BackstoryY = 131f;

    private bool keepAesthetics = false;
    private bool showHeadgear = true;
    private bool showApparel = true;

#if UI_DEBUGGING
    private static int _debugX = BackstoryX;
    private static string _debugXString = BackstoryX.ToString();
    private static int _debugY = BackstoryY;
    private static string _debugYString = BackstoryY.ToString();
#endif
    public override Vector2 InitialSize => new(CardSizeX + WindowMargin, CardSizeY + WindowMargin + YPadding);

    public EditSpecificPawn(Pawn pawn)
    {
        pawnIndex = StartingPawnUtility.PawnIndex(pawn);
        SelectedPawn.Select(pawn);
        doCloseX = true;
    }
    public override void DoWindowContents(Rect rect)
    {
        if (SelectedPawn.Pawn is null || SelectedPawn.Pawn.Destroyed || SelectedPawn.Pawn.Discarded)
        {
            Close(doCloseSound:false);
            return;
        }

        Text.Font = GameFont.Medium;
        var titleRect = new Rect(rect.x, rect.y, rect.width, YPadding);
        var label = "EditSpecificPawnTitle".PpTranslate();
        Widgets.Label(titleRect, label);
        Text.Font = GameFont.Small;
        
        DrawAestheticOptions(rect);
        
#if UI_DEBUGGING
        var debugRect = rect with { x = rect.x + 300, width = 150};
        Widgets.IntEntry(debugRect, ref _debugX, ref _debugXString);
        debugRect = debugRect with { x = debugRect.xMax + 10 };
        Widgets.IntEntry(debugRect, ref _debugY, ref _debugYString);
#endif
        rect.y += YPadding;
        rect.height -= YPadding;
        ReusingVanillaUi.DrawPortraitArea(rect, pawnIndex, showApparel, showHeadgear);
        SelectedPawn.KeepCosmetics = keepAesthetics;
        
        if (PrepareMod.Settings.showCompatibility)
        {
            var width = 60f * (Editor.StartingPawns.Length - 1);
            var height = 75f;
            var xStart = (rect.xMax - rect.xMin - width) / 2;
            var relationShipsRect = new Rect(xStart, rect.yMax - height, width, height);
            ShowRelationships(relationShipsRect);
        }
        
        if (PrepareMod.Settings.AlienRacesEnabled)
        {
            return;
        }
        
#if UI_DEBUGGING
        rect.x += _debugX;
        rect.y += _debugY;
#else
        rect.x += BackstoryX;
        rect.y += BackstoryY;
#endif
        rect.width = 22f;
        rect.height = 22f;
        
        DrawBackstoryRow(rect, BackstorySlot.Childhood);
        rect.y += 26f; // additional 4px padding added in DoLeftSection
        DrawBackstoryRow(rect, BackstorySlot.Adulthood);
    }
    
    private void DrawAestheticOptions(Rect rect)
    {
        const float checkboxSpace = 30f;
        const float margin = 10f;
        var aesthetics =  "KeepAesthetics".PpTranslate();
        var aestheticsLength = Text.CalcSize(aesthetics).x + checkboxSpace;
        var tooltip = "KeepAestheticsTooltip".PpTranslate();
        if (!ModsConfig.IdeologyActive)
        {
            Rect soloRect = new Rect(rect.xMax - aestheticsLength, rect.y, aestheticsLength, Text.LineHeight);
            Widgets.CheckboxLabeled(soloRect, aesthetics, ref keepAesthetics);
            TooltipHandler.TipRegion(soloRect, tooltip);
            return;
        }
        
        var head = "ShowHeadgear".Translate();
        var headLength = Text.CalcSize(head).x + checkboxSpace;
        var apparel = "ShowApparel".Translate();
        var apparelLength = Text.CalcSize(apparel).x + checkboxSpace;
        
        var totalWidth = aestheticsLength + headLength + apparelLength + margin * 3;
        Rect align = new Rect(rect.xMax - totalWidth, rect.y, aestheticsLength, Text.LineHeight);
        Widgets.CheckboxLabeled(align, aesthetics, ref keepAesthetics);
        TooltipHandler.TipRegion(align, tooltip);
        align.x += aestheticsLength + margin;
        align.width = headLength;
        Widgets.CheckboxLabeled(align, head, ref showHeadgear);
        align.x += headLength + margin;
        align.width = apparelLength;
        Widgets.CheckboxLabeled(align, apparel, ref showApparel);
    }
    
    private void DrawBackstoryRow(Rect buttonRect, BackstorySlot slot)
    {
        var locked = slot switch
        {
            BackstorySlot.Childhood => Editor.SetChildhoods[SelectedPawn.Index] is not null,
            _ => Editor.SetAdulthoods[SelectedPawn.Index] is not null
        };
        var button = locked ? LazyTexture.Locked : LazyTexture.Unlocked;
        BackstorySelection.ForSelectedPawn(slot, buttonRect);
        Widgets.ButtonImage(buttonRect, button.Value);
        TooltipHandler.TipRegion(buttonRect, "ClickToSelect".PpTranslate());
    }
    
    private void ShowRelationships(Rect areaRect)
    {
        var otherPawns = Editor.StartingPawns.Except(SelectedPawn.Pawn).ToList();
        var width = areaRect.width / otherPawns.Count;
        foreach (var otherPawn in otherPawns)
        {
            areaRect.SplitVertically(width, out var left, out var remainder);

            var topHeight = areaRect.height - Text.LineHeight;

            left.SplitHorizontally(topHeight, out _, out var textRect);

            DrawPawn(otherPawn, left);

            var anchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(textRect, SelectedPawn.Pawn.relations.CompatibilityWith(otherPawn).ToString("F"));
            Text.Anchor = anchor;
            
            TooltipHandler.TipRegion(left, otherPawn.LabelCap);
            
            areaRect = remainder;
        }
    }

    private void DrawPawn(Pawn otherPawn, Rect portraitRect)
    {
        portraitRect = portraitRect.ContractedBy(2f);
        var tex = PortraitsCache.Get(otherPawn, portraitRect.size, Rot4.South);
        Widgets.DrawTextureFitted(portraitRect, tex, 1f);
    }
}