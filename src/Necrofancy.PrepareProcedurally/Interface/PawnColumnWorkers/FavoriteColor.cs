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
        var pawnNamed = pawn.Named("PAWN");
        var colorNamed = pawn.story.favoriteColor.label.Named("COLOR");
        var percentNamed = 0.6f.ToStringPercent().Named("PERCENTAGE");
        var ideoColor = "OrIdeoColor".Translate(pawnNamed);
        
        var orIdeoColor = string.Empty;
        if (pawn.Ideo != null && !pawn.Ideo.hidden)
            orIdeoColor = ideoColor;
        var orIdeoNamed = orIdeoColor.Named("ORIDEO");
        
        return "FavoriteColorTooltip".Translate(pawnNamed,colorNamed, percentNamed, orIdeoNamed).Resolve();
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