using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using System.Web;
using AspNet.Security.OAuth.Twitch;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Users.GetUsers;

namespace Namezr.Infrastructure.Twitch.MockServer;

[AutoConstructor]
internal partial class MockServerAuthenticationHandler : TwitchAuthenticationHandler
{
    // TODO: custom authentication options
    private readonly IOptionsMonitor<TwitchOptions> _twitchOptions;

    private readonly ITwitchHttpFactory _twitchHttpFactory;
    private readonly ILoggerFactory _loggerFactory;

    protected override string BuildChallengeUrl(AuthenticationProperties properties, string redirectUri)
    {
        Dictionary<string, string> parameters = new()
        {
            ["state"] = Options.StateDataFormat.Protect(properties),
        };

        return QueryHelpers.AddQueryString(Options.CallbackPath, parameters!);
    }

    protected override async Task<HandleRequestResult> HandleRemoteAuthenticateAsync()
    {
        AuthenticationProperties properties =
            Options.StateDataFormat.Unprotect(Request.Query["state"])
            ?? throw new InvalidOperationException();

        string? errorMessage = null;

        if (Context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
        {
            IFormCollection form = await Context.Request.ReadFormAsync(Context.RequestAborted);
            TwitchOptions twitchOptions = _twitchOptions.CurrentValue;

            string userId = form["user_id"].Single(x => !string.IsNullOrWhiteSpace(x))!;

            string uri = twitchOptions.MockServerUrl + "/auth/authorize";
            uri = QueryHelpers.AddQueryString(uri, new Dictionary<string, string?>
            {
                ["client_id"] = Options.ClientId,
                ["client_secret"] = Options.ClientSecret,
                ["grant_type"] = "user_token",
                ["user_id"] = userId,
                ["scope"] = FormatScope(), // TODO
            });

            using var request = new HttpRequestMessage(HttpMethod.Post, uri);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await Options.Backchannel.SendAsync(request, Context.RequestAborted);

            if (response.IsSuccessStatusCode)
            {
                // TODO

                using JsonDocument payload =
                    JsonDocument.Parse(await response.Content.ReadAsStringAsync(Context.RequestAborted));

                OAuthTokenResponse tokens = OAuthTokenResponse.Success(payload);

                TwitchAPI api = new(
                    loggerFactory: _loggerFactory,

                    // this sets the URL override
                    http: _twitchHttpFactory.Create()
                )
                {
                    Settings =
                    {
                        AccessToken = tokens.AccessToken,
                        ClientId = Options.ClientId,
                        Secret = Options.ClientSecret,
                    }
                };

                GetUsersResponse userResponse = await api.Helix.Users.GetUsersAsync(ids: [userId]);
                User userInfo = userResponse.Users.Single();

                // TODO: convert to manual HttpClient call + OAuthCreatingTicketContext.RunClaimActions()
                ClaimsIdentity userIdentity = new(ClaimsIssuer);
                userIdentity.AddClaims([
                    new Claim(ClaimTypes.NameIdentifier, userInfo.Id),
                    new Claim(ClaimTypes.Name, userInfo.Login),
                    new Claim(TwitchAuthenticationConstants.Claims.DisplayName, userInfo.DisplayName),
                    new Claim(ClaimTypes.Email, userInfo.Email),
                    new Claim(TwitchAuthenticationConstants.Claims.Type, userInfo.Type),
                    new Claim(TwitchAuthenticationConstants.Claims.BroadcasterType, userInfo.BroadcasterType),
                    new Claim(TwitchAuthenticationConstants.Claims.Description, userInfo.Description),
                    new Claim(TwitchAuthenticationConstants.Claims.ProfileImageUrl, userInfo.ProfileImageUrl),
                    new Claim(TwitchAuthenticationConstants.Claims.OfflineImageUrl, userInfo.OfflineImageUrl),
                ]);
                ClaimsPrincipal user = new(userIdentity);

                if (Options.SaveTokens)
                {
                    var authTokens = new List<AuthenticationToken>();

                    authTokens.Add(new AuthenticationToken { Name = "access_token", Value = tokens.AccessToken! });

                    if (!string.IsNullOrEmpty(tokens.TokenType))
                    {
                        authTokens.Add(new AuthenticationToken { Name = "token_type", Value = tokens.TokenType });
                    }

                    if (!string.IsNullOrEmpty(tokens.ExpiresIn))
                    {
                        int value;
                        if (int.TryParse(tokens.ExpiresIn, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                        {
                            // https://www.w3.org/TR/xmlschema-2/#dateTime
                            // https://learn.microsoft.com/dotnet/standard/base-types/standard-date-and-time-format-strings
                            var expiresAt = TimeProvider.GetUtcNow() + TimeSpan.FromSeconds(value);
                            authTokens.Add(new AuthenticationToken
                            {
                                Name = "expires_at",
                                Value = expiresAt.ToString("o", CultureInfo.InvariantCulture)
                            });
                        }
                    }

                    properties.StoreTokens(authTokens);
                }
                
                return HandleRequestResult.Success(new AuthenticationTicket(user, properties, Scheme.Name));
            }

            errorMessage =
                $"""
                 Failed to get token from mock server. Response code: {response.StatusCode}. Body:
                 <pre>
                     {HttpUtility.HtmlEncode(await response.Content.ReadAsStringAsync(Context.RequestAborted))}
                 </pre>
                 """;
        }

        await WriteHtml(errorMessage);

        return HandleRequestResult.Handle();
    }

    private async ValueTask WriteHtml(string? errorMessage)
    {
        Context.Response.ContentType = "text/html";
        Context.Response.StatusCode = 200;

        StreamWriter writer = new(Context.Response.Body);

        await writer.WriteAsync(
            // language=html
            $"""
             <!DOCTYPE html>
             <html>
             <head>
                 <title>Namezr - mock Twitch server authentication</title>
             </head>
             <body>
                 <h1>Mock Twitch Server</h1>
                 
                 <div style='color: red; margin: 1em;'>
                     {errorMessage}
                 </div>
                 
                 <form method="post">
                     <div style='margin-bottom: 1em;'>
                         <label for="user_id">User ID</label>
                        <input type="text" id="user_id" name="user_id" autofocus />
                     </div>
                     
                     <button type="submit">Submit</button>
                 </form>
             </body>
             </html>
             """
        );
        
        await writer.FlushAsync();
    }
}