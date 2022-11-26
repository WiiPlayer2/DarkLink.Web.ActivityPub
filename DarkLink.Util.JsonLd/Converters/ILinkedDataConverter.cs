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
