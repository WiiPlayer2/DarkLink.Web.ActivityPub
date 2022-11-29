namespace Demo.Server;

internal class Config
{
    public const string KEY = "Config";

    public string DataDirectory { get; init; } = default!;

    public string Password { get; init; } = default!;

    public string Username { get; init; } = default!;
}
