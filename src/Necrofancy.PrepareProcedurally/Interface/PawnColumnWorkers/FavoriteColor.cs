using System.Collections.Generic;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

namespace Necrofancy.PrepareProcedurally.Interface.PawnColumnWorkers;

[UsedImplicitly]
public class FavoriteColor : PawnColumnWorker_Icon
{
    /// <summary>
    /// Gets the list of available colors that would be pickable from the Styling Station in Ideology
    /// </summary>
    public static List<ColorDef> AvailablePawnColors => DefDatabase<ColorDef>.AllDefsListForReading;
        
    protected override Texture2D GetIconFor(Pawn pawn) => BaseContent.WhiteTex;

    protected override Color GetIconColor(Pawn pawn) => pawn.story.favoriteColor?.color ?? Color.black;

    protected override string GetIconTip(Pawn pawn)
    {
        var orIdeoColor = string.Empty;
        if (pawn.Ideo != null && !pawn.Ideo.hidden)
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
                pawn.story.favoriteColor = color;
                Editor.LockedPawns.Add(pawn);
            }

            options.Add(new FloatMenuGridOption(BaseContent.WhiteTex, ApplyColor, color.color, color.LabelCap));
        }
            
        Find.WindowStack.Add(new FloatMenuGrid(options));
    }
}