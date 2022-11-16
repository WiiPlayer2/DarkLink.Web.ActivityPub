namespace DarkLink.Util.JsonLd;

internal static class Helper
{
    public static T Create<T>(this Type openGenericType, params Type[] typeArguments)
    {
        var genericType = openGenericType.MakeGenericType(typeArguments);
        return (T) Activator.CreateInstance(genericType)!;
    }

    public static string Uncapitalize(this string s)
        => string.IsNullOrEmpty(s)
            ? string.Empty
            : $"{char.ToLowerInvariant(s[0])}{s[1..]}";
}
