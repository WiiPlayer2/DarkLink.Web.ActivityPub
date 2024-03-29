﻿namespace DarkLink.Util.JsonLd.Types;

public sealed record Object<T>(T Data) : LinkOr<T>
{
    public override TResult Match<TResult>(Func<Uri, TResult> onLink, Func<T, TResult> onObject) => onObject(Data);
}
