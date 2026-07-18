namespace Microsoft.Extensions.Configuration;

/// <summary>
/// Loads shared MarketData secrets the same way in every service — one JSON file holds many secrets;
/// only the file's <i>location</i> varies per machine, never the secrets themselves. Cross-platform:
/// the same file works on Windows, Linux, and in containers.
/// </summary>
public static class MarketDataSecretsConfigurationExtensions
{
    /// <summary>Environment variable holding an explicit path to the shared secrets JSON file.</summary>
    public const string SecretsPathVariable = "MARKETDATA_SECRETS";

    /// <summary>
    /// Layers the shared secret sources onto <paramref name="builder"/>, later sources winning:
    /// <list type="number">
    ///   <item>a cross-platform conventional file —
    ///     <c>%APPDATA%\MarketData\secrets.json</c> on Windows,
    ///     <c>~/.config/MarketData/secrets.json</c> on Linux/macOS;</item>
    ///   <item>an explicit file named by the <c>MARKETDATA_SECRETS</c> env var (e.g.
    ///     <c>/etc/marketdata/secrets.json</c> on a server, or a mounted container secret);</item>
    ///   <item>environment variables (for one-off overrides / container injection).</item>
    /// </list>
    /// Every file source is optional, so a host still starts where no file is present (e.g. a
    /// container that supplies secrets purely via environment variables).
    /// </summary>
    public static IConfigurationBuilder AddMarketDataSecrets(this IConfigurationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (!string.IsNullOrEmpty(appData))
        {
            var conventional = Path.Combine(appData, "MarketData", "secrets.json");
            builder.AddJsonFile(conventional, optional: true, reloadOnChange: true);
        }

        var explicitPath = Environment.GetEnvironmentVariable(SecretsPathVariable);
        if (!string.IsNullOrWhiteSpace(explicitPath))
            builder.AddJsonFile(explicitPath, optional: true, reloadOnChange: true);

        // Re-add env vars last so a container/host can override any file-supplied value.
        builder.AddEnvironmentVariables();
        return builder;
    }
}
