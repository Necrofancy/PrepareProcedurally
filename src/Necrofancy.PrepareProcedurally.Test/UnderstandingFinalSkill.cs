using System.Collections.Generic;
using System.Threading.Tasks;
using VerifyXunit;
using Verse;
using Xunit;
using Xunit.Abstractions;

namespace Necrofancy.PrepareProcedurally.Test
{
    [UsesVerify]
    public class UnderstandingFinalSkill
    {
        private readonly ITestOutputHelper testOutputHelper;

        public UnderstandingFinalSkill(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
        }

        private static readonly SimpleCurve LevelRandomCurve = new SimpleCurve
        {
            new CurvePoint(0f, 0f),
            new CurvePoint(0.5f, 150f),
            new CurvePoint(4f, 150f),
            new CurvePoint(5f, 25f),
            new CurvePoint(10f, 5f),
            new CurvePoint(15f, 0f)
        };

        [Fact]
        public void BruteForceDistributions()
        {
            int[] skills = new int[16];
            const int rolls = 10000000;
            for (int i = 0; i < rolls; i++)
            {
                int num = (int)Rand.ByCurve(LevelRandomCurve);
                skills[num]++;
            }

            for (int i = 0; i < 16; i++)
            {
                testOutputHelper.WriteLine($"{i}\t{skills[i]}");
            }
        }

        [Fact]
        public Task VerifyCurveRanges()
        {
            Dictionary<int, float> values = new Dictionary<int, float>();
            for (int i = 0; i <= 100; i++)
            {
                float percent = i * 0.01f;
                float skillLevel = LevelRandomCurve.AssumingPercentRoll(percent);
                values[i] = skillLevel;
            }

            return Verifier.Verify(values);
        }

        [Fact]
        public Task VerifyIntRanges()
        {
            IntRange defaultSkillRolls = new IntRange(0, 4);
            Dictionary<int, float> values = new Dictionary<int, float>();
            for (int i = 0; i <= 100; i += 5)
            {
                float percent = i * 0.01f;
                float skillLevel = defaultSkillRolls.AssumingPercentRoll(percent);
                values[i] = skillLevel;
            }

            return Verifier.Verify(values);
        }
        
        [Fact]
        public Task VerifyFloatRanges()
        {
            FloatRange backstoryMultiplierRolls = new FloatRange(1f, 1.4f);
            Dictionary<int, float> values = new Dictionary<int, float>();
            for (int i = 0; i <= 100; i += 5)
            {
                float percent = i * 0.01f;
                float skillLevel = backstoryMultiplierRolls.AssumingPercentRoll(percent);
                values[i] = skillLevel;
            }

            return Verifier.Verify(values);
        }
    }
}