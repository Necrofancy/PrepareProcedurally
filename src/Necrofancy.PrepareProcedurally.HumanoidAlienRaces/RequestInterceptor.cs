using JetBrains.Annotations;

namespace Necrofancy.PrepareProcedurally.HumanoidAlienRaces;

public class RequestInterceptor
{
    public static RequestInterceptor Instance { get; } = new ();

    public Func<PawnGenerationRequest, PawnGenerationRequest> Transform { get; [UsedImplicitly]set; }

    public void Intercept(ref PawnGenerationRequest request) => request = Transform?.Invoke(request) ?? request;
}
