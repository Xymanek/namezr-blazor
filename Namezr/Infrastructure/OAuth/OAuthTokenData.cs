﻿using System.Diagnostics.CodeAnalysis;
using NodaTime;

namespace Namezr.Infrastructure.OAuth;

internal record OAuthTokenData(string AccessToken, string? RefreshToken);

internal record OAuthTokenContext
{
    public required Instant ExpiresAt { get; set; }

    // TODO: scopes
}

internal record OAuthTokenPayload
{
    public required OAuthTokenData Data { get; init; }
    public required OAuthTokenContext Context { get; init; }

    public OAuthTokenPayload()
    {
    }

    [SetsRequiredMembers]
    public OAuthTokenPayload(OAuthTokenData data, OAuthTokenContext context)
    {
        Data = data;
        Context = context;
    }
}