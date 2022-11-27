using DarkLink.Util.JsonLd.Attributes;
using DarkLink.Util.JsonLd.Types;

namespace DarkLink.Web.ActivityPub.Types.Extended;

public record TypedActivity(DataList<Uri> Type) : Activity;

[LinkedDataType($"{Constants.NAMESPACE}Accept")]
public record Accept : Activity;

[LinkedDataType($"{Constants.NAMESPACE}TentativeAccept")]
public record TentativeAccept : Accept;

[LinkedDataType($"{Constants.NAMESPACE}Add")]
public record Add : Activity;

[LinkedDataType($"{Constants.NAMESPACE}Arrive")]
public record Arrive : IntransitiveActivity;

[LinkedDataType($"{Constants.NAMESPACE}Create")]
public record Create : Activity;

[LinkedDataType($"{Constants.NAMESPACE}Delete")]
public record Delete : Activity;

[LinkedDataType($"{Constants.NAMESPACE}Follow")]
public record Follow : Activity;

[LinkedDataType($"{Constants.NAMESPACE}Ignore")]
public record Ignore : Activity;

[LinkedDataType($"{Constants.NAMESPACE}Join")]
public record Join : Activity;

[LinkedDataType($"{Constants.NAMESPACE}Leave")]
public record Leave : Activity;

[LinkedDataType($"{Constants.NAMESPACE}Like")]
public record Like : Activity;

[LinkedDataType($"{Constants.NAMESPACE}Offer")]
public record Offer : Activity;

[LinkedDataType($"{Constants.NAMESPACE}Invite")]
public record Invite : Offer;

[LinkedDataType($"{Constants.NAMESPACE}Reject")]
public record Reject : Activity;

[LinkedDataType($"{Constants.NAMESPACE}TentativeReject")]
public record TentativeReject : Reject;

[LinkedDataType($"{Constants.NAMESPACE}Remove")]
public record Remove : Activity;

[LinkedDataType($"{Constants.NAMESPACE}Undo")]
public record Undo : Activity;

[LinkedDataType($"{Constants.NAMESPACE}Update")]
public record Update : Activity;

[LinkedDataType($"{Constants.NAMESPACE}View")]
public record View : Activity;

[LinkedDataType($"{Constants.NAMESPACE}Listen")]
public record Listen : Activity;

[LinkedDataType($"{Constants.NAMESPACE}Read")]
public record Read : Activity;

[LinkedDataType($"{Constants.NAMESPACE}Move")]
public record Move : Activity;

[LinkedDataType($"{Constants.NAMESPACE}Travel")]
public record Travel : IntransitiveActivity;

[LinkedDataType($"{Constants.NAMESPACE}Announce")]
public record Announce : Activity;

[LinkedDataType($"{Constants.NAMESPACE}Block")]
public record Block : Ignore;

[LinkedDataType($"{Constants.NAMESPACE}Flag")]
public record Flag : Activity;

[LinkedDataType($"{Constants.NAMESPACE}Dislike")]
public record Dislike : Activity;

[LinkedDataType($"{Constants.NAMESPACE}Question")]
public record Question : IntransitiveActivity
{
    [LinkedDataProperty($"{Constants.NAMESPACE}anyOf")]
    public LinkableList<Object> AnyOf { get; set; }

    [LinkedDataProperty($"{Constants.NAMESPACE}oneOf")]
    public LinkableList<Object> OneOf { get; set; }

    //[LinkedDataProperty($"{Constants.NAMESPACE}closed")]
    //public (Object | Link | DateTime | bool) Closed { get; set; } // TODO create type
}
