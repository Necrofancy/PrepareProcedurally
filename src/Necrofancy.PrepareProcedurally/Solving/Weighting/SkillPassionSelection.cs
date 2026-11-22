using Necrofancy.PrepareProcedurally.Defs;
using Necrofancy.PrepareProcedurally.Editing;
using Necrofancy.PrepareProcedurally.Solving.Backgrounds;

namespace Necrofancy.PrepareProcedurally.Solving.Weighting;

public class SkillPassionSelection
{
    public int Total => major + minor + usable;

    public int major;
    public int minor;
    public int usable;

    public SkillPassionSelection(SkillDef def)
    {
        Skill = def;
    }

    public SkillDef Skill { get; }

    public int GetWeights(IReadOnlyList<BackgroundPossibility> possibilities)
    {
        const int MajorPassionLevel = 7, MajorPassionWeight = 120;
        const int MinorPassionLevel = 5, MinorPassionWeight = 110;
        const int UsableLevel = 4, UsableWeight = 100;

        var majorLeft = major;
        var minorLeft = minor;
        var usableLeft = usable;

        foreach (var possibility in possibilities)
        {
            var range = possibility.SkillRanges[Skill];
            if (range.TrueMax > MajorPassionLevel)
            {
                if (majorLeft > 0)
                    majorLeft--;
                else if (minorLeft > 0)
                    minorLeft--;
                else if (usableLeft > 0)
                    usableLeft--;
            }
            else if (range.TrueMax > MinorPassionLevel)
            {
                if (minorLeft > 0)
                    minorLeft--;
                else if (usableLeft > 0)
                    usableLeft--;
            }
            else if (range.TrueMax > UsableLevel && usableLeft > 0)
            {
                usableLeft--;
            }
        }

        return majorLeft * MajorPassionWeight + minorLeft * MinorPassionWeight + usableLeft * UsableWeight;
    }

    public static SkillPassionSelection CreateFromSkill(SkillDef def)
    {
        var situation = SituationFactory.FromPlayerData();

        return situation.SkillRequirements
            .FirstOrFallback(x => x.Skill == def, new SkillPassionSelection(def));
    }

    public static IReadOnlyList<SkillPassionSelection> FromReqs(IEnumerable<SkillRequirementDef> reqs, int pawnCount)
    {
        var selections = new Dictionary<SkillDef, SkillPassionSelection>();
        foreach (var requirement in reqs)
        {
            if (!selections.TryGetValue(requirement.skill, out var selection))
            {
                selection = new SkillPassionSelection(requirement.skill);
                selections[requirement.skill] = selection;
            }

            if (requirement.passion == Passion.Major)
                selection.major = Math.Max(selection.major, requirement.Count(pawnCount));
            else if (requirement.passion == Passion.Minor)
                selection.minor = Math.Max(selection.minor, requirement.Count(pawnCount));
            else
                selection.usable = Math.Max(selection.usable, requirement.Count(pawnCount));
        }

        var selectionsList = new List<SkillPassionSelection>(selections.Count);
        foreach (var selection in selections.Values)
        {
            selection.minor = Math.Min(selection.minor, pawnCount - selection.major);
            selection.usable = Math.Min(selection.usable, pawnCount - selection.major - selection.minor);
            selectionsList.Add(selection);
        }

        return selectionsList;
    }

    public bool StartingGroupSatisfies(
        IReadOnlyCollection<PawnSlot> slots,
        out int majorLeft,
        out int minorLeft,
        out int usableLeft)
    {
        majorLeft = major;
        minorLeft = minor;
        usableLeft = usable;

        foreach (var slot in slots)
        {
            var skillRecord = slot.Pawn.skills.skills.First(x => x.def == Skill);
            if (skillRecord.passion >= Passion.Major && majorLeft > 0)
                majorLeft--;
            else if (skillRecord.passion >= Passion.Minor && minorLeft > 0)
                minorLeft--;
            else if (!skillRecord.TotallyDisabled)
                usableLeft--;
        }

        return majorLeft <= 0 && minorLeft <= 0 && usableLeft <= 0;
    }
}
