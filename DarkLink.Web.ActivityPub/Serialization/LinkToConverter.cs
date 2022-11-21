using DarkLink.Util.JsonLd.Types;
using DarkLink.Web.ActivityPub.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Object = DarkLink.Web.ActivityPub.Types.Object;

namespace DarkLink.Web.ActivityPub.Serialization
{
    public class LinkToConverter : JsonConverterFactory
    {
        private LinkToConverter() { }

        public static LinkToConverter Instance { get; } = new();

        public override bool CanConvert(Type typeToConvert) => typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(LinkTo<>);

        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var itemType = typeToConvert.GenericTypeArguments[0];
            var converterType = typeof(Conv<>).MakeGenericType(itemType);
            var converter = (JsonConverter)Activator.CreateInstance(converterType)!;
            return converter;
        }

        private class Conv<T> : JsonConverter<LinkTo<T>>
            where T : Object
        {
            public override LinkTo<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                throw new NotImplementedException();
            }

            public override void Write(Utf8JsonWriter writer, LinkTo<T> value, JsonSerializerOptions options)
            {
                throw new NotImplementedException();
            }
        }
    }
}
