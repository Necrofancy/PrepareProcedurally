using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Necrofancy.PrepareProcedurally.Solving.Backgrounds;
using RimWorld;
using Verse;

namespace Necrofancy.PrepareProcedurally.Test.Mod
{
    public static class OutputGenerator
    {
        public static void GenerateOutput(Situation situationWithFileName)
        {
            var situation = situationWithFileName.ToBalance;
            var possibilities = BackstorySolver.TryToSolveWith(situation, new IntRange(1,1));
            var passionRanges = BackstorySolver.FigureOutPassions(possibilities, situation);
            using (var writer = CreateWriter(situationWithFileName.FileName))
            {
                writer.WriteLine($"{situation.CategoryName} start with {situation.Pawns} pawns.");
                writer.WriteLine(situationWithFileName.Description);

                NewSection(writer);

                foreach (var req in situation.SkillRequirements)
                {
                    writer.WriteLine($"    {req.Skill}: 🔥🔥:{req.major} 🔥:{req.minor} Usable:{req.usable}");
                }
                
                NewSection(writer);

                NewSection(writer);

                for (int i = 0; i < possibilities.Count; i++)
                {
                    var possibility = possibilities[i];
                    var passionRanged = passionRanges[i];
                    
                    if (!passionRanged.Value.ValidVanillaPawn)
                        writer.WriteLine($"!!!!COULD NOT FINALIZE THIS PAWN!!!!");
                    writer.WriteLine($"Childhood: {possibility.Background.Childhood.title}");
                    foreach (var skillLevel in possibility.Background.Childhood.skillGains)
                    {
                        writer.WriteLine($"    {skillLevel.Key.skillLabel}: {skillLevel.Value}");
                    }

                    writer.WriteLine($"    Disabled Work: {possibility.Background.Childhood.workDisables}");

                    writer.WriteLine($"Adulthood: {possibility.Background.Adulthood.title}");
                    foreach (var skillLevel in possibility.Background.Adulthood.skillGains)
                    {
                        writer.WriteLine($"    {skillLevel.Key.skillLabel}: {skillLevel.Value}");
                    }

                    writer.WriteLine($"    Disabled Work: {possibility.Background.Adulthood.workDisables}");
                    writer.WriteLine($"Skills at Age 35:");
                    foreach (var keyValue in possibility.SkillRanges.OrderByDescending(kv => kv.Key.listOrder))
                    {
                        var def = keyValue.Key;
                        var fullRange = keyValue.Value;
                        var solved = passionRanged.Value.FinalRanges[def];
                        if (possibility.Background.DisablesWorkType(def))
                            writer.WriteLine($"    {keyValue.Key}: --");
                        else
                            writer.WriteLine($"    {def}: {fullRange.min}-{fullRange.max} solved to {solved}");
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
            string directory = Path.GetDirectoryName(filePath);
            foreach (string file in Directory.EnumerateFiles(directory, "*.txt"))
                File.Delete(file);
        }

        private static TextWriter CreateWriter(string scenarioName, [CallerFilePath] string filePath = null)
        {
            string directory = Path.GetDirectoryName(filePath);
            string receivedFilePath = Path.Combine(directory, $"{scenarioName}.txt");

            return new StreamWriter(receivedFilePath);
        }
    }
}