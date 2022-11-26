namespace DarkLink.Util.JsonLd.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public sealed class LinkedDataPropertyAttribute : Attribute
{
    public LinkedDataPropertyAttribute(string iri)
    {
        Iri = new Uri(iri);
    }

    public Uri Iri { get; }
}
