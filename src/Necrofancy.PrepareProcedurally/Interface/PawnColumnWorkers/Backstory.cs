using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace Necrofancy.PrepareProcedurally.Interface.PawnColumnWorkers
{
    public abstract class Backstory : PawnColumnWorker
    {
        protected abstract BackstoryDef StoryFrom(Pawn pawn);
        
        public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
        {
            var story = StoryFrom(pawn);
            if (story is null)
                return;
            
            rect = rect.ContractedBy(4);
            var label = story.TitleCapFor(pawn.gender);

            var width = Text.CalcSize(story.TitleCapFor(pawn.gender)).x + 8;
            
            var color = GUI.color;
            GUI.color = CharacterCardUtility.StackElementBackground;
            GUI.DrawTexture(rect, BaseContent.WhiteTex);
            GUI.color = color;
            if (Mouse.IsOver(rect)) 
                Widgets.DrawHighlight(rect);
            Widgets.Label(new Rect(rect.x + 4f, rect.y, width, rect.height), label);
            GUI.color = Color.white;
            if (!Mouse.IsOver(rect)) 
                return;
            var tip = new TipSignal(() => story.FullDescriptionFor(pawn), (int) rect.y * 37);
            TooltipHandler.TipRegion(rect, tip);
        }

        public override int GetMinWidth(PawnTable table)
        {
            float maxWidth = 0;
            foreach (var pawn in table.PawnsListForReading)
            {
                if (StoryFrom(pawn) is null) 
                    continue;
                
                var width = Text.CalcSize(StoryFrom(pawn).TitleCapFor(pawn.gender)).x + 16f;
                maxWidth = Math.Max(maxWidth, width);
            }

            return (int)maxWidth;
        }
    }
}