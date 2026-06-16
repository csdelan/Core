# TradingSystem Core

`Core.dll` is the shared base library for common primitives used by consumer applications in the TradingSystem ecosystem. It contains reusable domain helpers, value-object infrastructure, tagging utilities, environment/config helpers, file metadata helpers, and a small set of general-purpose extensions.

This README covers the main `Core` library only. It does not cover `Core.Audio` or `Core.GoogleSheets`.

## Target Framework

`Core.dll` targets `net10.0`.

Consumer applications should target `net10.0` or a compatible framework and reference `Core.dll` directly or reference the `Core` project during development.

## What Consumers Get

Main types and helpers exposed by `Core.dll`:

- `ValueObject`: base class for structural equality and comparison
- `BaseEvent` and `EventStatus`: simple event/domain record types
- `TagList`, `TagCloud`, and `ITaggable`: lightweight tagging support
- `App` and `AppEnv`: environment-based config and secret lookup
- `PersonalFile` and `PersonalFileDb`: file hashing and file repository helpers
- `DateTimeOffsetExtensions`: date/time convenience methods
- `ManualTimeProvider`: a controllable, advanceable `System.TimeProvider` for deterministic time
- `SyncfusionLicenser`: optional Syncfusion license registration helper
- `ComputerInfo`: Windows-specific machine information helper

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

var runtime = App.Env;
var configFile = App.GetConfigFilename("appsettings");
var dbConnection = App.GetSecret("DbConnectionString");
```

`App` reads the `RUNTIME_ENVIRONMENT` environment variable and maps it as follows:

- `dev` -> `AppEnv.Dev`
- `staging` -> `AppEnv.Staging`
- `prod` -> `AppEnv.Prod`

Secret lookup conventions:

- `App.GetSecret("DbConnectionString")` reads `{ENV}_DbConnectionString`
- `App.GetGlobalSecret("SomeName")` reads `SomeName`

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

## Consumer Notes

- `ComputerInfo` uses `System.Management` and is Windows-specific.
- `SyncfusionLicenser.Register(version)` looks for a machine-level environment variable named like `SYNCFUSIONKEY_27_2_2`.
- `Core` is built with warnings treated as errors, so downstream source changes should be kept clean when contributing back to this repo.

## Repository Layout

- `Core/`: main library source
- `Core.Tests/`: tests for the main library
- `Core.Audio/`: separate Windows-only audio library, not covered here
- `Core.GoogleSheets/`: separate Google Sheets library, not covered here
