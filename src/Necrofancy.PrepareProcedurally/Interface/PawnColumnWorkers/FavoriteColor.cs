﻿using System.Collections.Generic;
using System.Linq;
using Necrofancy.PrepareProcedurally.Solving;
using RimWorld;
using UnityEngine;
using Verse;

namespace Necrofancy.PrepareProcedurally.Interface.PawnColumnWorkers
{
    public class FavoriteColor : PawnColumnWorker_Icon
    {
        private static List<Color> _allColors;

        /// <summary>
        /// Gets the list of available colors that would be pickable from the Styling Station in Ideology
        /// </summary>
        public static List<ColorDef> AvailablePawnColors => DefDatabase<ColorDef>.AllDefsListForReading;
        
        protected override Texture2D GetIconFor(Pawn pawn) => BaseContent.WhiteTex;

        protected override Color GetIconColor(Pawn pawn) => pawn.story.favoriteColor ?? Color.black;

        protected override string GetIconTip(Pawn pawn)
        {
            string orIdeoColor = string.Empty;
            if (pawn.Ideo != null && !pawn.Ideo.hiddenIdeoMode)
                orIdeoColor = "OrIdeoColor".Translate(pawn.Named("PAWN"));
            return "FavoriteColorTooltip".Translate(pawn.Named("PAWN"), 0.6f.ToStringPercent().Named("PERCENTAGE"), orIdeoColor.Named("ORIDEO")).Resolve();
        }
        
        protected override void ClickedIcon(Pawn pawn)
        {
            var options = new List<FloatMenuGridOption>(AvailablePawnColors.Count);
            foreach (var color in AvailablePawnColors)
            {
                void ApplyColor()
                {
                    pawn.story.favoriteColor = color.color;
                    ProcGen.LockedPawns.Add(pawn);
                }

                options.Add(new FloatMenuGridOption(BaseContent.WhiteTex, ApplyColor, color.color, color.LabelCap));
            }
            
            Find.WindowStack.Add(new FloatMenuGrid(options));
        }
    }
}