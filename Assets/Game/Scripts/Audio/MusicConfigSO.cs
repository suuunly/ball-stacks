using UnityEngine;

namespace BallStacks
{
    /// <summary>
    /// Tuning data for the match music: the looping in-game track, the
    /// game-over track, and the fade timings and volumes between them — all
    /// designer-editable without code changes.
    /// </summary>
    [CreateAssetMenu(fileName = "MusicConfig", menuName = "Ball Stacks/Music Config")]
    public class MusicConfigSO : ScriptableObject
    {
        [Tooltip("The looping track that plays through the menu and the match.")]
        [SerializeField] private AudioClip _matchLoopClip;

        [Tooltip("The track the match loop cross-fades into when the winner is decided.")]
        [SerializeField] private AudioClip _gameOverClip;

        [Tooltip("Seconds the match loop takes to fade in from silence when the scene starts.")]
        [Min(0.01f)]
        [SerializeField] private float _fadeInSeconds = 2f;

        [Tooltip("Seconds the cross-fade from the match loop to the game-over track takes.")]
        [Min(0.01f)]
        [SerializeField] private float _crossfadeSeconds = 1.5f;

        [Range(0f, 1f)]
        [SerializeField] private float _matchVolume = 1f;

        [Range(0f, 1f)]
        [SerializeField] private float _gameOverVolume = 1f;

        [Tooltip("Loop the game-over track while the end screen lingers, or leave off to play it once as a sting.")]
        [SerializeField] private bool _loopGameOverTrack;

        public AudioClip MatchLoopClip => _matchLoopClip;
        public AudioClip GameOverClip => _gameOverClip;
        public float FadeInSeconds => _fadeInSeconds;
        public float CrossfadeSeconds => _crossfadeSeconds;
        public float MatchVolume => _matchVolume;
        public float GameOverVolume => _gameOverVolume;
        public bool LoopGameOverTrack => _loopGameOverTrack;
    }
}
