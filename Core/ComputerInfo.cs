using System.Management;

namespace Core
{
    public class ComputerInfo
    {
        public string GetProcessorId()
        {
            string processorId = string.Empty;
            ManagementObjectSearcher searcher = new("select ProcessorId from Win32_Processor");
            foreach (ManagementObject obj in searcher.Get())
            {
                processorId = obj["ProcessorId"].ToString();
                break;
            }
            return processorId;
        }
    }
}
