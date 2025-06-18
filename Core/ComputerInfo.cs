using System.Management;

namespace Core
{
    public class ComputerInfo
    {
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
