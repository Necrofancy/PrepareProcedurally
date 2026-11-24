using System.Text;
using Necrofancy.PrepareProcedurally.Editing;

namespace Necrofancy.PrepareProcedurally.Interface.PawnColumnWorkers;

[UsedImplicitly]
public class Gender : PawnColumnWorker_Icon
{
    private const string PreferMaleLabel = "Necrofancy.PrepareProcedurally.ProcGenPreferMaleLabel";
    private const string PreferFemaleLabel = "Necrofancy.PrepareProcedurally.ProcGenPreferFemaleLabel";
    private const string RemovePreferenceLabel = "Necrofancy.PrepareProcedurally.ProcGenNoGenderPreferenceTooltip";

    private const string TooltipPreferMale = "Necrofancy.PrepareProcedurally.ProcGenPreferMaleTooltip";
    private const string TooltipPreferFemale = "Necrofancy.PrepareProcedurally.ProcGenPreferFemaleTooltip";
    private const string TooltipNoPreference = "Necrofancy.PrepareProcedurally.ProcGenPreferNoGenderPreferenceTooltip";

    private const string TooltipCouldRequestGender = "Necrofancy.PrepareProcedurally.ProcGenCannotRequestGender";

    private static EditorContext Editor => EditorSession.Require();

    protected override Texture2D GetIconFor(Pawn pawn) => pawn.gender.GetIcon();

    protected override Color GetIconColor(Pawn pawn)
    {
        if (!CanSelectGender(pawn))
            return ColoredText.SubtleGrayColor;

        var index = StartingPawnUtility.PawnIndex(pawn);
        var genderPossibility = index != -1 ? EditorSession.Require().Slots[index].Gender : GenderPossibility.Either;
        return genderPossibility switch
        {
            GenderPossibility.Male => ColorLibrary.Turquoise,
            GenderPossibility.Female => ColorLibrary.Lavender,
            _ => Color.white
        };
    }

    protected override string GetIconTip(Pawn pawn)
    {
        StringBuilder builder = new StringBuilder(pawn.GetGenderLabel().CapitalizeFirst());
        var index = StartingPawnUtility.PawnIndex(pawn);
        if (index < 0)
            return builder.ToString();

        builder.AppendLine().AppendLine();
        if (!CanSelectGender(pawn))
        {
            builder.AppendLine(TooltipCouldRequestGender.Translate());
        }
        else
        {
            var currentPref = Editor.Slots[index].Gender switch
            {
                GenderPossibility.Male => TooltipPreferMale.Translate(),
                GenderPossibility.Female => TooltipPreferFemale.Translate(),
                _ => TooltipNoPreference.Translate()
            };

            builder.AppendLine(currentPref);
        }

        return builder.ToString();
    }

    protected override void ClickedIcon(Pawn pawn)
    {
        var options = new List<FloatMenuOption>();

        var index = StartingPawnUtility.PawnIndex(pawn);
        if (index < 0)
            return;

        var currentPossibility = Editor.Slots[index].Gender;

        if (currentPossibility != GenderPossibility.Male)
            options.Add(OptionForGender(index, PreferMaleLabel, GenderPossibility.Male));

        if (currentPossibility != GenderPossibility.Female)
            options.Add(OptionForGender(index, PreferFemaleLabel, GenderPossibility.Female));

        if (currentPossibility != GenderPossibility.Either)
            options.Add(OptionForGender(index, RemovePreferenceLabel, GenderPossibility.Either));

        Find.WindowStack.Add(new FloatMenu(options));
    }

    private static FloatMenuOption OptionForGender(int pawnIndex, string label, GenderPossibility possibility)
    {
        void OnClick()
        {
            Editor.Slots[pawnIndex].Gender = possibility;
        }

        return new FloatMenuOption(label.Translate(), OnClick);
    }

    private static bool CanSelectGender(Pawn pawn)
    {
        return PrepareMod.Settings.RuleSet.RaceRules.GetGenderPossibility(pawn.def) == GenderPossibility.Either;
    }
}
