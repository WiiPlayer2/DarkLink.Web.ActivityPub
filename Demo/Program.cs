using System.Text.Json.Nodes;
using DarkLink.Text.Json.NewtonsoftJsonMapper;
using DarkLink.Util.JsonLd;
using DarkLink.Web.ActivityPub.Types;
using DarkLink.Web.ActivityPub.Types.Extended;
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

var poco = LinkedDataSerializer.Deserialize<Person>(compact, Constants.Context);
var node = LinkedDataSerializer.Serialize(poco, Constants.Context);

Console.WriteLine(node);
Console.WriteLine(LINE);

Console.WriteLine("done.");
