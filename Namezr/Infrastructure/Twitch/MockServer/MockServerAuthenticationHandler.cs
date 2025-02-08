using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace Namezr.Infrastructure.Twitch.MockServer;

// TODO: this should be OAuthOptions
internal class MockServerAuthenticationHandler : RemoteAuthenticationHandler<RemoteAuthenticationOptions>
{
    private readonly IOptionsMonitor<TwitchOptions> _twitchOptions;

    public MockServerAuthenticationHandler(
        IOptionsMonitor<RemoteAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptionsMonitor<TwitchOptions> twitchOptions
    ) : base(options, logger, encoder)
    {
        _twitchOptions = twitchOptions;
    }

    protected override async Task<HandleRequestResult> HandleRemoteAuthenticateAsync()
    {
        string? errorMessage = null;

        if (Context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
        {
            IFormCollection form = await Context.Request.ReadFormAsync(Context.RequestAborted);
            TwitchOptions twitchOptions = _twitchOptions.CurrentValue;

            string userId = form["user_id"].Single(x => !string.IsNullOrWhiteSpace(x))!;

            string uri = twitchOptions.MockServerUrl + "/auth/authorize";
            uri = QueryHelpers.AddQueryString(uri, new Dictionary<string, string?>
            {
                ["client_id"] = twitchOptions.OAuth.ClientId,
                ["client_secret"] = twitchOptions.OAuth.ClientSecret,
                ["grant_type"] = "user_token",
                ["user_id"] = userId,
                ["scope"] = "user:read:email", // TODO
            });

            using var request = new HttpRequestMessage(HttpMethod.Post, uri);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await Options.Backchannel.SendAsync(request, Context.RequestAborted);

            if (response.IsSuccessStatusCode)
            {
                // TODO

                // TODO: call userinfo endpoint
                
                ClaimsPrincipal user = new();
                user.AddIdentity(new ClaimsIdentity([
                    new Claim(ClaimTypes.NameIdentifier, "123"),
                    new Claim(ClaimTypes.Name, "astraldescend"),
                    new Claim("urn:twitch:displayname", "AstralDescend"),
                ]));

                // public TwitchAuthenticationOptions()

                // ClaimActions.MapCustomJson(ClaimTypes.NameIdentifier, user => GetData(user, "id"));
                // ClaimActions.MapCustomJson(ClaimTypes.Name, user => GetData(user, "login"));
                // ClaimActions.MapCustomJson(Claims.DisplayName, user => GetData(user, "display_name"));
                // ClaimActions.MapCustomJson(ClaimTypes.Email, user => GetData(user, "email"));
                // ClaimActions.MapCustomJson(Claims.Type, user => GetData(user, "type"));
                // ClaimActions.MapCustomJson(Claims.BroadcasterType, user => GetData(user, "broadcaster_type"));
                // ClaimActions.MapCustomJson(Claims.Description, user => GetData(user, "description"));
                // ClaimActions.MapCustomJson(Claims.ProfileImageUrl, user => GetData(user, "profile_image_url"));
                // ClaimActions.MapCustomJson(Claims.OfflineImageUrl, user => GetData(user, "offline_image_url"));

                // using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Context.RequestAborted));
                //
                // var principal = new ClaimsPrincipal(identity);
                // var context = new OAuthCreatingTicketContext(principal, properties, Context, Scheme, Options, Backchannel, tokens, payload.RootElement);
                // context.RunClaimActions();

                AuthenticationProperties properties = new();
                properties.Items.Add(".Token.access_token", "123");

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
                 
                 <form>
                     <div style='margin-bottom: 1em;'>
                         <label for="user_id">User ID</label>
                         <input type="text" id="user_id" name="user_id" value="123"/>
                     </div>
                     
                     <button type="submit">Submit</button>
                 </form>
             </body>
             </html>
             """
        );
    }
}