using System.Management;

namespace Core
{
    /// <summary>
    /// Provides methods to retrieve computer hardware and system information.
    /// </summary>
    public class ComputerInfo
    {
        /// <summary>
        /// Retrieves the processor ID from the system's CPU.
        /// </summary>
        /// <returns>A string containing the processor ID, or an empty string if not found.</returns>
        /// <exception cref="PlatformNotSupportedException">
        /// Thrown when this method is called on a non-Windows platform.
        /// </exception>
        /// <remarks>
        /// This method uses Windows Management Instrumentation (WMI) to query the Win32_Processor class.
        /// Only the first processor's ID is returned in multi-processor systems.
        /// </remarks>
        public string GetProcessorId()
        {
            if (!OperatingSystem.IsWindows())
            {
                throw new PlatformNotSupportedException("This method is only supported on Windows.");
            }

            string processorId = string.Empty;
            ManagementObjectSearcher searcher = new("select ProcessorId from Win32_Processor");
            foreach (ManagementObject obj in searcher.Get())
            { 
                processorId = obj["ProcessorId"]?.ToString() ?? string.Empty;
                break;
            }
            return processorId;
        }
    }
}
