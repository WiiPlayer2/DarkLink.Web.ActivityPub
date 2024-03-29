﻿namespace DarkLink.Util.JsonLd.Types;

public sealed record Link<T>(Uri Uri) : LinkOr<T>
{
    public override TResult Match<TResult>(Func<Uri, TResult> onLink, Func<T, TResult> onObject) => onLink(Uri);
}
