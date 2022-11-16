using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using DarkLink.Text.Json.NewtonsoftJsonMapper;
using DarkLink.Util.JsonLd;
using DarkLink.Util.JsonLd.Types;
using JsonLD.Core;
using Newtonsoft.Json.Linq;

const string LINE = "----------------------------------------";

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
var recompacted = expanded.Compact(new JsonObject());
Console.WriteLine(compact);
Console.WriteLine(LINE);
Console.WriteLine(expanded);
Console.WriteLine(LINE);
Console.WriteLine(recompacted);
Console.WriteLine(LINE);

var serializer = new JsonLdSerializer();
var poco = serializer.Deserialize<Person>(compact);
var node = serializer.Serialize(poco);

Console.WriteLine(node);
Console.WriteLine(LINE);

Console.WriteLine("done.");

[LinkedData("https://www.w3.org/ns/activitystreams#")]
public record OrderedCollection(
    Uri Id,
    Typed<int> TotalItems,
    LinkOr<object> First);

[LinkedData("https://www.w3.org/ns/activitystreams#")]
public record Person(
    Uri Id,
    IReadOnlyList<Uri> Type,
    string Summary,
    [property: JsonPropertyName("http://joinmastodon.org/ns#featured")]
    IReadOnlyList<object> Featured);

[LinkedData]
public record Typed<T>(Uri Type, T Value);
