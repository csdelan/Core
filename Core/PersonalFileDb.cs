using Serilog;
using System.ComponentModel;

namespace Core
{
    /// <summary>
    /// A PersonalFileRepo is set up at the root of the directory structure.
    /// </summary>
    public class PersonalFileDb
    {
        private readonly HashSet<PersonalFile> _files = new();
        private BackgroundWorker? _rebuildWorker;
        public string RepoPath { get; private set; }
        public string[] IncludePaths { get; }
        public string RepoName { get; private set; }

        /// <summary>
        /// Create new repo
        /// </summary>
        /// <param name="repoPath"></param>
        /// <param name="includePaths"></param>
        public PersonalFileDb(string repoPath, string[] includePaths)
        {
            RepoPath = repoPath;
            IncludePaths = includePaths;
            // Need to create the repo
            FileInfo fi = new(repoPath);
            RepoName = fi.Name;
        }

        /// <summary>
        /// Open existing repo
        /// </summary>
        /// <param name="repoPath"></param>
        public PersonalFileDb(string repoPath)
        {
            RepoPath = repoPath;
            IncludePaths = Array.Empty<string>(); // Initialize IncludePaths to an empty array
            FileInfo fi = new(repoPath);
            if (!fi.Exists)
            {
                throw new FileNotFoundException("Repo file not found", repoPath);
            }
            RepoName = fi.Name;

            // Read all of the PersonalFiles and includePaths from the database
        }

        public void StartRebuild()
        {
            if (_rebuildWorker != null)
            {
                if (_rebuildWorker.IsBusy)
                {
                    Log.Warning("{0} - Rebuild already in progress. New rebuild request ignored", RepoName);
                    return;
                }
            }
            _rebuildWorker = new BackgroundWorker();
            _rebuildWorker.DoWork += RebuildWorkerImpl;
            _rebuildWorker.WorkerSupportsCancellation = true;
            _rebuildWorker.RunWorkerCompleted += RebuildWorkerCompleted;
            _rebuildWorker.RunWorkerAsync();
        }

        private void RebuildWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
        {
            Log.Information("{0} - Rebuild complete", RepoName);
        }

        private void RebuildWorkerImpl(object? sender, DoWorkEventArgs e)
        {
            Log.Information("{0} - Rebuild started", RepoName);
            Log.Information("{0} - We are starting with {1} files in the database", RepoName, _files.Count);

            // Do the rebuild
            foreach (string filePath in Directory.EnumerateFiles(RepoPath, "*", SearchOption.AllDirectories))
            {
                PersonalFile file = new(filePath);
                // Log.Information("{0} - Added files {1} to the database", RepoName, file.FilePath);
                _files.Add(file);

                // Cancel check
                if (_rebuildWorker != null && _rebuildWorker.CancellationPending)
                {
                    Log.Information("{0} - Cancelled rebuild", RepoName);
                    e.Cancel = true;
                    return;
                }
            }
        }

        public bool RealTimeWatcherEnabled { get; set; } = true;
    }
}
