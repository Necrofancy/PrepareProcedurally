using System.Text;
using Necrofancy.PrepareProcedurally.Editing;
using Necrofancy.PrepareProcedurally.Solving.StateEdits;

namespace Necrofancy.PrepareProcedurally.Solving.Backgrounds;

public record BackstoryOption
{
    public BackstoryOption(BackstoryDef def)
    {
        Backstory = def;
        ForcedBio = null;
    }

    public BackstoryOption(PawnBio bio)
    {
        Backstory = null;
        ForcedBio = bio;
        GenderRequirement = bio.gender;
    }

    public BackstoryDef Backstory { get; init; }

    public PawnBio ForcedBio { get; init; }

    public GenderPossibility GenderRequirement { get; init; }

    private string Label()
    {
        return ForcedBio is not null
            ? SolidBioString(ForcedBio)
            : Backstory.titleFemale is not null
                ? $"{Backstory.TitleCapFor(Gender.Male)} / {Backstory.TitleCapFor(Gender.Female)}"
                : Backstory.TitleCapFor(Gender.Male);
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
            return Backstory.FullDescriptionFor(pawn);
        }

        using (EditSequence.Apply(HackFactory.SetBackstory(pawn, ForcedBio)))
        {
            StringBuilder builder = new();
            builder.AppendLine(ForcedBio.childhood.FullDescriptionFor(pawn));
            builder.AppendLine();
            builder.AppendLine(ForcedBio.adulthood.FullDescriptionFor(pawn));
            return builder.ToString();
        }
    }
}
