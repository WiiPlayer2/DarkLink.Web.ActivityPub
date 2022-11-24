using DarkLink.Util.JsonLd.Attributes;

namespace DarkLink.Util.JsonLd.Types;

[ContextProxy(ProxyTypeResolver = typeof(LinkOrContextProxyResolver), IgnoreProperties = true)]
public abstract record LinkOr<T>
{
    internal LinkOr() { }

    public abstract TResult Match<TResult>(Func<Uri, TResult> onLink, Func<T, TResult> onObject);

    public static implicit operator LinkOr<T>(Uri iri)
        => new Link<T>(iri);

    public static implicit operator LinkOr<T>?(T? obj)
        => obj is null ? default : new Object<T>(obj);
}

internal class LinkOrContextProxyResolver : IContextProxyResolver
{
    public IEnumerable<Type> ResolveProxyTypes(Type proxiedType) => new[] {proxiedType};
}
