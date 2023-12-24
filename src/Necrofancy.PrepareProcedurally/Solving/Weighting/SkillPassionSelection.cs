using System;
using System.Collections.Generic;
using System.Linq;
using Necrofancy.PrepareProcedurally.Defs;
using Necrofancy.PrepareProcedurally.Solving.Backgrounds;
using RimWorld;
using Verse;

namespace Necrofancy.PrepareProcedurally.Solving.Weighting
{
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
            const int majorPassionLevel = 7, majorPassionWeight = 120;
            const int minorPassionLevel = 5, minorPassionWeight = 110;
            const int usableLevel = 4, usableWeight = 100;

            var majorLeft = major;
            var minorLeft = minor;
            var usableLeft = usable;

            foreach (var possibility in possibilities)
            {
                var range = possibility.SkillRanges[Skill];
                if (range.TrueMax > majorPassionLevel)
                {
                    if (majorLeft > 0)
                        majorLeft--;
                    else if (minorLeft > 0)
                        minorLeft--;
                    else if (usableLeft > 0)
                        usableLeft--;
                }
                else if (range.TrueMax > minorPassionLevel)
                {
                    if (minorLeft > 0)
                        minorLeft--;
                    else if (usableLeft > 0)
                        usableLeft--;
                }
                else if (range.TrueMax > usableLevel && usableLeft > 0)
                {
                    usableLeft--;
                }
            }
            
            return majorLeft * majorPassionWeight + minorLeft * minorPassionWeight + usableLeft * usableWeight;
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
                {
                    selection.major = Math.Max(selection.major, requirement.Count(pawnCount));
                }
                else if (requirement.passion == Passion.Minor)
                {
                    selection.minor = Math.Max(selection.minor, requirement.Count(pawnCount));
                }
                else
                {
                    selection.usable = Math.Max(selection.usable, requirement.Count(pawnCount));
                }
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

        public bool StartingGroupSatisfies(IReadOnlyCollection<Pawn> pawns)
        {
            var majorLeft = major;
            var minorLeft = minor;
            var usableLeft = usable;

            foreach (var pawn in pawns)
            {
                var skillRecord = pawn.skills.skills.First(x => x.def == Skill);
                if (skillRecord.passion == Passion.Major && majorLeft > 0)
                    majorLeft--;
                else if (skillRecord.passion >= Passion.Minor)
                    minorLeft--;
                else if (!skillRecord.TotallyDisabled)
                    usableLeft--;
            }
            
            return majorLeft <= 0 && minorLeft <= 0 && usableLeft <= 0;
        }
    }
}