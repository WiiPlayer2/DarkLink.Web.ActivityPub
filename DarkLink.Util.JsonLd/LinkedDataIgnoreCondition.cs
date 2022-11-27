namespace DarkLink.Util.JsonLd;

public enum LinkedDataIgnoreCondition
{
    Never,

    Always,

    //WhenWritingNull, // TODO I don't think this makes sense for now
    WhenWritingDefault,
}
