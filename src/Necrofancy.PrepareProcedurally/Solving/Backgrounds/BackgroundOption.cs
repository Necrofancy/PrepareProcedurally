using System;
using System.Text;
using Necrofancy.PrepareProcedurally.Solving.StateEdits;
using RimWorld;
using UnityEngine;
using Verse;

namespace Necrofancy.PrepareProcedurally.Solving.Backgrounds;

public record BackstoryOption(
    BackstoryDef Def,
    GenderPossibility ForcedGender = GenderPossibility.Either,
    PawnBio ForcedBio = null)
{    
    public void Apply (int pawnIndex, Action onSelect)
    {
        if (ForcedBio is not null)
        {
            Editor.SetChildhoods[pawnIndex] = ForcedBio.childhood;
            Editor.SetAdulthoods[pawnIndex] = ForcedBio.adulthood;
        }
        else
        {
            var array = Def.slot == BackstorySlot.Childhood ? Editor.SetChildhoods : Editor.SetAdulthoods;
            var otherArray = Def.slot == BackstorySlot.Childhood ? Editor.SetAdulthoods : Editor.SetChildhoods;
            array[pawnIndex] = Def;
            if (otherArray[pawnIndex]?.shuffleable == false)
            {
                otherArray[pawnIndex] = null;
            }
        }
                
        onSelect();
    }
    
    
    private string Label()
    {
        return ForcedBio is not null
            ? SolidBioString(ForcedBio)
            : ForcedGender == GenderPossibility.Female
                ? Def.TitleCapFor(Gender.Female)
                : Def.TitleCapFor(Gender.Male);
    }
    
    private static string SolidBioString(PawnBio bio)
    {
        var name = bio.name.ToStringFull;
        return bio.gender switch
        {
            GenderPossibility.Male => $"<color=#06c2ac>{name}</color>",
            GenderPossibility.Female => $"<color=#c79fef>{name}</color>",
            _ => bio.name.ToStringFull
        };
    }

    public void DrawLabel(Rect labelRect, Pawn reference)
    {
        Widgets.Label(labelRect, Label());
        if (Mouse.IsOver(labelRect))
        {
            TooltipHandler.TipRegion(labelRect, CreateTooltip(reference));
        }
    }

    public FloatMenuOption CreateFloatOption(Pawn pawn, int pawnIndex, bool disabled, Action onSelect)
    {
        var option = this;

        void Add()
        {
            option.Apply(pawnIndex, onSelect);
        }
        
        void ShowTooltip(Rect draw)
        {
            TooltipHandler.TipRegion(draw, option.CreateTooltip(pawn));
        }
        
        return new FloatMenuOption(this.CreateTooltip(pawn), Add, mouseoverGuiAction: ShowTooltip)
        {
            Disabled = disabled
        };
    }
    
    private string CreateTooltip(Pawn pawn)
    {
        if (ForcedBio is null)
        {
            return Def.FullDescriptionFor(pawn);
        }

        using (FakeBackstory(pawn, ForcedBio))
        {
            StringBuilder builder = new();
            builder.AppendLine(ForcedBio.childhood.FullDescriptionFor(pawn));
            builder.AppendLine();
            builder.AppendLine(ForcedBio.adulthood.FullDescriptionFor(pawn));
            return builder.ToString();
        }
    }

    public static TemporaryEdit<(Name, Gender)> FakeBackstory(Pawn pawn, PawnBio bio)
    {
        var current = (pawn.Name, pawn.gender);
        var setGender = bio.gender switch
        {
            GenderPossibility.Male => Gender.Male,
            GenderPossibility.Female => Gender.Female,
            _ => pawn.gender
        };
        var setForBackstoryDesc = (bio.name, setGender);
        void Set((Name name, Gender gender) pawnFacts)
        {
            pawn.Name = pawnFacts.name;
            pawn.gender = pawnFacts.gender;
        }

        return new TemporaryEdit<(Name, Gender)>(current, setForBackstoryDesc, Set);
    }
}