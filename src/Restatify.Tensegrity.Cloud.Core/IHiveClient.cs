namespace Restatify.Tensegrity.Cloud;

public interface IHiveClient
{
    Task<HiveSubmitResult> SubmitAsync(GenerateRequest request, CancellationToken cancellationToken = default);
}

public sealed record HiveSubmitResult(bool Accepted, IReadOnlyList<string> ResolvedSlots, double TrustScore);
