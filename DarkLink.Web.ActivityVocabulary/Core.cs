namespace DarkLink.Web.ActivityVocabulary;

public interface IEntity
{
    Uri? Type { get; }

    Uri? Id { get; }

    IReadOnlyDictionary<string, string> NameMap { get; }

    IReadOnlyList<ILinkOr<IObject>> Previews { get; }
}

public interface IObject : IEntity
{
    IReadOnlyList<ILinkOr<IObject>> Attachments { get; }
}

public interface ILink : IEntity
{
    Uri Href { get; }

    string? HrefLanguage { get; }

    IReadOnlyList<string> Relations { get; }

    string? MediaType { get; }

    int? Height { get; }

    int? Width { get; }
}

public interface ILinkOr<out TObject>
    where TObject : IObject
{
    TResult Match<TResult>(Func<TObject, TResult> onObject, Func<ILink, TResult> onLink);
}

public interface IActivity : IObject
{
    IReadOnlyList<ILinkOr<IObject>> Actors { get; }

    IReadOnlyList<ILinkOr<IObject>> Objects { get; }
}

public interface IIntransitiveActivity : IActivity { }

public interface ICollection : IObject
{
    int TotalItems { get; }

    IReadOnlyList<IEntity> Items { get; }

    IEntity? Current { get; }

    IEntity? First { get; }

    IEntity? Last { get; }
}

public interface IOrderedCollection : ICollection
{
    IReadOnlyList<IEntity> OrderedItems { get; }
}

public interface ICollectionPage : ICollection
{
    IEntity? PartOf { get; }

    IEntity? Next { get; }

    IEntity? Previous { get; }
}

public interface IOrderedCollectionPage : IOrderedCollection, ICollectionPage
{
    int StartIndex { get; }
}