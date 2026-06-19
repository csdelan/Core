# TradingSystem Core

`Core.dll` is the shared base library for common primitives used by consumer applications in the TradingSystem ecosystem. It contains reusable domain helpers, value-object infrastructure, tagging utilities, environment/config helpers, file metadata helpers, persistence contracts, the canonical JSON serialization policy, and a small set of general-purpose extensions.

This README covers the main `Core` library and the companion `Core.Persistence` package. It does not cover `Core.Audio` or `Core.GoogleSheets`.

## Target Framework

`Core.dll` targets `net10.0`.

Consumer applications should target `net10.0` or a compatible framework and reference `Core.dll` directly or reference the `Core` project during development.

## What Consumers Get

Main types and helpers exposed by `Core.dll`:

- `ValueObject`: base class for structural equality and comparison
- `BaseEvent` and `EventStatus`: simple event/domain record types
- `TagList`, `TagCloud`, and `ITaggable`: lightweight tagging support
- `App.Env`: environment-based config and secret lookup
- `PersonalFile` and `PersonalFileDb`: file hashing and file repository helpers
- `DateTimeOffsetExtensions`: date/time convenience methods
- `ManualTimeProvider`: a controllable, advanceable `System.TimeProvider` for deterministic time
- `ComputerInfo`: Windows-specific machine information helper
- `IDocumentStore<T>`, `IDocument`, `DocumentKey`: backend-agnostic document-persistence contracts (concrete JSON and MongoDB stores ship in the separate `Core.Persistence` package)
- `Core.Json.CoreJson`: the canonical, frozen `JsonSerializerOptions` shared across wire, storage, and config (BCL-only, no third-party serializers)
- `IBackgroundJob`, `JobExecutionContext`, and `BackgroundJobExecutor`: key-based background job dispatch; jobs are resolved per-execution inside a fresh DI scope with structured start/finish logging

## Typical Usage

### Value objects

```csharp
using Core;

public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
```

### Event records

```csharp
using Core;

var evt = new BaseEvent
{
    Name = "OrderSubmitted",
    Class = "Order",
    Subclass = "Submission",
    Priority = 1,
    EventStatus = EventStatus.Unread
};
```

### Environment-aware configuration

```csharp
using Core;

var runtime = App.Env.Current;
var configFile = App.Env.GetConfigFilename("appsettings");
var dbConnection = App.Env.GetSecret("DbConnectionString");
```

`App.Env` reads the `RUNTIME_ENVIRONMENT` environment variable and maps it as follows:

- `dev` -> `App.Env.Dev`
- `staging` -> `App.Env.Staging`
- `prod` -> `App.Env.Prod`

Secret lookup conventions:

- `App.Env.GetSecret("DbConnectionString")` reads `{ENV}_DbConnectionString`
- `App.Env.GetGlobalSecret("SomeName")` reads `SomeName`

### Tags

```csharp
using Core;

var tags = new TagList("swing breakout watchlist");
var serialized = tags.ToString();
```

### DateTime helpers

```csharp
using Core;

var rounded = DateTimeOffset.Now.TruncateToMinute();
var customDay = DateTimeOffset.Now.WithDay(15);
```

### Controllable time (`ManualTimeProvider`)

`Core` builds on the BCL `System.TimeProvider` abstraction so the whole ecosystem can share a single, testable notion of time. Depend on `TimeProvider` everywhere you need the current instant, a `Stopwatch`-style elapsed measurement, or a timer — never on `DateTime.UtcNow` directly.

- **Production** uses the real clock: `TimeProvider.System`.
- **Deterministic simulation and tests** use `Core.ManualTimeProvider`, whose clock only moves when you advance it. Crucially, timers created against it fire when — and only when — the clock is advanced across their due time, so a long-running service can be driven through a simulated timeline.

Both are registered as the single `TimeProvider` service. With `Microsoft.Extensions.DependencyInjection` (a dependency of the *consumer* app, not of bare `Core`):

```csharp
// Production
services.AddSingleton(TimeProvider.System);

// Simulation / tests
services.AddSingleton<TimeProvider>(new ManualTimeProvider());
```

`Core` itself takes no DI-container dependency; consumers register the chosen `TimeProvider` using whatever container they already use.

Driving the clock:

