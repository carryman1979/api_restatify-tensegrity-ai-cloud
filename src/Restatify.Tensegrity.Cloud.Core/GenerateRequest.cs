namespace Restatify.Tensegrity.Cloud;

/// <summary>Cloud inference input carrying its confidentiality level and optional expert slots.</summary>
public sealed record GenerateRequest(
    string RequestId,
    string ConfidentialityLevel,
    string Prompt,
    IReadOnlyList<string>? ExpertSlotIds = null);
