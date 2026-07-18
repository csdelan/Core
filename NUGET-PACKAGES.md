# NuGet Packages

Packages published from this repository to the local NuGet feed (`\\bart\MyNuget`).

<!-- Managed by the nuget-publish skill. The publish script parses and updates the
     tables below - keep their column layout intact. -->

## Packages

| Package ID | Project | Current Version |
| --- | --- | --- |
| Core | Core/Core.csproj | 2.2.0 |
| Core.Persistence | Core.Persistence/Core.Persistence.csproj | 2.0.0 |

## Version History

| Date | Package | Version | Notes |
| --- | --- | --- | --- |
| 2026-07-05 | Core | 2.2.0 | Add IConfigurationBuilder.AddMarketDataSecrets(): cross-platform shared secrets loader (conventional %APPDATA%/~/.config file + MARKETDATA_SECRETS path override + env vars). |
| 2026-07-04 | Core | 2.1.0 | Fix missing period in TruncateToMinute XML doc comment (republish); republished: Fix inadvertent Core.BackgroundJobs namespace regression - restore flat Core namespace for BackgroundJob, BackgroundJobExecutor, JobResult, JobRunRegistry |
| 2026-06-19 | Core.Persistence | 2.0.0 | Initial publish |
| 2026-06-19 | Core | 2.0.0 | Published alongside Core.Persistence 2.0.0 |
| 2026-04-25 | Core | 1.0.0 | Initial publish |
