using UnityEngine;
using Verse;
using Xunit;
using Xunit.Abstractions;

namespace Necrofancy.PrepareProcedurally.Test
{
    public class UnderstandingPassionPoints
    {
        private readonly ITestOutputHelper testOutputHelper;

        public UnderstandingPassionPoints(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
        }
        [Fact]
        public void BruteForcingDistributions()
        {
            int[] counts = new int[20];
            const int rolls = 10000000;
            for (int i = 0; i < rolls; i++)
            {
                // this is what a generation will come up with.
                float value = 5f + Mathf.Clamp(Rand.Gaussian(), -4f, 4f);

                int unitOfPointFive = (int)(value * 2);
                counts[unitOfPointFive]++;
            }

            float pointsUsable = 0;
            foreach (var count in counts)
            {
                float percent = (float)count / rolls;
                testOutputHelper.WriteLine($"{pointsUsable:F1}: {percent:P} ({count})");
                pointsUsable += 0.5f;
            }
        }
    }
}