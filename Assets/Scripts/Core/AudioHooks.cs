namespace Cellstorm.Core
{
    public enum AudioCue
    {
        Select,
        Assign,
        Error
    }

    /// <summary>
    /// There is no audio yet (GDD: add it in the future). Gameplay code calls these hooks where a sound
    /// belongs, so adding audio later only needs a listener on Played.
    /// </summary>
    public static class AudioHooks
    {
        public static event System.Action<AudioCue> Played;

        public static void Play(AudioCue cue)
        {
            Played?.Invoke(cue);
        }
    }
}