```csharp
using Core;

var time = new ManualTimeProvider(); // starts at the fixed epoch 2000-01-01T00:00:00Z

// A timer that fires only when the clock is advanced across its due time.
using var timer = time.CreateTimer(
    _ => Console.WriteLine("fired"),
    state: null,
    dueTime: TimeSpan.FromSeconds(10),
    period: Timeout.InfiniteTimeSpan);

time.Advance(TimeSpan.FromSeconds(9));  // nothing fires yet
time.Advance(TimeSpan.FromSeconds(1));  // due time crossed -> "fired"

// GetUtcNow and GetElapsedTime stay consistent under the manual clock.
long t0 = time.GetTimestamp();
time.Advance(TimeSpan.FromMilliseconds(250));
TimeSpan elapsed = time.GetElapsedTime(t0); // exactly 250 ms
```

**Timer-firing contract.** Timers never fire on their own or on a background thread; they fire synchronously during `Advance`/`SetUtcNow`. When a single advance spans several due times (including the periods of a repeating timer), callbacks run in chronological order, the correct number of times, and while each callback runs `GetUtcNow()` reports that callback's due time. The clock may only move forward — negative `Advance` deltas and backward `SetUtcNow` are rejected. All operations are thread-safe: the clock can be advanced from one thread while reads and timer callbacks happen on others.

### Background jobs

`Core` provides a lightweight key-based background job system built on the standard DI abstractions.

**Implement a job:**

```csharp
using Core;

public sealed class ReportJob : IBackgroundJob
{
    public string Key => "daily-report";

    public async Task ExecuteAsync(JobExecutionContext context, CancellationToken cancellationToken)
    {
        var region = context.Parameters.GetValueOrDefault("region", "us-east");
        // ... do work ...
    }
}
```

**Register with DI:**

```csharp
services.AddScoped<ReportJob>();
services.AddSingleton<IBackgroundJob, ReportJob>(); // used only to populate the key→type map
services.AddSingleton<BackgroundJobExecutor>();
```

**Dispatch:**

```csharp
await executor.ExecuteAsync("daily-report", new Dictionary<string, string>
{
    ["region"] = "us-east"
});
```

`BackgroundJobExecutor` is a singleton. Each call to `ExecuteAsync` creates a fresh DI scope, resolves the job type from that scope, builds a `JobExecutionContext` with a new `JobId` (GUID) and `ScheduledAt` set to `DateTimeOffset.UtcNow`, then calls the job's `ExecuteAsync`. Start and finish are logged at `Information` level. An unknown `jobKey` throws `InvalidOperationException`.

`JobExecutionContext` properties:

| Property | Type | Description |
|---|---|---|
| `JobId` | `string` | New GUID (`"N"` format) generated per execution |
| `ScheduledAt` | `DateTimeOffset` | UTC instant the job was dispatched |
| `Parameters` | `Dictionary<string, string>` | Caller-supplied key/value pairs (empty by default) |

### JSON serialization policy (`Core.Json.CoreJson`)

`CoreJson` is the single canonical source of `JsonSerializerOptions` for the ecosystem, so on-disk JSON, ZMQ wire payloads, and (via a mirrored convention) MongoDB BSON cannot silently disagree on enum/date/decimal formatting. It depends only on `System.Text.Json` — no third-party serializers, no domain knowledge.

```csharp
using Core.Json;

// Compact, for wire and storage (the forward-looking default — most JSON is machine-read).
string wire = JsonSerializer.Serialize(order, CoreJson.Default);

// Same policy, pretty-printed — for human-edited config such as system-config.json.
File.WriteAllText("system-config.json", JsonSerializer.Serialize(config, CoreJson.Indented));
```

`CoreJson.Default` and `CoreJson.Indented` are **frozen** (`MakeReadOnly`) so the shared instances cannot be mutated. To extend the policy with an extra converter, take a mutable copy and modify that:

```csharp
JsonSerializerOptions extended = CoreJson.CreateOptions();   // == new JsonSerializerOptions(CoreJson.Default)
extended.Converters.Add(new MyCustomConverter());
```

The canonical policy (stated authoritatively in the XML docs, and the contract other layers mirror) is based on `JsonSerializerDefaults.Web`, then:

- **Property names:** camelCase on write, case-insensitive on read — round-trips existing on-disk files written with plain Web defaults.
- **Enums:** serialized as strings via `JsonStringEnumConverter`, with member names preserved as authored (PascalCase, no camelCase policy); reads are case-insensitive.
- **Decimal:** full-precision `System.Decimal`, never routed through `double` (money/quantities stay lossless).
- **DateTimeOffset / DateTime:** ISO-8601 round-trip; callers are expected to store UTC instants.
- **Numbers:** keeps the Web default `NumberHandling` (numbers may be read from strings).

