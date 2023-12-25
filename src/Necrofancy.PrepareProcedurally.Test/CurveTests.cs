using Necrofancy.PrepareProcedurally.Solving.Skills;
using Verse;
using Xunit;
using Xunit.Abstractions;

namespace Necrofancy.PrepareProcedurally.Test
{
    public class CurveTests
    {
        private static readonly SimpleCurve HumanAgeRange = new SimpleCurve
        {
            new CurvePoint(16f, 0f),
            new CurvePoint(20f, 100f),
            new CurvePoint(30f, 100f),
            new CurvePoint(50f, 30f),
            new CurvePoint(60f, 10f),
            new CurvePoint(70f, 1f),
            new CurvePoint(80, 0)
        };
        
        private readonly ITestOutputHelper testOutputHelper;

        public CurveTests(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void ForceWithinRange()
        {
            const int Min = 30;
            const int Max = 50;
            const int Attempts = 10000;
            var curve = EstimateRolling.SubSampleCurve(HumanAgeRange, new IntRange(30, 50));
            foreach (var point in curve)
            {
                testOutputHelper.WriteLine($"{point.x}: {point.y}");
            }
            for (int i = 0; i < Attempts; i++)
            {
                float age = Rand.ByCurve(curve);
                Assert.True(Min < age && age < Max, $"Age was {age}");
            }
        }
    }
}