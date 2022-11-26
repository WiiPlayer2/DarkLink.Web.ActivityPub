using System.Reflection;
using DarkLink.Util.JsonLd.Attributes;
using DarkLink.Util.JsonLd.Types;

namespace DarkLink.Util.JsonLd.Converters;

public interface ILinkedDataConverter
{
    bool CanConvert(Type typeToConvert);

    object? Convert(DataList<LinkedData> dataList, Type typeToConvert, LinkedDataSerializationOptions options);

    DataList<LinkedData> ConvertBack(object? value, LinkedDataSerializationOptions options);
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

    public DataList<LinkedData> ConvertBack(object? value, LinkedDataSerializationOptions options) => throw new NotImplementedException();
}

public abstract class LinkedDataConverter<T> : ILinkedDataConverter
    where T : notnull
{
    public bool CanConvert(Type typeToConvert) => typeToConvert == typeof(T);

    public object? Convert(DataList<LinkedData> dataList, Type typeToConvert, LinkedDataSerializationOptions options) => Convert(dataList, options);

    public DataList<LinkedData> ConvertBack(object? value, LinkedDataSerializationOptions options) => ConvertBack((T) value!, options);

    protected abstract T? Convert(DataList<LinkedData> dataList, LinkedDataSerializationOptions options);

    protected abstract DataList<LinkedData> ConvertBack(T? value, LinkedDataSerializationOptions options);
}

public abstract class LinkedDataConverterFactory : ILinkedDataConverter
{
    public abstract bool CanConvert(Type typeToConvert);

    public object? Convert(DataList<LinkedData> dataList, Type typeToConvert, LinkedDataSerializationOptions options)
        => CreateConverter(typeToConvert, options)?.Convert(dataList, typeToConvert, options);

    public DataList<LinkedData> ConvertBack(object? value, LinkedDataSerializationOptions options)
        => CreateConverter(value?.GetType() ?? typeof(object), options)?.ConvertBack(value, options) ?? default;

    protected abstract ILinkedDataConverter? CreateConverter(Type typeToConvert, LinkedDataSerializationOptions options);
}

internal class StringConverter : LinkedDataConverter<string>
{
    protected override string? Convert(DataList<LinkedData> dataList, LinkedDataSerializationOptions options)
        => dataList.Value?.Value?.GetValue<string>();

    protected override DataList<LinkedData> ConvertBack(string? value, LinkedDataSerializationOptions options) => throw new NotImplementedException();
}

internal class UriConverter : LinkedDataConverter<Uri>
{
    protected override Uri? Convert(DataList<LinkedData> dataList, LinkedDataSerializationOptions options) => dataList.Value?.Id;

    protected override DataList<LinkedData> ConvertBack(Uri? value, LinkedDataSerializationOptions options) => throw new NotImplementedException();
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

        protected override DataList<LinkedData> ConvertBack(TEnumerable? value, LinkedDataSerializationOptions options) => throw new NotImplementedException();
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
