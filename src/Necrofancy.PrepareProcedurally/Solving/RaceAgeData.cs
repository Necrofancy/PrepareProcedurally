using System;
using Verse;

namespace Necrofancy.PrepareProcedurally.Solving;

public struct RaceAgeData : IExposable, IEquatable<RaceAgeData>
{
    public RaceAgeData(IntRange desiredAgeRange, IntRange possibleAgeRange)
    {
        this.desiredAgeRange = desiredAgeRange;
        this.possibleAgeRange = possibleAgeRange;
    }

    public IntRange desiredAgeRange;
    public IntRange possibleAgeRange;

    public RaceAgeData WithUpdatedAge(IntRange newRange) => new RaceAgeData(newRange, possibleAgeRange);
    
    public void ExposeData()
    {
        Scribe_Values.Look(ref desiredAgeRange, "desiredAgeRange");
        Scribe_Values.Look(ref possibleAgeRange, "possibleAgeRange");
    }

    public bool Equals(RaceAgeData other)
    {
        return desiredAgeRange.Equals(other.desiredAgeRange) && possibleAgeRange.Equals(other.possibleAgeRange);
    }

    public override bool Equals(object obj)
    {
        return obj is RaceAgeData other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (desiredAgeRange.GetHashCode() * 397) ^ possibleAgeRange.GetHashCode();
        }
    }
}