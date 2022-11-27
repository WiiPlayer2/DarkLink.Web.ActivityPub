using DarkLink.Util.JsonLd.Types;

namespace DarkLink.Util.JsonLd.Converters;

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
            var sequence = dataList.Select(data => LinkedDataSerializer.DeserializeFromLinkedData<TItem>(data) ?? throw new InvalidOperationException());
            var enumerable = create(sequence);
            return enumerable;
        }

        protected override DataList<LinkedData> ConvertBack(TEnumerable? value, LinkedDataSerializationOptions options)
        {
            var list = (value ?? Enumerable.Empty<TItem>())
                .Select(item => LinkedDataSerializer.SerializeToLinkedData(item, typeof(TItem), options).Value!)
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
