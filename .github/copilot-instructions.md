# Copilot Cloud Agent Instructions

## Repository Overview

This is a **C# / .NET** shared-library repository containing reusable building blocks used across multiple consumer applications. It targets **.NET 10.0** (except `Core.GoogleSheets`, which targets net8.0).

---

## Solution & Project Structure

The solution file is at `Core/Core.sln` (inside the `Core/` subdirectory, not the repo root).

| Project | Path | Namespace | Target | Notes |
|---|---|---|---|---|
| `Core` | `Core/Core.csproj` | `Core` | net10.0 | Main library. Uses GitVersion + Serilog + Syncfusion |
| `Core.Tests` | `Core.Tests/Core.Tests.csproj` | `Core.Tests` | net10.0 | xUnit test project (68 tests) |
| `Core.Audio` | `Core.Audio/Core.Audio.csproj` | `Common` | net10.0 | **Windows-only** (COM MediaPlayer reference) |
| `Core.GoogleSheets` | `Core.GoogleSheets/Core.GoogleSheets.csproj` | `Core.GoogleSheets` | net8.0 | Google Sheets CRUD |

Build output goes to `bin/` at the repo root (i.e., `../../bin/` relative to individual project files).

---

## Building and Testing

### Prerequisites (Linux / cloud agent)

`Core.csproj` uses the **GitVersion.MsBuild** package for automatic semantic versioning. In a fresh clone without the global CLI tool, the build will fail with:

```
error MSB3073: The command "dotnet --roll-forward Major .../gitversion.dll" exited with code 1.
```

**Workaround – install the GitVersion global tool once per environment:**

```bash
dotnet tool install --global GitVersion.Tool
```

After installing the tool, builds of `Core.csproj` succeed.

### Build commands (run from the repo root)

```bash
# Build the main Core library
dotnet build Core/Core.csproj

# Build the Google Sheets library (no special prereqs)
dotnet build Core.GoogleSheets/Core.GoogleSheets.csproj

# Run all tests
dotnet test Core.Tests/Core.Tests.csproj

# Build the whole solution (Windows only – Core.Audio uses COM references)
dotnet build Core/Core.sln
```

> **Note:** `Core.Audio` uses a COM reference to Windows Media Player (`MediaPlayer` type library). It **cannot** be built with `dotnet build` on Linux because `ResolveComReference` is only supported by the .NET Framework MSBuild. On Linux, build `Core/Core.csproj` and `Core.GoogleSheets/Core.GoogleSheets.csproj` individually and skip `Core.Audio`.

### Build configurations

Three configurations are defined: `Debug`, `Release`, `DebugPackage`. All three have `TreatWarningsAsErrors=true` for `Core`.

---

## Key Abstractions & Domain Concepts

### `ValueObject` (`Core/ValueObject.cs`)
Abstract base class for DDD value objects. Subclasses implement `GetEqualityComponents()` to define structural equality. Supports comparison operators, `IComparable`, and ORM proxy unwrapping (EF Core / NHibernate).

### `BaseEvent` (`Core/BaseEvent.cs`)
Base class for domain events with lifecycle tracking (Created, Modified, Closed, Priority, Status, Payload). `EventStatus` enum: `Unread → Read → Processing → Completed`.

### Tag system (`Core/TagList.cs`, `Core/TagCloud.cs`)
- `TagList` is a `HashSet<string>` that can be constructed from a space-separated string or a `string[]`.
- `ITaggable` interface exposes a `TagList Tags` property.
- `TagCloud` takes a `List<ITaggable>` and provides `GetAllTags()` / `GetTagStatistics()`.

### `PersonalFile` / `PersonalFileDb` (`Core/PersonalFile.cs`, `Core/PersonalFileDb.cs`)
File management: `PersonalFile` hashes files with SHA-256 on construction. `PersonalFileDb` scans a directory tree, populates a `HashSet<PersonalFile>`, and supports async rebuild via `BackgroundWorker` with cancellation.

### `App` / `AppEnv` (`Core/Env.cs`)
Environment-driven configuration helper. Reads the `RUNTIME_ENVIRONMENT` env var (`dev` / `staging` / `prod`, defaults to `dev`) to select secrets and config file names.

