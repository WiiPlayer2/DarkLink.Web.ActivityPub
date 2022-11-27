namespace DarkLink.Util.JsonLd;

internal class UriEqualityComparer : EqualityComparer<Uri>
{
    public new static UriEqualityComparer Default { get; } = new();

    public override bool Equals(Uri? x, Uri? y)
        => object.Equals(x, y) && Equals(x?.Fragment, y?.Fragment);

    public override int GetHashCode(Uri obj) => obj.GetHashCode() ^ obj.Fragment.GetHashCode();
}
