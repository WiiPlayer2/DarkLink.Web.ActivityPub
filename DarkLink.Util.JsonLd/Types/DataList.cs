using System.Collections;

namespace DarkLink.Util.JsonLd.Types;

public static class DataList
{
    public static DataList<T> From<T>(T? value)
        => new(value == null ? Array.Empty<T>() : new[] {value,});

    public static DataList<T> FromItems<T>(IEnumerable<T> values)
        => new(values.ToList());
}

public class DataList<T> : IReadOnlyList<T>
{
    private readonly IReadOnlyList<T> items;

    public DataList(IReadOnlyList<T> items)
    {
        this.items = items;
    }

    public T? Value
    {
        get
        {
            if (Count > 1)
                throw new InvalidOperationException("Access is only allowed for single or no items.");
            return Count == 1 ? items[0] : default;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IEnumerator<T> GetEnumerator() => items.GetEnumerator();

    public int Count => items.Count;

    public T this[int index] => items[index];
}
