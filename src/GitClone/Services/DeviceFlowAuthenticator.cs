using Octokit;

namespace GitClone.Services;

/// <summary>Runs the OAuth device flow. The returned token lives only in memory.</summary>
public sealed class DeviceFlowAuthenticator
{
    private readonly GitHubClient _client = new(new ProductHeaderValue(AppConfig.ProductName));

    /// <summary>Step 1: ask GitHub for a user code; caller shows it and opens the verification URL.</summary>
    public Task<OauthDeviceFlowResponse> RequestCodeAsync()
    {
        var request = new OauthDeviceFlowRequest(AppConfig.OAuthClientId);
        foreach (var scope in AppConfig.OAuthScopes)
            request.Scopes.Add(scope);
        return _client.Oauth.InitiateDeviceFlow(request);
    }

    /// <summary>Step 2: poll until the user authorizes; returns the access token.</summary>
    public async Task<string> WaitForTokenAsync(OauthDeviceFlowResponse code)
    {
        var token = await _client.Oauth.CreateAccessTokenForDeviceFlow(AppConfig.OAuthClientId, code);
        return token.AccessToken;
    }
}
