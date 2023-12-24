using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using VerifyTests;
using VerifyXunit;
using Xunit;

namespace Necrofancy.PrepareProcedurally.Test
{
    /// <summary>
    /// Instrument Rimworld.exe to run the plugin that generates `received.txt` files, then run this test as a catch-all
    /// to approve all of those scenarios going on at once. 
    /// <para/>
    /// This way, there is no need to try "collecting" all the data from Rimworld base-game with respect to
    /// Skills, Traits, Backstories, and Bios. The latter two are not even in XML-definitions, so using an external
    /// source-parsing system does not even make that much sense either.
    /// </summary>
    [UsesVerify]
    public class IntegrationTests
    {
        // Adjust these to whatever fits your local directory best.
        private const string RimworldExe = @"C:/Program Files (x86)/Steam/steamapps/common/RimWorld/RimWorldWin64.exe";
        private const string PathToPluginTest = @"..\Necrofancy.PrepareProcedurally.Test.Mod\";
        private const string ApprovedSubFolderName = "Approved";
        private const string Message = "This scuffed test relies on the Test.Mod source being in a certain spot relative to this test in source.";

        // The game, with these arguments and the test plugin, will launch running in the background, run the scenarios, and exit asap.
        private const string Arguments = @"-popupwindow -quicktest -runscenarios -exitafterscenarios";

        [Fact]
        public void RunTest()
        {
            Process.Start(RimworldExe, Arguments)?.WaitForExit();
        }

        [Theory]
        [MemberData(nameof(GetFilesFromPlugin))]
        public Task Scenarios(string inputFile)
        {
            string fileName = Path.GetFileName(inputFile);
            string approvedFileName = fileName.Replace(".txt", string.Empty);

            VerifySettings settings = new VerifySettings();
            settings.UseFileName(approvedFileName);
            settings.UseDirectory(ApprovedSubFolderName);

            // Verify only supports multiple verifies when using VerifySettings.UseMethod(). Big sad.
            return Verifier.VerifyFile(inputFile, settings);
        }

        public static IEnumerable<object[]> GetFilesFromPlugin()
        {
            string sourceFolder = GetSource();
            foreach (string file in Directory.EnumerateFiles(sourceFolder, "*.txt"))
            {
                yield return new object[] {file};
            }
        }

        private static string GetSource([CallerFilePath] string thisSourceFile = null)
        {
            string directory = Path.GetDirectoryName(thisSourceFile) 
                               ?? throw new InvalidOperationException("Could not find source files.");
            string receivedFolder = Path.Combine(directory, PathToPluginTest);

            if (!Directory.Exists(receivedFolder))
                throw new InvalidOperationException(Message);

            return receivedFolder;
        }
    }
}