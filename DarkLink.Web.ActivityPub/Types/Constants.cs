﻿using DarkLink.Util.JsonLd;
using DarkLink.Util.JsonLd.Types;
using DarkLink.Web.ActivityPub.Serialization;

namespace DarkLink.Web.ActivityPub.Types;

public static class Constants
{
    public const string MEDIA_TYPE = "application/activity+json";

    public const string NAMESPACE = "https://www.w3.org/ns/activitystreams#";

    public static readonly LinkedDataList<ContextEntry> Context = DataList.FromItems(new LinkOr<ContextEntry>[]
    {
        new Uri("https://www.w3.org/ns/activitystreams"),
        new ContextEntry
        {
            MapId("inbox", "ldp:inbox"),
            MapId("outbox", "as:outbox"),
            MapId("url", "as:url"),
            MapId("actor", "as:actor"),
            Map("published", "as:published", "xsd:dateTime"),
            MapId("to", "as:to"),
            MapId("attributedTo", "as:attributedTo"),
            {new("totalItems", UriKind.RelativeOrAbsolute), new Uri("as:totalItems", UriKind.RelativeOrAbsolute)},
        }!,
    });

    public static readonly LinkedDataSerializationOptions SerializationOptions = new()
    {
        Converters =
        {
            new LinkToConverter(),
            new LinkableListConverter(),
        },
        TypeResolvers =
        {
            new ActivityPubTypeResolver(),
        },
    };

    public static readonly Uri Public = new($"{NAMESPACE}Public");


    private static (Uri Id, TermMapping Mapping) Map(string property, string iri, string type)
        => (new Uri(property, UriKind.Relative),
            new TermMapping(new Uri(iri, UriKind.RelativeOrAbsolute))
            {
                Type = new Uri(type, UriKind.RelativeOrAbsolute),
            });

    private static (Uri Id, TermMapping Mapping) MapId(string property, string iri)
        => (new Uri(property, UriKind.Relative),
            new TermMapping(new Uri(iri, UriKind.RelativeOrAbsolute))
            {
                Type = Util.JsonLd.Constants.Id,
            });
}
