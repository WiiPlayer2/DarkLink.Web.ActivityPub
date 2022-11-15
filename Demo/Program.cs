using DarkLink.Text.Json.NewtonsoftJsonMapper;
using DarkLink.Util.JsonLd;
using JsonLD.Core;
using Newtonsoft.Json.Linq;

const string LINE = "----------------------------------------";

//using var httpClient = new HttpClient();
//using var webFingerClient = new WebFingerClient(httpClient);
//var descriptor = await webFingerClient.GetResourceDescriptorAsync("tech.lgbt", new Uri("acct:wiiplayer2@tech.lgbt"));
//if (descriptor is null)
//    return;

//var selfLink = descriptor.Links.First(o => o.Relation == "self");
//var request = new HttpRequestMessage(HttpMethod.Get, selfLink.Href!)
//{
//    Headers =
//    {
//        Accept = {MediaTypeWithQualityHeaderValue.Parse(selfLink.Type),},
//    },
//};
//var personJson = await (await httpClient.SendAsync(request)).Content.ReadAsStringAsync();
//await File.WriteAllTextAsync("./person.json", personJson);

var personJson = await File.ReadAllTextAsync("./person.json");
var personCompact = JObject.Parse(personJson).Map();
var personExpanded = JsonLdProcessor.Expand(personCompact.Map()).First().Map()!;
Console.WriteLine(personCompact);
Console.WriteLine(LINE);
Console.WriteLine(personExpanded);
Console.WriteLine(LINE);

var serializer = new JsonLdSerializer();
var person = serializer.Deserialize<Person>(personCompact);

Console.WriteLine("done.");

[LinkedData("https://www.w3.org/ns/activitystreams")]
public record Person(
    Uri Id,
    IReadOnlyList<Uri> Type,
    string Summary,
    IReadOnlyList<object> Attachment);
