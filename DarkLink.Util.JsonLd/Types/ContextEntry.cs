namespace DarkLink.Util.JsonLd.Types;

public record TermMapping(Uri Id, Uri? Type = default)
{
    public static implicit operator TermMapping(Uri id) => new(id);
}

public class ContextEntry : Dictionary<Uri, TermMapping> { }
