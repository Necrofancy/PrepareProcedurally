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

#if RW1_6
    protected override Color GetIconColor(Pawn pawn) => pawn.story.favoriteColor?.color ?? Color.black;
#else 
    protected override Color GetIconColor(Pawn pawn) => pawn.story.favoriteColor ?? Color.black;
#endif
    
    
    protected override string GetIconTip(Pawn pawn)
    {
#if RW1_4
        var orIdeoColor = string.Empty;
        if (pawn.Ideo != null && !pawn.Ideo.hiddenIdeoMode)
            orIdeoColor = "OrIdeoColor".Translate(pawn.Named("PAWN"));
        return "FavoriteColorTooltip".Translate(pawn.Named("PAWN"), 0.6f.ToStringPercent().Named("PERCENTAGE"), orIdeoColor.Named("ORIDEO")).Resolve();
#elif RW1_5
        var orIdeoColor = string.Empty;
        if (pawn.Ideo != null && !pawn.Ideo.hidden)
            orIdeoColor = "OrIdeoColor".Translate(pawn.Named("PAWN"));
        return "FavoriteColorTooltip".Translate(pawn.Named("PAWN"), 0.6f.ToStringPercent().Named("PERCENTAGE"), orIdeoColor.Named("ORIDEO")).Resolve();
#else
        var pawnNamed = pawn.Named("PAWN");
        
        var colorNamed = pawn.story.favoriteColor.label.Named("COLOR");
        var percentNamed = 0.6f.ToStringPercent().Named("PERCENTAGE");
        var ideoColor = "OrIdeoColor".Translate(pawnNamed);
        
        var orIdeoColor = string.Empty;
        if (pawn.Ideo != null && !pawn.Ideo.hidden)
            orIdeoColor = ideoColor;
        var orIdeoNamed = orIdeoColor.Named("ORIDEO");
        
        return "FavoriteColorTooltip".Translate(pawnNamed, colorNamed, percentNamed, orIdeoNamed).Resolve();
#endif
    }
        
    protected override void ClickedIcon(Pawn pawn)
    {
        var options = new List<FloatMenuGridOption>(AvailablePawnColors.Count);
        foreach (var color in AvailablePawnColors)
        {
            void ApplyColor()
            {
#if !(RW1_4 || RW1_5)
                pawn.story.favoriteColor = color;
#else 
                pawn.story.favoriteColor = color.color;
#endif
                Editor.LockedPawns.Add(pawn);
            }

            options.Add(new FloatMenuGridOption(BaseContent.WhiteTex, ApplyColor, color.color, color.LabelCap));
        }
            
        Find.WindowStack.Add(new FloatMenuGrid(options));
    }
}