using System.Collections.Generic;
using Necrofancy.PrepareProcedurally.Interface.Dialogs;
using RimWorld;
using UnityEngine;
using Verse;

namespace Necrofancy.PrepareProcedurally.Interface.PawnColumnWorkers
{
    // ReSharper disable once UnusedType.Global

    public class PawnCanBePrepared : PawnColumnWorker
    {
        private const string CustomizeText = "ClickToCustomizeGeneration";
        private static Dictionary<string, string> labelCache = new Dictionary<string, string>();
        private static float labelCacheForWidth = -1f;

        protected virtual TextAnchor LabelAlignment => TextAnchor.MiddleLeft;

        public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
        {
            Rect fullRect = new Rect(rect.x, rect.y, rect.width,
                Mathf.Min(rect.height, def.groupable ? rect.height : GetMinCellHeight(pawn)));
            Rect iconRect = fullRect;
            iconRect.xMin += 3f;
            
            iconRect.xMin += fullRect.height;
            Widgets.ThingIcon(new Rect(fullRect.x, fullRect.y, fullRect.height, fullRect.height), pawn);

            if (Mouse.IsOver(fullRect))
                GUI.DrawTexture(fullRect, TexUI.HighlightTex);
            
            string str = pawn.LabelNoCount.CapitalizeFirst();
            if (iconRect.width != (double) labelCacheForWidth)
            {
                labelCacheForWidth = iconRect.width;
                labelCache.Clear();
            }

            if (Text.CalcSize(str.StripTags()).x > (double) iconRect.width)
                str = str.StripTags().Truncate(iconRect.width, labelCache);
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
                TipSignal tooltip = pawn.GetTooltip();
                tooltip.text = CustomizeText.Translate() + "\n\n" + tooltip.text;
                TooltipHandler.TipRegion(fullRect, tooltip);
            }
        }

        public override int GetMinWidth(PawnTable table) => Mathf.Max(base.GetMinWidth(table), 80);

        public override int GetOptimalWidth(PawnTable table) =>
            Mathf.Clamp(165, GetMinWidth(table), GetMaxWidth(table));

        public override int Compare(Pawn a, Pawn b)
        {
            var indexA = StartingPawnUtility.PawnIndex(a);
            var indexB = StartingPawnUtility.PawnIndex(b);
            return indexA.CompareTo(indexB);
        }
    }
}