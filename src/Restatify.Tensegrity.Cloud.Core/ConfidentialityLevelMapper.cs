namespace Restatify.Tensegrity.Cloud;

public static class ConfidentialityLevelMapper
{
    public static string ToHiveLevel(string level)
    {
        return level.Trim().ToLowerInvariant() switch
        {
            "offen" => "OFFEN",
            "vertraulich" => "VERTRAULICH",
            "streng geheim" => "STRENG_GEHEIM",
            "nur lokal" => "NUR_LOKAL",
            _ => "OFFEN"
        };
    }
}
