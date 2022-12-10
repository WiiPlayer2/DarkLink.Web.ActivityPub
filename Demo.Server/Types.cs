using System.Text.Json.Serialization;

namespace Demo.Server;

internal record PersonData(string Name, string? Summary);

internal record NoteData(string Content, DateTimeOffset? Published);

internal record DeviceRegistrationRequest(
    [property: JsonPropertyName("client_name")]
    string ClientName,
    [property: JsonPropertyName("redirect_uris")]
    string RedirectUris,
    [property: JsonPropertyName("scopes")] string Scopes,
    [property: JsonPropertyName("website")]
    Uri Website);
