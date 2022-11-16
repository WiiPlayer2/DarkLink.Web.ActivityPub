namespace DarkLink.Util.JsonLd.Types;

public abstract record LinkOr<T>
{
    internal LinkOr() { }

    public abstract TResult Match<TResult>(Func<Uri, TResult> onLink, Func<T, TResult> onObject);
}
