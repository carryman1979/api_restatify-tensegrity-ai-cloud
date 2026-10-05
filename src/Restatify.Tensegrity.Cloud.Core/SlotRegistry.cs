namespace Restatify.Tensegrity.Cloud;

public static class SlotRegistry
{
    private static readonly IReadOnlyDictionary<string, SlotInfo> Slots = new Dictionary<string, SlotInfo>(StringComparer.Ordinal)
    {
        ["legal"] = new SlotInfo("legal", "Recht", true),
        ["finance"] = new SlotInfo("finance", "Finanzen", true),
        ["medical"] = new SlotInfo("medical", "Medizin", false),
        ["engineering"] = new SlotInfo("engineering", "Technik", true)
    };

    public static IReadOnlyList<string> Resolve(IEnumerable<string>? requestedSlotIds)
    {
        if (requestedSlotIds is null)
        {
            return [];
        }

        return requestedSlotIds
            .Where(slotId => !string.IsNullOrWhiteSpace(slotId))
            .Select(slotId => slotId.Trim())
            .Where(slotId => Slots.TryGetValue(slotId, out var slot) && slot.IsActive)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(slotId => slotId, StringComparer.Ordinal)
            .ToArray();
    }

    public static bool IsKnownActiveSlot(string slotId)
    {
        return Slots.TryGetValue(slotId, out var slot) && slot.IsActive;
    }

    public sealed record SlotInfo(string SlotId, string Category, bool IsActive);
}
