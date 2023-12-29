using System;
using System.Collections.Generic;
using Necrofancy.PrepareProcedurally.Interface.Dialogs;
using RimWorld;
using UnityEngine;
using Verse;

namespace Necrofancy.PrepareProcedurally.Interface.PawnColumnWorkers;

// ReSharper disable once UnusedType.Global
public class PawnCanBePrepared : PawnColumnWorker
{
    private const string CustomizeText = "Necrofancy.PrepareProcedurally.ClickToCustomizePawnTooltip";
    private static readonly Dictionary<string, string> LabelCache = new();
    private static float labelCacheForWidth = -1f;

    protected virtual TextAnchor LabelAlignment => TextAnchor.MiddleLeft;

    public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
    {
        var fullRect = new Rect(rect.x, rect.y, rect.width,
            Mathf.Min(rect.height, def.groupable ? rect.height : GetMinCellHeight(pawn)));
        var iconRect = fullRect;
        iconRect.xMin += 3f;

        iconRect.xMin += fullRect.height;
        Widgets.ThingIcon(new Rect(fullRect.x, fullRect.y, fullRect.height, fullRect.height), pawn);

        if (Mouse.IsOver(fullRect))
            GUI.DrawTexture(fullRect, TexUI.HighlightTex);

        var str = pawn.LabelShort.CapitalizeFirst();
        if (Math.Abs(iconRect.width - (double)labelCacheForWidth) > 0.01)
        {
            labelCacheForWidth = iconRect.width;
            LabelCache.Clear();
        }

        if (Text.CalcSize(str.StripTags()).x > (double)iconRect.width)
            str = str.StripTags().Truncate(iconRect.width, LabelCache);
        Text.Font = GameFont.Small;
        Text.Anchor = LabelAlignment;
        Text.WordWrap = false;
        Widgets.Label(iconRect, str);
        Text.WordWrap = true;
        Text.Anchor = TextAnchor.UpperLeft;
        if (Widgets.ButtonInvisible(fullRect))
        {
            var dialog = new EditSpecificPawn(pawn)
            {
                closeOnClickedOutside = true
            };
            Find.WindowStack.Add(dialog);
        }
        else
        {
            if (!Mouse.IsOver(fullRect))
                return;
            var tooltip = pawn.GetTooltip();
            tooltip.text = CustomizeText.Translate() + "\n\n" + tooltip.text;
            TooltipHandler.TipRegion(fullRect, tooltip);
        }
    }

    public override int GetMinWidth(PawnTable table)
    {
        return Mathf.Max(base.GetMinWidth(table), 80);
    }

    public override int GetOptimalWidth(PawnTable table)
    {
        return Mathf.Clamp(165, GetMinWidth(table), GetMaxWidth(table));
    }

    public override int Compare(Pawn a, Pawn b)
    {
        var indexA = StartingPawnUtility.PawnIndex(a);
        var indexB = StartingPawnUtility.PawnIndex(b);
        return indexA.CompareTo(indexB);
    }
}