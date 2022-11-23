using DarkLink.Util.JsonLd.Attributes;

namespace DarkLink.Util.JsonLd.Types;

[ContextProxy(ProxyTypeResolver = typeof(LinkOrContextProxyResolver), IgnoreProperties = true)]
public abstract record LinkOr<T>
{
    internal LinkOr() { }

    public abstract TResult Match<TResult>(Func<Uri, TResult> onLink, Func<T, TResult> onObject);
}

internal class LinkOrContextProxyResolver : IContextProxyResolver
{
    public IEnumerable<Type> ResolveProxyTypes(Type proxiedType) => new[] {proxiedType};
}
