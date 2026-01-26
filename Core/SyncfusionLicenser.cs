using Serilog;
using Syncfusion.Licensing;

namespace Core
{
    static internal class SyncfusionLicenser
    {
        static internal void RegisterLicense()
        {
            // Read Environment Variable for Syncfusion license key
            var licenseKey = Environment.GetEnvironmentVariable("SYNCFUSIONKEY_27_2_2");
            if (licenseKey != null)
            {
                // Syncfusion license registration
                SyncfusionLicenseProvider.RegisterLicense(licenseKey);
                Log.Debug("Syncfusion license registered from Environment Variable");
                return;
            }

            // Syncfusion license registration
            Log.Debug("Syncfusion license not found in environment variable: SYNCFUSIONKEY_27_2_2");
        }
    }
}
