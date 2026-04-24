# AGENTS.md

## Core Project Summary

This repository contains shared .NET libraries used by other parts of the TradingSystem codebase. The main solution file is `Core/Core.sln`, even though the repository root is one level above it.

Projects in this repo:

- `Core/Core.csproj`: main reusable library targeting `net10.0`
- `Core.Tests/Core.Tests.csproj`: xUnit tests for `Core`, targeting `net10.0`
- `Core.Audio/Core.Audio.csproj`: Windows-only audio helpers targeting `net10.0`
- `Core.GoogleSheets/Core.GoogleSheets.csproj`: Google Sheets helpers targeting `net8.0`

## Usage

Run commands from the repository root: `C:\Users\csdel\source\repos\TradingSystem\Core`

Common commands:

```powershell
dotnet build .\Core\Core.csproj -c Debug --nologo
dotnet test .\Core.Tests\Core.Tests.csproj --nologo
dotnet build .\Core.GoogleSheets\Core.GoogleSheets.csproj -c Debug --nologo
dotnet build .\Core\Core.sln -c Debug --nologo
```

Use project-level builds when you do not need the full solution. That is usually faster and avoids unnecessary failures from platform-specific projects.

## Important Constraints

- `Core.Audio` uses a Windows Media Player COM reference and should be treated as Windows-only.
- `Core.GoogleSheets` targets `net8.0`; the other main projects target `net10.0`.
- `Core/Core.csproj` uses `GitVersion.MsBuild`. If build/version targets fail in a fresh environment, install the GitVersion tool first:

```powershell
dotnet tool install --global GitVersion.Tool
```

- `Core` treats warnings as errors in `Debug`, `Release`, and `DebugPackage`.
- Build output is redirected to the repo-level `bin\` directory.

## Working Guidance

- Prefer editing and validating the smallest relevant project instead of always building the solution.
- Run `Core.Tests` for changes in the main `Core` library.
- Avoid changing generated files under `bin\` or `obj\`.
- Keep public API changes deliberate; this repo provides reusable primitives consumed elsewhere.

## Notable Code Areas

- `Core\ValueObject.cs`: structural equality base type
- `Core\BaseEvent.cs`: event lifecycle model
- `Core\Env.cs`: environment/secret lookup helpers
- `Core\PersonalFile*.cs`: file metadata and file database helpers
- `Core\TagList.cs` and `Core\TagCloud.cs`: tagging utilities
- `Core.GoogleSheets\RowTable.cs`: strongly typed Google Sheets row mapping
- `Core.Audio\AudioManager.cs`: Windows audio playback helpers
