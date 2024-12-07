using System.Collections;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Core
{
    public enum FileStatus
    {
        New,
        Pending,
        Filed,
        Archived,
        Deleted
    }

    public class PersonalFile
    {
        /// <summary>
        /// The name of the file collection node this file belongs to (eg. "Documents", "Pictures", "cf", etc.)
        /// </summary>
        public string CollectionNode { get; set; }

        /// <summary>
        /// Relative path off the root of the collection node where this file is located
        /// </summary>
        public string RelativePath { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Hash { get; private set; }
        public string Category { get; set; }
        public string Subcategory { get; set; }
        public string FileType { get; set; }
        public FileStatus Status { get; set; }
        public Tags Tags { get; set; }
        public DateTime CreateTime { get; set; }
        public ulong Size { get; set; }

        public PersonalFile(string filePath)
        {
            Hash = ComputeFileHash(filePath);
        }

        private string ComputeFileHash(string filePath)
        {
            using (var sha256 = SHA256.Create())
            {
                using (var fileStream = File.OpenRead(filePath))
                {
                    byte[] hashBytes = sha256.ComputeHash(fileStream);
                    return ConvertHashToHexString(hashBytes);
                }
            }
        }

        private string ConvertHashToHexString(byte[] bytes)
        {
            StringBuilder hashStringBuilder = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
            {
                hashStringBuilder.Append(b.ToString("x2"));
            }
            return hashStringBuilder.ToString();
        }

        public override int GetHashCode()
        {
            return Int32.Parse(Hash.Substring(0, 8), NumberStyles.HexNumber);
        }
    }
}
