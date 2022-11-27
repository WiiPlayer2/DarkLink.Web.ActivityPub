using System.Collections;

namespace DarkLink.Util.JsonLd.Types;

public static class DataList
{
    public static DataList<T> From<T>(T? value)
        => new(value == null ? Array.Empty<T>() : new[] {value});

    public static DataList<T> FromItems<T>(IEnumerable<T> values)
        => new(values.ToList());

    public static LinkedDataList<T> FromItems<T>(IEnumerable<LinkOr<T>> values)
        => new(values.ToList());
}

public readonly struct DataList<T> : IReadOnlyList<T>
{
    private readonly IReadOnlyList<T>? items;

    public DataList(IReadOnlyList<T> items)
    {
        this.items = items;
    }

    public bool IsEmpty => Count == 0;

    public bool IsValue => Count == 1;

    private IReadOnlyList<T> Items => items ?? Array.Empty<T>();

    public T? Value
    {
        get
        {
            if (Count > 1)
                throw new InvalidOperationException("Access is only allowed for single or no items.");
            return Count == 1 ? Items[0] : default;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IEnumerator<T> GetEnumerator() => Items.GetEnumerator();

    public int Count => Items.Count;

    public T this[int index] => Items[index];

    public static implicit operator DataList<T>(T? value) => DataList.From(value);

    private string ToDebuggerString() => $"[{string.Join(", ", this)}]";
}
