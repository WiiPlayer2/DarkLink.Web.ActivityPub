using DarkLink.Text.Json.NewtonsoftJsonMapper;
using DarkLink.Util.JsonLd;
using DarkLink.Web.ActivityPub.Serialization;
using DarkLink.Web.ActivityPub.Types.Extended;
using JsonLD.Core;
using Newtonsoft.Json.Linq;
using Constants = DarkLink.Web.ActivityPub.Types.Constants;

const string LINE = "----------------------------------------";
var linkedDataOptions = new LinkedDataSerializationOptions
{
    Converters =
    {
        new LinkToConverter2(),
    },
    JsonSerializerOptions =
    {
        Converters =
        {
            LinkToConverter.Instance,
        },
    },
};

//using var httpClient = new HttpClient();
//var request = new HttpRequestMessage(HttpMethod.Get, "https://tech.lgbt/users/wiiplayer2/outbox")
//{
//    Headers =
//    {
//        Accept = {MediaTypeWithQualityHeaderValue.Parse("application/json"),},
//    },
//};
//var json = await (await httpClient.SendAsync(request)).Content.ReadAsStringAsync();
//await File.WriteAllTextAsync("./outbox.json", json);

var json = await File.ReadAllTextAsync("./person.json");
var compact = JObject.Parse(json).Map();
var expanded = JsonLdProcessor.Expand(compact.Map()).First().Map()!;
//var recompacted = expanded.Compact(new JsonObject());
//Console.WriteLine(compact);
//Console.WriteLine(LINE);
Console.WriteLine(expanded);
Console.WriteLine(LINE);
//Console.WriteLine(recompacted);
//Console.WriteLine(LINE);

//var poco = LinkedDataSerializer.Deserialize<Person>(compact, Constants.Context, jsonOptions);
//var node = LinkedDataSerializer.Serialize(poco, Constants.Context, jsonOptions);

//Console.WriteLine(node);
//Console.WriteLine(LINE);

var ld = LinkedDataSerializer.DeserializeLinkedData(compact, linkedDataOptions.JsonSerializerOptions)!;
//var ldNode = LinkedDataSerializer.SerializeLinkedData(ld, jsonOptions);
//var compactLdNode = ldNode.Compact(LinkedDataSerializer.SerializeContext(Constants.Context)!);

//Console.WriteLine(ldNode);
//Console.WriteLine(LINE);
//Console.WriteLine(compactLdNode);
//Console.WriteLine(LINE);

var myPerson = LinkedDataSerializer.DeserializeFromLinkedData<Person>(ld, linkedDataOptions);
var myNode = LinkedDataSerializer.Serialize2(myPerson, context: Constants.Context, options: linkedDataOptions);

Console.WriteLine(myNode);
Console.WriteLine(LINE);

Console.WriteLine("done.");
