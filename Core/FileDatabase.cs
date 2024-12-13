using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public class FileDatabase
    {
        private readonly HashSet<PersonalFile> _files = [];
        private readonly ComputerFileNodeRoots _nodeRootPaths;

        //        public List<PersonalFileDb> Nodes { get; set; }

        public FileDatabase()
        {
        }
    }

    public class ComputerFileNodeRoots
    {
        public string ProcessorId { get; set; }
        public string ComputerName { get; set; }

        // For each collection node in the database, I have a dictionary hash lookup that has the
        // root path of that node for the current computer.
        public Dictionary<string, string> LocalNodeRoots { get; set; }
    }
}
