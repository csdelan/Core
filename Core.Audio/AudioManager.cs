using Serilog;
using System.ComponentModel;

namespace Common
{
    public static class AudioManager
    {
        private static Queue<AudioTrack> sequentialSoundPlayers = new Queue<AudioTrack>();
        private const int _SleepDelay = 200;
        private static AudioTrack StreamingAudio;
        private static BackgroundWorker sequentialQueuePlayer = new BackgroundWorker();

        static AudioManager()
        {
            StreamingAudio = new AudioTrack();
            sequentialQueuePlayer.DoWork += SequentialQueuePlayer_DoWork;
            sequentialQueuePlayer.WorkerSupportsCancellation = true;
            sequentialQueuePlayer.RunWorkerAsync();
        }

        private static void SequentialQueuePlayer_DoWork(object? sender, DoWorkEventArgs e)
        {
            while (!e.Cancel)
            {
                if (sequentialSoundPlayers.TryDequeue(out AudioTrack trackPlayer) == true)
                {
                    Log.Information("Playing {0}", trackPlayer.Uri);
                    trackPlayer.Play();
                    while (!trackPlayer.IsDone)
                        Thread.Sleep(_SleepDelay);
                }
                Thread.Sleep(_SleepDelay);
            }
        }

        public static void BeginStreamingAudio(Uri uri)
        {
            StreamingAudio.Play(uri);
        }

        public static void StopStreamingAudio()
        {
            StreamingAudio.Stop();
        }

        public static void PauseStreamingAudio()
        {
            StreamingAudio.Pause();
        }

        public static void ResumeStreamingAudio()
        {
            StreamingAudio.Resume();
        }

        public static void BeginPlaySound(Uri uri, bool overlapAudio = false)
        {
            if (!overlapAudio)
            {
                AudioTrack newSeqTrack = new AudioTrack(uri);
                sequentialSoundPlayers.Enqueue(newSeqTrack);
                return;
            }

            AudioTrack newTrack = new AudioTrack();
            newTrack.Play(uri);
        }
    }
}
