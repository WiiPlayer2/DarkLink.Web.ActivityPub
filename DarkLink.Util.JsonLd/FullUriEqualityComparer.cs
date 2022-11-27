namespace DarkLink.Util.JsonLd;

public class FullUriEqualityComparer : EqualityComparer<Uri>
{
    public new static FullUriEqualityComparer Default { get; } = new();

    public override bool Equals(Uri? x, Uri? y)
        => object.Equals(x, y) && Equals(x?.Fragment, y?.Fragment);

    public override int GetHashCode(Uri obj) => obj.GetHashCode() ^ obj.Fragment.GetHashCode();
}
