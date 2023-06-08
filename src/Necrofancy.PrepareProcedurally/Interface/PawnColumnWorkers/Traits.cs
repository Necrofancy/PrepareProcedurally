using System;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

// ReSharper disable once UnusedType.Global

namespace Necrofancy.PrepareProcedurally.Interface.PawnColumnWorkers
{
    public class Traits : PawnColumnWorker
    {
        private const int PaddingBetweenButtons = 4;
        const int AddButtonWidth = 30;
        
        public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
        {
            var traitsCount = pawn.story.traits.allTraits.Count;
            var allowPlusButton = traitsCount < 4;

            rect = rect.ContractedBy(PaddingBetweenButtons);

            float x = rect.x;
            float y = rect.y;
            float width;
            float height = rect.height;
            
            foreach (var trait in pawn.story.traits.allTraits)
            {
                width = Text.CalcSize(trait.LabelCap).x + 10f;
                var drawRect = new Rect(x, y, width, height);
                DrawTrait(drawRect, trait, pawn);
                x += width + PaddingBetweenButtons;
            }

            if (allowPlusButton)
            {
                Widgets.ButtonText(new Rect(x, y, AddButtonWidth, height), "+");
            }
        }

        public override int GetMinWidth(PawnTable table)
        {
            float maxWidth = 0;
            foreach (var pawn in table.PawnsListForReading)
            {
                var width = pawn.story.traits.allTraits.Sum(trait => Text.CalcSize(trait.LabelCap).x + 10f + 2 * PaddingBetweenButtons);
                if (pawn.story.traits.allTraits.Count < 4)
                {
                    width += 2 * PaddingBetweenButtons + AddButtonWidth;
                }

                maxWidth = Math.Max(maxWidth, width);
            }

            return (int)maxWidth;
        }

        private static void DrawTrait(Rect rect, Trait trait, Pawn pawn)
        {
            Color color = GUI.color;
            GUI.color = CharacterCardUtility.StackElementBackground;
            GUI.DrawTexture(rect, BaseContent.WhiteTex);
            GUI.color = color;
            if (Mouse.IsOver(rect)) Widgets.DrawHighlight(rect);
            if (trait.Suppressed)
                GUI.color = ColoredText.SubtleGrayColor;
            else if (trait.sourceGene != null) GUI.color = ColoredText.GeneColor;
            Widgets.Label(new Rect(rect.x + 5f, rect.y, rect.width - 10f, rect.height), trait.LabelCap);
            GUI.color = Color.white;
            if (!Mouse.IsOver(rect)) return;
            Trait trLocal = trait;
            TipSignal tip = new TipSignal(() => trLocal.TipString(pawn), (int) rect.y * 37);
            TooltipHandler.TipRegion(rect, tip);
        }
        
        
    }
}