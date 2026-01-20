using System;
using System.Collections.Generic;
using Necrofancy.PrepareProcedurally.Solving;
using Necrofancy.PrepareProcedurally.Solving.Weighting;
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
    
    
    public override Vector2 InitialSize => new(CardSizeX + WindowMargin, CardSizeY + WindowMargin + YPadding);

    public EditSpecificPawn(Pawn pawn)
    {
        SelectedPawn.Select(pawn);
        doCloseX = true;
    }
    
    public override void DoWindowContents(Rect rect)
    {
        if (SelectedPawn.Pawn is null || SelectedPawn.Pawn.Destroyed || SelectedPawn.Pawn.Discarded)
        {
            Close();
            return;
        }

        Text.Font = GameFont.Medium;
        var titleRect = new Rect(rect.x, rect.y, rect.width, YPadding);
        Widgets.Label(titleRect, "Necrofancy.PrepareProcedurally.EditSpecificPawnTitle".Translate());
        Text.Font = GameFont.Small;

        rect.y += YPadding;
        rect.height -= YPadding;

        var pawnIndex = StartingPawnUtility.PawnIndex(SelectedPawn.Pawn);
        ReusingVanillaUi.DrawPortraitArea(rect, pawnIndex, true, true);
    }
}