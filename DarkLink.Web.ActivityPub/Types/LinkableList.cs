using System.Collections;
using DarkLink.Util.JsonLd.Types;

namespace DarkLink.Web.ActivityPub.Types;

public readonly struct LinkableList<T> : IReadOnlyList<LinkTo<T>>
    where T : Object
{
    private readonly IReadOnlyList<LinkTo<T>>? items;

    public LinkableList(IReadOnlyList<LinkTo<T>> items)
    {
        this.items = items;
    }

    private IReadOnlyList<LinkTo<T>> Items => items ?? Array.Empty<LinkTo<T>>();

    public LinkTo<T>? Value
    {
        get
        {
            if (Count > 1)
                throw new InvalidOperationException("Access is only allowed for single or no items.");
            return Count == 1 ? Items[0] : default;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IEnumerator<LinkTo<T>> GetEnumerator() => Items.GetEnumerator();

    public int Count => Items.Count;

    public LinkTo<T> this[int index] => Items[index];

    public static implicit operator LinkableList<T>(T? value) => (LinkTo<T>?) value;

    public static implicit operator LinkableList<T>(Uri iri) => (LinkTo<T>) iri;

    public static implicit operator LinkableList<T>(LinkTo<T>? value)
        => value is null
            ? default
            : new LinkableList<T>(new[] {value});

    public static implicit operator LinkableList<T>(DataList<LinkTo<T>> list)
        => new(list);

    public static implicit operator DataList<LinkTo<T>>(LinkableList<T> list)
        => new(list);
}