`Core.Persistence.MongoConventions` mirrors this same policy for BSON (enums as strings, `decimal` as `Decimal128`, offset-preserving `DateTimeOffset`), keeping the JSON and Mongo representations in agreement.

### Document persistence (`IDocumentStore<T>`)

`Core` defines a small, backend-agnostic document-repository contract; the implementations live in the **`Core.Persistence`** package so bare `Core` takes no dependency on `MongoDB.Driver`.

```csharp
public interface IDocumentStore<T> where T : class
{
    Task<T?> GetAsync(string id, CancellationToken ct = default);
    Task SaveAsync(T entity, CancellationToken ct = default);            // upsert
    Task<bool> DeleteAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> QueryAsync(Expression<Func<T, bool>> filter, CancellationToken ct = default);
}
```

A store is keyed by an **id member**. A single expression yields both the runtime key and the MongoDB `_id` mapping, so they can never disagree:

```csharp
// Store the entity's Symbol as the document key.
var beta = factory.Create<Beta>("Beta", collectionName: "betas", jsonSubDirectory: "betas", b => b.Symbol);
```

Entities you own may instead implement `Core.IDocument` (`string Id`), in which case the key defaults to `x => x.Id`. External or sealed models (which cannot implement `IDocument`) always work via the id-member expression, or via a custom `Func<T,string>` for composite keys (e.g. `s => $"{s.Date}_{s.Account}"`).

The `Core.Persistence` package provides:

- `JsonDocumentStore<T>` — one `{id}.json` file per entity, atomic temp-file-then-rename writes.
- `MongoDocumentStore<T>` — one document per entity, atomic upserts, server-side queries.
- `MongoConventions` — register-once serialization (enums as strings, `decimal` as `Decimal128`, offset-preserving `DateTimeOffset`).
- `PersistenceOptions` + `IDocumentStoreFactory` — choose the JSON or MongoDB backend per store from configuration, with instant fallback.
- `AddPersistence` / `AddDocumentStore<T>` DI helpers (the MongoDB client connects lazily, so a JSON-only configuration never requires a running `mongod`).

```csharp
using Core.Persistence;

services.AddPersistence(configuration);                       // binds the "Persistence" section
services.AddDocumentStore<Beta>("Beta", "betas", "betas", b => b.Symbol);
```

```jsonc
"Persistence": {
  "Mongo": { "ConnectionString": "mongodb://localhost:27017", "DatabaseName": "TradingSystem" },
  "JsonRootPath": "C:\\Users\\me\\OneDrive\\TradingSystem",
  "DefaultBackend": "Json",
  "Stores": { "Beta": "Mongo", "Trade": "Json" }
}
```

## Publishing Core.dll

Build or publish the main library from the repository root:

```powershell
dotnet build .\Core\Core.csproj -c Release --nologo
dotnet publish .\Core\Core.csproj -c Release /p:PublishProfile=FolderProfile --nologo
```

The folder publish output now includes:

- `Core.dll`
- supporting runtime files generated by .NET
- this `README.md` copied into the publish folder as `README.md`

To pack and push the NuGet packages to a local feed, use `scripts/Publish-Nuget.ps1`. By default it publishes **both** `Core` and `Core.Persistence`:

```powershell
.\scripts\Publish-Nuget.ps1 -LocalFeedPath '\\server\share\NuGet' -Configuration Release
```

Pass `-ProjectPaths` to publish a specific set of projects, or `-ProjectPath` for a single project.

## Consumer Notes

- `ComputerInfo` uses `System.Management` and is Windows-specific.
- `Core` is built with warnings treated as errors, so downstream source changes should be kept clean when contributing back to this repo.

## Repository Layout

- `Core/`: main library source
- `Core.Tests/`: tests for the main library
- `Core.Persistence/`: JSON and MongoDB implementations of `IDocumentStore<T>`
- `Core.Persistence.Tests/`: tests for `Core.Persistence` (uses EphemeralMongo)
- `Core.Audio/`: separate Windows-only audio library, not covered here
- `Core.GoogleSheets/`: separate Google Sheets library, not covered here
- `scripts/`: build/publish scripts (`Publish-Nuget.ps1`)