```csharp
// pattern for env-scoped secrets: {ENV}_{NAME}
App.GetSecret("DbConnectionString");    // e.g. DEV_DbConnectionString

// pattern for global secrets
App.GetGlobalSecret("SomeName");

// environment-scoped config filenames
App.GetConfigFilename("appsettings");  // → "appsettings.dev.json" in dev
```

### `SyncfusionLicenser` (`Core/SyncfusionLicenser.cs`)
Reads the Syncfusion license from a **machine-level** environment variable named `SYNCFUSIONKEY_{major}_{minor}_{patch}` (e.g., `SYNCFUSIONKEY_27_2_2`). Must be called at app startup with the version string (e.g., `"27.2.2"`).

### `ComputerInfo` (`Core/ComputerInfo.cs`)
Uses WMI (`System.Management`) to query CPU processor ID. **Windows-only** — throws `PlatformNotSupportedException` on non-Windows platforms.

### `DateTimeOffsetExtensions` (`Core/DateTimeOffsetExtensions.cs`)
Extension methods: `WithDay`, `WithDayAndMonth`, `TruncateToMinute`.

---

## Google Sheets Integration (`Core.GoogleSheets`)

### `GoogleWorksheet`
Thin wrapper around `SheetsService` that provides cell/range get, set, append, and batch-update operations against a named sheet tab within a spreadsheet.

### `RowTable<T>` (strongly-typed CRUD)
Maps a POCO class to rows in a Google Sheet using two custom attributes:
- `[SheetColumn("Header Name")]` — maps a property to a column (optionally pin with `Index`).
- `[SheetKey]` — marks the unique key property (exactly one per type required).

```csharp
public class MyRow
{
    [SheetKey]
    [SheetColumn("ID")]
    public string Id { get; set; } = "";

    [SheetColumn("Name")]
    public string Name { get; set; } = "";
}

var table = new RowTable<MyRow>(worksheet);
await table.EnsureHeaderAsync();
await table.UpsertAsync(new MyRow { Id = "1", Name = "Alice" });
var rows = await table.GetAllAsync();
await table.DeleteByKeyAsync("1");
```

Supports `copyFormattingOnInsert` to copy row formatting from the previous data row when inserting.

---

## Audio (`Core.Audio`) — Windows only

`AudioManager` (static class, namespace `Common`) provides sequential and streaming audio playback via Windows Media Player COM:

```csharp
AudioManager.BeginStreamingAudio(uri);
AudioManager.BeginPlaySound(uri);           // queued sequential
AudioManager.BeginPlaySound(uri, overlapAudio: true); // parallel
AudioManager.StopStreamingAudio();
```

---

## Versioning

Uses **GitVersion** (`ContinuousDeployment` mode, tag prefix `v`, semantic version format `Loose`). The current base version is `1.0.2` (`VersionPrefix` in `Core.csproj`). Version tags use the `v1.0.0` format.

---

## Code Conventions

- **Nullable reference types** enabled in all projects (`<Nullable>enable</Nullable>`).
- **Implicit usings** enabled (`<ImplicitUsings>enable</ImplicitUsings>`).
- `TreatWarningsAsErrors` is `true` for all build configurations in `Core`.
- All public APIs have XML doc comments (`/// <summary>…</summary>`).
- Tests follow the **Arrange-Act-Assert** pattern, use `[Fact]` attributes (xUnit), and test class names end with `Test`.
- `Core.Audio` uses namespace `Common` (not matching the project name `Core.Audio`).

---

## Known Issues / Errors Encountered

| Issue | Root cause | Workaround |
|---|---|---|
| `error MSB3073: gitversion.dll exited with code 1` | `GitVersion.MsBuild` (v6.5.0) requires the `GitVersion.Tool` CLI to be installed globally | `dotnet tool install --global GitVersion.Tool` |
| `error MSB4803: ResolveComReference not supported` | `Core.Audio` uses a Windows-only COM reference (MediaPlayer) | Build `Core.Audio` with Visual Studio on Windows; skip on Linux; build other projects individually |
| `Core.GoogleSheets` nullable warnings treated as errors in stricter contexts | Two known nullable suppressions at `GoogleWorksheet.cs:75` and `RowTable.cs:310` | Warnings are present but don't block build since `TreatWarningsAsErrors` is not set for `Core.GoogleSheets` |
