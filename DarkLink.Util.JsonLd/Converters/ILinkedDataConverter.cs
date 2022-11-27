using System.Reflection;
using System.Text.Json.Nodes;
using DarkLink.Util.JsonLd.Attributes;
using DarkLink.Util.JsonLd.Types;

namespace DarkLink.Util.JsonLd.Converters;

public interface ILinkedDataConverter
{
    bool CanConvert(Type typeToConvert);

    object? Convert(DataList<LinkedData> dataList, Type typeToConvert, LinkedDataSerializationOptions options);

    DataList<LinkedData> ConvertBack(object? value, Type typeToConvert, LinkedDataSerializationOptions options);
}

internal class ObjectConverter : ILinkedDataConverter
{
    public bool CanConvert(Type typeToConvert) => !typeToConvert.IsPrimitive && typeToConvert != typeof(string);

    public object? Convert(DataList<LinkedData> dataList, Type typeToConvert, LinkedDataSerializationOptions options)
    {
        var data = dataList.Value;
        if (data is null) return null;

        var obj = Activator.CreateInstance(typeToConvert)!;
        foreach (var property in typeToConvert.GetProperties())
        {
            if (property.Name.Equals("id", StringComparison.InvariantCultureIgnoreCase))
            {
                property.SetValue(obj, data.Id);
                continue;
            }

            if (property.Name.Equals("type", StringComparison.CurrentCultureIgnoreCase))
            {
                property.SetValue(obj, data.Type);
                continue;
            }

            var linkedDataProperty = property.GetCustomAttribute<LinkedDataPropertyAttribute>() ?? throw new InvalidOperationException();
            var propertyData = data[linkedDataProperty.Iri];
            var convertedValue = LinkedDataSerializer.Deserialize2(DataList.FromItems(propertyData), property.PropertyType, options);
            property.SetValue(obj, convertedValue);
        }

        return obj;
    }

    public DataList<LinkedData> ConvertBack(object? value, Type typeToConvert, LinkedDataSerializationOptions options)
    {
        var valueType = value?.GetType() ?? typeToConvert;
        if (value is null)
            return default;

        var data = new LinkedData
        {
            Type = DataList.FromItems(valueType.GetCustomAttributes<LinkedDataTypeAttribute>()
                .Select(attr => attr.Type)),
        };
        var properties = new Dictionary<Uri, DataList<LinkedData>>(UriEqualityComparer.Default);
        foreach (var property in valueType.GetProperties())
        {
            if (property.Name.Equals("id", StringComparison.InvariantCultureIgnoreCase))
            {
                data = data with
                {
                    Id = (Uri?) property.GetValue(value),
                };
                continue;
            }

            if (property.Name.Equals("type", StringComparison.CurrentCultureIgnoreCase))
            {
                data = data with
                {
                    Type = (DataList<Uri>) property.GetValue(value)!,
                };
                continue;
            }

            var linkedDataProperty = property.GetCustomAttribute<LinkedDataPropertyAttribute>() ?? throw new InvalidOperationException();
            var propertyValue = property.GetValue(value);
            var propertyData = LinkedDataSerializer.Serialize2(propertyValue, property.PropertyType, options);
            properties.Add(linkedDataProperty.Iri, propertyData);
        }

        return data with
        {
            Properties = properties,
        };
    }
}

public abstract class LinkedDataConverter<T> : ILinkedDataConverter
    where T : notnull
{
    public bool CanConvert(Type typeToConvert) => typeToConvert == typeof(T);

    public object? Convert(DataList<LinkedData> dataList, Type typeToConvert, LinkedDataSerializationOptions options) => Convert(dataList, options);

    public DataList<LinkedData> ConvertBack(object? value, Type typeToConvert, LinkedDataSerializationOptions options) => ConvertBack((T) value!, options);

    protected abstract T? Convert(DataList<LinkedData> dataList, LinkedDataSerializationOptions options);

    protected abstract DataList<LinkedData> ConvertBack(T? value, LinkedDataSerializationOptions options);
}

public abstract class LinkedDataConverterFactory : ILinkedDataConverter
{
    public abstract bool CanConvert(Type typeToConvert);

    public object? Convert(DataList<LinkedData> dataList, Type typeToConvert, LinkedDataSerializationOptions options)
        => CreateConverter(typeToConvert, options)?.Convert(dataList, typeToConvert, options);

    public DataList<LinkedData> ConvertBack(object? value, Type typeToConvert, LinkedDataSerializationOptions options)
        => CreateConverter(typeToConvert, options)?.ConvertBack(value, typeToConvert, options) ?? default;

    protected abstract ILinkedDataConverter? CreateConverter(Type typeToConvert, LinkedDataSerializationOptions options);
}

internal class StringConverter : LinkedDataConverter<string>
{
    protected override string? Convert(DataList<LinkedData> dataList, LinkedDataSerializationOptions options)
        => dataList.Value?.Value?.GetValue<string>();

    protected override DataList<LinkedData> ConvertBack(string? value, LinkedDataSerializationOptions options)
        => value is null
            ? default
            : new LinkedData
            {
                Value = JsonValue.Create(value),
            };
}

internal class UriConverter : LinkedDataConverter<Uri>
{
    protected override Uri? Convert(DataList<LinkedData> dataList, LinkedDataSerializationOptions options)
        => dataList.Value?.Id;

    protected override DataList<LinkedData> ConvertBack(Uri? value, LinkedDataSerializationOptions options)
        => value is null ? default : new LinkedData {Id = value};
}

internal class EnumerableConverter : LinkedDataConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
        => typeToConvert.GetInterfaces()
            .Where(o => o.IsGenericType)
            .Select(o => o.GetGenericTypeDefinition())
            .Contains(typeof(IEnumerable<>));

    protected override ILinkedDataConverter? CreateConverter(Type typeToConvert, LinkedDataSerializationOptions options)
    {
        var itemType = typeToConvert.GetInterfaces()
            .First(o => o.IsGenericType && o.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            .GenericTypeArguments[0];
        return typeof(Conv<,>).Create<ILinkedDataConverter>(itemType, typeToConvert);
    }

    private class Conv<TItem, TEnumerable> : LinkedDataConverter<TEnumerable>
        where TEnumerable : IEnumerable<TItem>
    {
        private readonly Func<IEnumerable<TItem>, TEnumerable> create;

        public Conv()
        {
            create = Map<TItem>.Get<TEnumerable>();
        }

        protected override TEnumerable? Convert(DataList<LinkedData> dataList, LinkedDataSerializationOptions options)
        {
            var sequence = dataList.Select(data => LinkedDataSerializer.Deserialize2<TItem>(data) ?? throw new InvalidOperationException());
            var enumerable = create(sequence);
            return enumerable;
        }

        protected override DataList<LinkedData> ConvertBack(TEnumerable? value, LinkedDataSerializationOptions options)
        {
            var list = (value ?? Enumerable.Empty<TItem>())
                .Select(item => LinkedDataSerializer.Serialize2(item, typeof(TItem), options).Value!)
                .ToList();
            return DataList.FromItems(list);
        }
    }

    private static class Map<TItem>
    {
        public static Func<IEnumerable<TItem>, TEnumerable> Get<TEnumerable>()
            where TEnumerable : IEnumerable<TItem>
        {
            if (typeof(TEnumerable) == typeof(IReadOnlyList<TItem>))
                return sequence => (TEnumerable) (object) new List<TItem>(sequence);

            throw new NotImplementedException();
        }
    }
}
