using System.Collections;

namespace DarkLink.Util.JsonLd.Types;

public readonly struct LinkedDataList<T> : IReadOnlyList<LinkOr<T>>
{
    private readonly IReadOnlyList<LinkOr<T>>? items;

    public LinkedDataList(IReadOnlyList<LinkOr<T>> items)
    {
        this.items = items;
    }

    private IReadOnlyList<LinkOr<T>> Items => items ?? Array.Empty<LinkOr<T>>();

    public LinkOr<T>? Value
    {
        get
        {
            if (Count > 1)
                throw new InvalidOperationException("Access is only allowed for single or no items.");
            return Count == 1 ? Items[0] : default;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IEnumerator<LinkOr<T>> GetEnumerator() => Items.GetEnumerator();

    public int Count => Items.Count;

    public LinkOr<T> this[int index] => Items[index];

    public static implicit operator LinkedDataList<T>(T? value) => (LinkOr<T>?) value;

    public static implicit operator LinkedDataList<T>(Uri iri) => (LinkOr<T>) iri;

    public static implicit operator LinkedDataList<T>(LinkOr<T>? value)
        => value is null
            ? default
            : new LinkedDataList<T>(new[] {value});

    public static implicit operator LinkedDataList<T>(DataList<LinkOr<T>> list)
        => new(list);

    public static implicit operator DataList<LinkOr<T>>(LinkedDataList<T> list)
        => new(list);
}
