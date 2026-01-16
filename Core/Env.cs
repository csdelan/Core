namespace Core
{
    /// <summary>
    /// 
    /// </summary>
    public enum AppEnv
    {
        Dev,
        Staging,
        Prod
    }

    public static class App
    {
        public static AppEnv Env
        {
            get
            {
                var env = Environment.GetEnvironmentVariable("RUNTIME_ENVIRONMENT")?.ToLowerInvariant();
                return env switch
                {
                    "dev" => AppEnv.Dev,
                    "staging" => AppEnv.Staging,
                    "prod" => AppEnv.Prod,
                    _ => AppEnv.Dev
                };
            }
        }

        public static string GetSecret(string name)
        {
            var connectionString = Environment.GetEnvironmentVariable($"{Env.ToString().ToUpperInvariant()}_{name}");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException($"Connection string '{name}' is not set for environment '{Env}'.");
            }
            return connectionString;
        }

        public static string GetGlobalSecret(string name)
        {
            var connectionString = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException($"Global secret '{name}' is not set.");
            }
            return connectionString;
        }

        public static string GetConfigFilename(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Configuration filename cannot be null or empty.", nameof(name));
            }
            return Env switch
            {
                AppEnv.Dev => $"{name}.dev.json",
                AppEnv.Staging => $"{name}.staging.json",
                AppEnv.Prod => $"{name}.prod.json",
                _ => $"{name}.dev.json"
            };
        }
    }
}
