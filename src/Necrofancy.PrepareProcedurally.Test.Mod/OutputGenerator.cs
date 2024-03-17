using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Necrofancy.PrepareProcedurally.Solving.Backgrounds;
using Necrofancy.PrepareProcedurally.Solving.Skills;
using RimWorld;
using Verse;

namespace Necrofancy.PrepareProcedurally.Test.Mod
{
    public static class OutputGenerator
    {
        public static void GenerateOutput(Situation situationWithFileName)
        {
            var situation = situationWithFileName.ToBalance;
            var ageRange = new IntRange(21, 25);
            var ageGenerationCurve = Faction.OfPlayer.def.basicMemberKind.race.race.ageGenerationCurve;
            var subsample = EstimateRolling.SubSampleCurve(ageGenerationCurve, ageRange);

            var possibilities = BackstorySolver.TryToSolveWith(situation, subsample, new IntRange(1, 1));
            var passionRanges = BackstorySolver.FigureOutPassions(possibilities, situation);
            using (var writer = CreateWriter(situationWithFileName.FileName))
            {
                writer.WriteLine($"{situation.CategoryName} start with {situation.Pawns} pawns.");
                writer.WriteLine(situationWithFileName.Description);

                NewSection(writer);

                foreach (var req in situation.SkillRequirements)
                    writer.WriteLine($"    {req.Skill}: 🔥🔥:{req.major} 🔥:{req.minor} Usable:{req.usable}");

                NewSection(writer);

                NewSection(writer);

                for (var i = 0; i < possibilities.Count; i++)
                {
                    var possibility = possibilities[i];
                    var passionRanged = passionRanges[i];

                    if (passionRanged != null && !passionRanged.Value.ValidVanillaPawn)
                        writer.WriteLine($"!!!!COULD NOT FINALIZE THIS PAWN!!!!");
                    writer.WriteLine($"Childhood: {possibility.Background.Childhood.title}");
                    foreach (var skillLevel in possibility.Background.Childhood.skillGains)
                        writer.WriteLine($"    {skillLevel.skill.skillLabel}: {skillLevel.amount}");

                    writer.WriteLine($"    Disabled Work: {possibility.Background.Childhood.workDisables}");

                    writer.WriteLine($"Adulthood: {possibility.Background.Adulthood.title}");
                    foreach (var skillLevel in possibility.Background.Adulthood.skillGains)
                        writer.WriteLine($"    {skillLevel.skill.skillLabel}: {skillLevel.amount}");

                    writer.WriteLine($"    Disabled Work: {possibility.Background.Adulthood.workDisables}");
                    writer.WriteLine($"Skills at Age 35:");
                    foreach (var keyValue in possibility.SkillRanges.OrderByDescending(kv => kv.Key.listOrder))
                    {
                        var def = keyValue.Key;
                        var fullRange = keyValue.Value;
                        if (passionRanged != null)
                        {
                            var solved = passionRanged.Value.FinalRanges[def];
                            writer.WriteLine(possibility.Background.DisablesWorkType(def)
                                ? $"    {keyValue.Key}: --"
                                : $"    {def}: {fullRange.min}-{fullRange.max} solved to {solved}");
                        }
                    }

                    writer.WriteLine();
                }
            }
        }

        private static void NewSection(TextWriter writer)
        {
            writer.WriteLine();
            writer.WriteLine("=============================================================");
            writer.WriteLine();
        }

        public static void ClearFiles([CallerFilePath] string filePath = null)
        {
            var directory = Path.GetDirectoryName(filePath)
                            ?? throw new InvalidOperationException("Could not find source folder");
            foreach (var file in Directory.EnumerateFiles(directory, "*.txt"))
                File.Delete(file);
        }

        private static TextWriter CreateWriter(string scenarioName, [CallerFilePath] string filePath = null)
        {
            var directory = Path.GetDirectoryName(filePath)
                            ?? throw new InvalidOperationException("Could not find source folder");
            var receivedFilePath = Path.Combine(directory, $"{scenarioName}.txt");

            return new StreamWriter(receivedFilePath);
        }
    }
}