namespace DarkLink.Web.ActivityPub.Types;

public abstract record LinkTo<TObject>()
    where TObject : Object
{
    private record Ref(Link Link) : LinkTo<TObject>()
    {
        public override T Match<T>(Func<Link, T> onLink, Func<TObject, T> onObject) => onLink(Link);
    }

    private record Obj(TObject Object) : LinkTo<TObject>()
    {
        public override T Match<T>(Func<Link, T> onLink, Func<TObject, T> onObject) => onObject(Object);
    }

    public abstract T Match<T>(Func<Link, T> onLink, Func<TObject, T> onObject);

    public static implicit operator LinkTo<TObject>(Uri iri) => new Link { Id = iri };

    public static implicit operator LinkTo<TObject>(Link link) => new Ref(link);

    public static implicit operator LinkTo<TObject>(TObject obj) => new Obj(obj);
}
