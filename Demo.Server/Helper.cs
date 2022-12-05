namespace Demo.Server;

internal static class Helper
{
    public static Uri BuildUri(this HttpContext ctx, string path)
        => new($"{ctx.Request.Scheme}://{ctx.Request.Host}/{path.TrimStart('/')}");

    public static Task<T[]> WhenAll<T>(this IEnumerable<Task<T>> tasks, CancellationToken cancellationToken = default)
        => Task.WhenAll(tasks);
}
