namespace DarkLink.Util.JsonLd.Types;

public record TermMapping(Uri Id)
{
    public string? Container { get; init; }

    public Uri? Type { get; init; }

    public static implicit operator TermMapping(Uri id) => new(id);
}

public class ContextEntry : Dictionary<Uri, TermMapping> { }
