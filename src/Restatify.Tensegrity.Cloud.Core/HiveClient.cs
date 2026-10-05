using System.Net.Http.Json;
using System.Text.Json;

namespace Restatify.Tensegrity.Cloud;

public sealed class HiveClient : IHiveClient
{
    private static readonly HttpClient Http = new()
    {
        BaseAddress = new Uri("http://127.0.0.1:50051")
    };

    // Coverage exception: this method is pure HTTP transport against a running Hive process.
    // It is verified by the smoke tests (tests/smoke), not by unit/component tests.
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public async Task<HiveSubmitResult> SubmitAsync(GenerateRequest request, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            device_id = request.RequestId,
            app_instance_id = "cloud-gateway",
            trust_score = 0.8,
            level = ConfidentialityLevelMapper.ToHiveLevel(request.ConfidentialityLevel),
            slots = request.ExpertSlotIds ?? Array.Empty<string>(),
            source = "cloud"
        };

        using var response = await Http.PostAsJsonAsync("/submit", payload, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var root = document.RootElement;
        var accepted = root.GetProperty("accepted").GetBoolean();
        var resolvedSlots = root.GetProperty("resolved_slots").EnumerateArray()
            .Select(slot => slot.GetString()!)
            .ToArray();
        var trustScore = root.GetProperty("trust_score").GetDouble();

        return new HiveSubmitResult(accepted, resolvedSlots, trustScore);
    }
}
