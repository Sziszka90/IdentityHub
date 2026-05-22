# IdentityHub.ExampleProject

Small consumer app for testing `IdentityHub.Client` attribute-based authorization and caching without a database.

## What it does

- Uses `AddIdentityHubClient(...)`
- Uses `AddIdentityHubAuthorization()`
- Protects `GET /api/example/fixed` with `[RequirePermission("example.read")]`
- Hosts a mock IdentityHub permission endpoint at `POST /mock-identityhub/api/authorization/check`
- Tracks how many permission checks hit the mock backend so cache behavior is visible

## Run

```bash
dotnet run --project src/IdentityHub.ExampleProject/IdentityHub.ExampleProject.csproj
```

## Test with Swagger

Open `http://localhost:5085/swagger`.

Use the `Authorize` button and enter one of these values:

- `Bearer allow-token` to call the protected endpoint successfully
- `Bearer deny-token` to verify the endpoint returns `403`

## Test permission attribute

Allowed request:

```bash
curl -i -H 'Authorization: Bearer allow-token' http://localhost:5085/api/example/fixed
```

Denied request:

```bash
curl -i -H 'Authorization: Bearer deny-token' http://localhost:5085/api/example/fixed
```

## Test caching

Reset the probe:

```bash
curl -X POST http://localhost:5085/api/example/cache-stats/reset
```

Call the protected endpoint multiple times with the same allowed token:

```bash
curl -H 'Authorization: Bearer allow-token' http://localhost:5085/api/example/fixed
curl -H 'Authorization: Bearer allow-token' http://localhost:5085/api/example/fixed
curl -H 'Authorization: Bearer allow-token' http://localhost:5085/api/example/fixed
```

Inspect the mock permission-check call count:

```bash
curl http://localhost:5085/api/example/cache-stats
```

With caching enabled, repeated calls inside the TTL should only increment the mock backend once.

## Switch to Redis

Update `IdentityHubClient` settings in `appsettings.json`:

```json
{
    "IdentityHubClient": {
        "BaseUrl": "http://localhost:5085/mock-identityhub",
        "CacheProvider": "Distributed",
        "RedisConnectionString": "localhost:6379",
        "RedisInstanceName": "identityhub-example:",
        "CacheSeconds": 300,
        "PermissionCheckCacheSeconds": 60,
        "CacheKeyPrefix": "IdentityHubExample"
    }
}
```
