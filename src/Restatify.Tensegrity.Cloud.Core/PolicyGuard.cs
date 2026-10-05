namespace Restatify.Tensegrity.Cloud;

/// <summary>
/// Central cloud policy gate for inbound generation requests.
/// </summary>
/// <remarks>
/// Coverage intent: the public API is intentionally tiny and deterministic. The only non-trivial branch is the
/// empty-input guard, which is treated as a rejected request by policy. No broader exceptions are permitted unless
/// the business rules for the gateway change and this documentation is updated in the same code review.
/// </remarks>
public static class PolicyGuard
{
    public static bool IsRequestAllowed(string? confidentialityLevel)
    {
        if (string.IsNullOrWhiteSpace(confidentialityLevel))
        {
            return false;
        }

        var normalized = confidentialityLevel.Trim();

        return normalized.Equals("Offen", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("Vertraulich", StringComparison.OrdinalIgnoreCase);
    }
}
