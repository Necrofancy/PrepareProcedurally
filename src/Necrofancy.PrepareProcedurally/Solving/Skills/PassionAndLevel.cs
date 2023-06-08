using RimWorld;

namespace Necrofancy.PrepareProcedurally.Solving.Skills
{
    public readonly struct PassionAndLevel
    {
        public PassionAndLevel(int min, int max, Passion passion)
        {
            Min = min;
            Max = max;
            Passion = passion;
        }

        public int Min { get; }
        public int Max { get; }
        public Passion Passion { get; }

        public override string ToString()
        {
            return Min == Max
                ? $"{Min}{Passion.AsFireEmojis()}"
                : $"{Min}-{Max}{Passion.AsFireEmojis()}";
        }
    }
}