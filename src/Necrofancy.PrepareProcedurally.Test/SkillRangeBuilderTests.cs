using System.Collections.Generic;
using Necrofancy.PrepareProcedurally.Test.SkillLockIn;
using RimWorld;
using VerifyXunit;
using Xunit;
using Xunit.Abstractions;
using System.Threading.Tasks;

namespace Necrofancy.PrepareProcedurally.Test;

using static StaticData;

[UsesVerify]
public class SkillRangeBuilderTests
{
    private readonly ITestOutputHelper testOutputHelper;

    public SkillRangeBuilderTests(ITestOutputHelper testOutputHelper)
    {
        this.testOutputHelper = testOutputHelper;
    }

    [Fact]
    public Task SetMedical()
    {
        var builder = StringSkillBuilder.Make(AllBaseline);
        builder.TryLockInPassion(Medical, Passion.Major);
        return Verifier.Verify(builder.GetFinalResult());
    }

    [Fact]
    public Task TribeTender()
    {
        var builder = StringSkillBuilder.Make(TribeChildTender);
        builder.TryLockInPassion(Shooting, Passion.Major);
        builder.TryLockInPassion(Construction, Passion.Minor);
        builder.TryLockInPassion(Cooking, Passion.Major);
        builder.TryLockInPassion(Animals, Passion.Major);
        builder.TryLockInPassion(Social, Passion.Minor);
        return Verifier.Verify(builder.GetFinalResult());
    }

    [Fact]
    public Task TribeTenderTorturedArtist()
    {
        var forcedPassion = new HashSet<string> { Artistic };
        var builder = StringSkillBuilder.Make(TribeChildTender, forcedPassion);
        builder.TryLockInPassion(Shooting, Passion.Major);
        builder.TryLockInPassion(Construction, Passion.Minor);
        builder.TryLockInPassion(Cooking, Passion.Major);
        builder.TryLockInPassion(Animals, Passion.Major);
        builder.TryLockInPassion(Social, Passion.Minor);
        return Verifier.Verify(builder.GetFinalResult());
    }
}