namespace GitClone;

/// <summary>Compile-time configuration. The OAuth client id is public (device-flow public client).</summary>
public static class AppConfig
{
    public const string OAuthClientId = "Ov23lisIxJSnEoMAdIzj";
    public const string ProductName = "GitClone";

    /// <summary>
    /// Scopes requested during device flow: read/create/push private repos, delete them, and
    /// push GitHub Actions workflow files (.github/workflows/*) — GitHub rejects those without
    /// the dedicated 'workflow' scope, which broke restoring repos that contain workflows.
    /// </summary>
    public static readonly string[] OAuthScopes = { "repo", "delete_repo", "workflow" };
}
