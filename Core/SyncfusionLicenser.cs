using Serilog;

namespace Core
{
    [Obsolete("This class is deprecated. Syncfusion license registration should be handled in the application startup code.")]
    static public class SyncfusionLicenser
    {
        public static void Register(string versionString)
        {
            // Read Environment Variable for Syncfusion license key
            var vsString = versionString.Replace(".", "_");
            EnvironmentVariableTarget target = EnvironmentVariableTarget.Machine;
            var licenseKey = Environment.GetEnvironmentVariable("SYNCFUSIONKEY_" + vsString, target);
            if (licenseKey != null)
            {
                // Syncfusion license registration
                //SyncfusionLicenseProvider.RegisterLicense(licenseKey);
                Log.Debug("Syncfusion license registered from Environment Variable");
                return;
            }

            // Syncfusion license registration
            Log.Debug("Syncfusion license not found in environment variable: SYNCFUSIONKEY_" + vsString);
        }
    }
}
