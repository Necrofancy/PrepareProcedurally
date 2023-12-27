using Verse;

namespace Necrofancy.PrepareProcedurally.Solving
{
    public readonly struct RaceAgeData
    {
        public RaceAgeData(IntRange ageRange, IntRange allowedAgeRange)
        {
            AgeRange = ageRange;
            AllowedAgeRange = allowedAgeRange;
        }

        public IntRange AgeRange { get; }
        public IntRange AllowedAgeRange { get; }

        public RaceAgeData WithUpdatedAge(IntRange newRange) => new RaceAgeData(newRange, AllowedAgeRange);
    }
}