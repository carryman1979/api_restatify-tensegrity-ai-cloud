using Microsoft.AspNetCore.Http;

namespace Restatify.Tensegrity.Cloud;

public static class GatewayRouter
{
    public static async Task<IResult> HandleAsync(
        GenerateRequest? request,
        IHiveClient hiveClient,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return Results.BadRequest(new { error = "Request body is required." });
        }

        if (!PolicyGuard.IsRequestAllowed(request.ConfidentialityLevel))
        {
            return Results.BadRequest(new
            {
                error = "Request rejected: STRONG_SECRET and NUR_LOKAL are not allowed on the cloud gateway.",
                allowed = false,
                level = request.ConfidentialityLevel,
                requestId = request.RequestId
            });
        }

        var normalizedLevel = request.ConfidentialityLevel.Trim();
        var isConfidential = normalizedLevel.Equals("Vertraulich", StringComparison.OrdinalIgnoreCase);
        var requestedSlotIds = request.ExpertSlotIds?
            .Where(slotId => !string.IsNullOrWhiteSpace(slotId))
            .Select(slotId => slotId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray() ?? [];
        var resolvedSlotIds = SlotRegistry.Resolve(requestedSlotIds).ToArray();
        var unknownSlotIds = requestedSlotIds.Except(resolvedSlotIds, StringComparer.Ordinal).ToArray();

        if (unknownSlotIds.Length > 0)
        {
            return Results.BadRequest(new
            {
                error = "Request rejected: unknown or inactive expert slot(s).",
                allowed = false,
                requestId = request.RequestId,
                unknownSlotIds
            });
        }

        var hasExperts = resolvedSlotIds.Length > 0;
        var route = (isConfidential, hasExperts) switch
        {
            (true, true) => "generalist-with-redaction-and-experts",
            (true, false) => "generalist-with-redaction",
            (false, true) => "generalist-with-experts",
            _ => "generalist"
        };

        var hiveResult = await hiveClient.SubmitAsync(request, cancellationToken);

        return Results.Ok(new
        {
            requestId = request.RequestId,
            level = normalizedLevel,
            allowed = true,
            route,
            selectedExpertSlotIds = resolvedSlotIds,
            promptLength = request.Prompt?.Length ?? 0,
            hiveAccepted = hiveResult.Accepted,
            hiveResolvedSlots = hiveResult.ResolvedSlots
        });
    }
}
