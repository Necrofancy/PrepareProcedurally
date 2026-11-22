using System.Text;
using Necrofancy.PrepareProcedurally.Editing;
using Necrofancy.PrepareProcedurally.Interface;
using Necrofancy.PrepareProcedurally.Solving.StateEdits;

namespace Necrofancy.PrepareProcedurally.Solving.Backgrounds;

public record BackstoryOption(
    BackstoryDef Def,
    GenderPossibility ForcedGender = GenderPossibility.Either,
    PawnBio ForcedBio = null
)
{
    private string Label()
    {
        return ForcedBio is not null
            ? SolidBioString(ForcedBio)
            : Def.titleFemale is not null
                ? $"{Def.TitleCapFor(Gender.Male)} / {Def.TitleCapFor(Gender.Female)}"
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

    public FloatMenuOption CreateFloatOption(PawnSlot slot, bool disabled, Action onSelect)
    {
        void Add()
        {
            slot.ApplyBackstoryOption(this);
            onSelect();
        }

        void ShowTooltip(Rect draw)
        {
            TooltipHandler.TipRegion(draw, CreateTooltip(slot.Pawn));
        }

        return new FloatMenuOption(Label(), Add, mouseoverGuiAction: ShowTooltip) { Disabled = disabled };
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
