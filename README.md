# Namezr

# Configuration

Example:

```json
{
  "Sentry:Dsn": "...",
  "Sentry:ClientDsn": "...",
  "ConnectionStrings": {
    "postgresdb": "Host=localhost;Port=5432;Database=namezr;Username=postgres;Password=postgres;Include Error Detail=true"
  },
  "App:Files": {
    "StoragePath": "..."
  },
  "Twitch": {
    "OAuth": {
      "ClientId": "...",
      "ClientSecret": "..."
    },
    "MockServerUrl": "http://localhost:8080"
  },
  "Patreon": {
    "ClientId": "...",
    "ClientSecret": "..."
  },
  "Google": {
    "ClientId": "...",
    "ClientSecret": "..."
  },
  "Discord": {
    "ClientId": "...",
    "ClientSecret": "...",
    "BotToken": "..."
  }
}
```

+ `ASPNETCORE_FORWARDEDHEADERS_ENABLED` = `true` for reverse proxy support (env var)

# Third party setup

## Discord

* Requires `Server Members Intent` to be enabled

## Google

* Requires `People API` to be enabled
