using Serilog;

namespace Common
{
    internal class AudioTrack
    {
        public Uri? Uri { get; set; }

        private MediaPlayer.MediaPlayer mediaPlayer = new MediaPlayer.MediaPlayer();

        public AudioTrack(Uri location)
        {
            Uri = location;
        }

        public AudioTrack()
        {
        }

        public void Play(Uri uri)
        {
            mediaPlayer.AutoStart = false;
            mediaPlayer.FileName = uri.LocalPath;
            mediaPlayer.Play();
        }

        public void Play()
        {
            Play(Uri);
        }

        public void Stop()
        {
            mediaPlayer.Stop();
        }

        public void Pause()
        {
            mediaPlayer.Pause();
        }

        public void Resume()
        {
            mediaPlayer.Play();
        }

        public bool IsDone
        {
            get
            {
                try
                {
                    var currentState = mediaPlayer.PlayState;
                    Log.Information(currentState.ToString());
                    var desiredState1 = MediaPlayer.MPPlayStateConstants.mpClosed;
                    var desiredState2 = MediaPlayer.MPPlayStateConstants.mpStopped;
                    return (mediaPlayer.PlayState == desiredState1 ||
                            mediaPlayer.PlayState == desiredState2);
                }
                catch { return false; }
            }
        }
    }
}
