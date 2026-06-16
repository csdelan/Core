# AGENTS.md

## Core Project Summary

This repository contains shared .NET libraries used by other parts of the TradingSystem codebase. The main solution file is `Core/Core.sln`, even though the repository root is one level above it.

Projects in this repo:

- `Core/Core.csproj`: main reusable library targeting `net10.0`
- `Core.Tests/Core.Tests.csproj`: xUnit tests for `Core`, targeting `net10.0`
- `Core.Persistence/Core.Persistence.csproj`: JSON and MongoDB implementations of `Core.IDocumentStore<T>`, targeting `net10.0` (references `MongoDB.Driver`)
- `Core.Persistence.Tests/Core.Persistence.Tests.csproj`: xUnit tests for `Core.Persistence`, targeting `net10.0` (uses EphemeralMongo)
- `Core.Audio/Core.Audio.csproj`: Windows-only audio helpers targeting `net10.0`
- `Core.GoogleSheets/Core.GoogleSheets.csproj`: Google Sheets helpers targeting `net8.0`

## Usage

Common commands:

```powershell
dotnet build .\Core\Core.csproj -c Debug --nologo
dotnet test .\Core.Tests\Core.Tests.csproj --nologo
dotnet build .\Core.Persistence\Core.Persistence.csproj -c Debug --nologo
dotnet test .\Core.Persistence.Tests\Core.Persistence.Tests.csproj --nologo
dotnet build .\Core.GoogleSheets\Core.GoogleSheets.csproj -c Debug --nologo
.\scripts\Publish-Nuget.ps1 -LocalFeedPath '.\LocalNuGetFeed' -Configuration Debug
```

Use project-level builds when you do not need the full solution. That is usually faster and avoids unnecessary failures from platform-specific projects. In particular, `dotnet build .\Core\Core.sln` fails under the .NET CLI because `Core.Audio` uses a COM reference (`ResolveComReference`) that only the full MSBuild/Visual Studio toolchain supports — build the individual `net10.0` projects instead.

## NuGet Packaging

- `Core\Core.csproj` and `Core.Persistence\Core.Persistence.csproj` each pack both `README.md` and `AGENTS.md` into their NuGet package.
- `README.md` is also declared as the package readme via `PackageReadmeFile`.
- Use `scripts\Publish-Nuget.ps1` to build, pack, and push the packages to a local folder feed or UNC share. By default it publishes **both** `Core` and `Core.Persistence`.
- Override the targets with `-ProjectPaths` (an array) or `-ProjectPath` (a single project, back-compat).
- `LocalFeedPath` supports both relative paths such as `.\LocalNuGetFeed` and rooted UNC paths such as `\\server\share\NuGet`.
- For UNC usage, the share itself must already exist. The script can create subfolders inside an existing share, but it cannot create the SMB share.
- Package artifacts are written to each project's `artifacts\` folder (e.g. `Core\artifacts\`, `Core.Persistence\artifacts\`) before being pushed.
- The script skips symbol packages and pushes only `.nupkg` files.

### Package Versioning

- Each packable project declares `VersionPrefix` as the intended package version source.
- `scripts\Publish-Nuget.ps1` reads the version from each project file and passes it explicitly to `dotnet build` and `dotnet pack`.
- The script sets `/p:UpdateVersionProperties=false` so `GitVersion.MsBuild` does not rewrite the NuGet package version to its fallback value.
- To change a published NuGet version for this workflow, update `VersionPrefix` in that project's `.csproj`.

## Important Constraints

- `Core.Audio` uses a Windows Media Player COM reference and should be treated as Windows-only.
- `Core.GoogleSheets` targets `net8.0`; the other main projects target `net10.0`.
- `Core/Core.csproj` uses `GitVersion.MsBuild`. If build/version targets fail in a fresh environment, install the GitVersion tool first:

```powershell
dotnet tool install --global GitVersion.Tool
```

- `Core` treats warnings as errors in `Debug`, `Release`, and `DebugPackage`; `Core.Persistence` treats warnings as errors in `Debug` and `Release`. This includes NuGet audit warnings (`NU190x`), so transitive package vulnerabilities must be resolved (e.g. by bumping the offending dependency) rather than suppressed.
- `Core.Persistence` references `MongoDB.Driver` (3.x). `Core` itself stays free of that dependency — only the persistence contracts (`IDocumentStore<T>`, `IDocument`, `DocumentKey`) live in `Core`.
- `Core.Persistence.Tests` uses **EphemeralMongo**, which downloads and launches a throwaway `mongod` on first run; that test run needs internet access (and the ability to start a local process). The JSON-store and factory tests do not need MongoDB.
- Build output is redirected to the repo-level `bin\` directory.

## Working Guidance

- Prefer editing and validating the smallest relevant project instead of always building the solution.
- Run `Core.Tests` for changes in the main `Core` library.
- Avoid changing generated files under `bin\` or `obj\`.
- Avoid changing generated files under `artifacts\`.
- Keep public API changes deliberate; this repo provides reusable primitives consumed elsewhere.

## Notable Code Areas

- `Core\ManualTimeProvider.cs`: controllable, advanceable `System.TimeProvider` (timers fire on `Advance`)
- `Core\ValueObject.cs`: structural equality base type
- `Core\BaseEvent.cs`: event lifecycle model
- `Core\Env.cs`: environment/secret lookup helpers
- `Core\PersonalFile*.cs`: file metadata and file database helpers
- `Core\TagList.cs` and `Core\TagCloud.cs`: tagging utilities
- `Core\IDocumentStore.cs`, `Core\IDocument.cs`, `Core\DocumentKey.cs`: backend-agnostic document-persistence contracts and id-member helpers
- `Core.Persistence\JsonDocumentStore.cs` and `Core.Persistence\MongoDocumentStore.cs`: the JSON and MongoDB store implementations
- `Core.Persistence\MongoConventions.cs`: register-once MongoDB serialization (enums as strings, `decimal` as `Decimal128`, offset-preserving `DateTimeOffset`, id → `_id`)
- `Core.Persistence\DocumentStoreFactory.cs` and `Core.Persistence\ServiceCollectionExtensions.cs`: per-store JSON/Mongo backend selection and DI registration
- `Core.GoogleSheets\RowTable.cs`: strongly typed Google Sheets row mapping
- `Core.Audio\AudioManager.cs`: Windows audio playback helpers
