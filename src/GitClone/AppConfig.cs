namespace GitClone;

/// <summary>Compile-time configuration. The OAuth client id is public (device-flow public client).</summary>
public static class AppConfig
{
    public const string OAuthClientId = "Ov23lisIxJSnEoMAdIzj";
    public const string ProductName = "GitClone";

    /// <summary>Scopes requested during device flow: read/create/push private repos and delete them.</summary>
    public static readonly string[] OAuthScopes = { "repo", "delete_repo" };
}
