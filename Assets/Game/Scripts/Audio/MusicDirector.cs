using Gaman;
using UnityEngine;

namespace BallStacks
{
    /// <summary>
    /// Plays the match music: fades the looping track in from silence when
    /// the scene starts, and cross-fades it into the game-over track when the
    /// winner is decided. Owns its two AudioSources (created in Awake, always
    /// flat 2D); the fade runs in Update, which stays disabled whenever no
    /// fade is in progress. A rematch reloads the scene, so every match gets
    /// the fade-in again.
    /// </summary>
    public class MusicDirector : MonoBehaviour
    {
        [SerializeField] private MusicConfigSO _config;

        [Tooltip("Raised by the referee with the final result when the match ends — triggers the cross-fade into the game-over track.")]
        [SerializeField] private GameEventPure _onWinnerDecided;

        private AudioSource _matchSource;
        private AudioSource _gameOverSource;

        private float _fadeDuration;
        private float _fadeTimer;
        private float _matchStartVolume;
        private float _matchTargetVolume;
        private float _gameOverStartVolume;
        private float _gameOverTargetVolume;

        private void Awake()
        {
            Assert.IsNotNull(_config, "MusicDirector: _config is not assigned in the inspector!");
            Assert.IsNotNull(_onWinnerDecided, "MusicDirector: _onWinnerDecided is not assigned in the inspector!");
            Assert.IsNotNull(_config.MatchLoopClip, "MusicDirector: the config has no match loop clip!");
            Assert.IsNotNull(_config.GameOverClip, "MusicDirector: the config has no game-over clip!");

            _matchSource = CreateSource(_config.MatchLoopClip, loop: true);
            _gameOverSource = CreateSource(_config.GameOverClip, _config.LoopGameOverTrack);

            _onWinnerDecided.OnRaised += HandleWinnerDecided;

            // Update only runs while a fade is in progress, but the idle
            // disable must NOT happen here: disabling a component in Awake
            // stops Unity from ever calling its Start (same gotcha MatchTimer
            // documents), which would strand the fade-in. Start begins the
            // fade-in immediately, and FinishFade disables between fades.
        }

        private void OnDestroy()
        {
            if (_onWinnerDecided != null) { _onWinnerDecided.OnRaised -= HandleWinnerDecided; }
        }

        private void Start()
        {
            _matchSource.Play();
            BeginFade(_config.FadeInSeconds, _config.MatchVolume, 0f);
        }

        private void Update()
        {
            _fadeTimer += Time.deltaTime;
            float progress = Mathf.Clamp01(_fadeTimer / _fadeDuration);

            _matchSource.volume = Mathf.Lerp(_matchStartVolume, _matchTargetVolume, progress);
            _gameOverSource.volume = Mathf.Lerp(_gameOverStartVolume, _gameOverTargetVolume, progress);

            bool fadeComplete = progress >= 1f;
            if (fadeComplete) { FinishFade(); }
        }

        private void HandleWinnerDecided(object payload)
        {
            _gameOverSource.volume = 0f;
            _gameOverSource.Play();

            BeginFade(_config.CrossfadeSeconds, 0f, _config.GameOverVolume);
        }

        // Start volumes are captured from wherever the sources currently sit,
        // so a winner decided mid-fade-in still cross-fades smoothly instead
        // of snapping.
        private void BeginFade(float duration, float matchTargetVolume, float gameOverTargetVolume)
        {
            _fadeDuration = duration;
            _fadeTimer = 0f;

            _matchStartVolume = _matchSource.volume;
            _matchTargetVolume = matchTargetVolume;
            _gameOverStartVolume = _gameOverSource.volume;
            _gameOverTargetVolume = gameOverTargetVolume;

            enabled = true;
        }

        private void FinishFade()
        {
            enabled = false;

            bool matchTrackFadedOut = Mathf.Approximately(_matchSource.volume, 0f);
            if (matchTrackFadedOut) { _matchSource.Stop(); }
        }

        private AudioSource CreateSource(AudioClip clip, bool loop)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = loop;
            source.playOnAwake = false;
            source.spatialBlend = 0f; // Music is always flat 2D.
            source.volume = 0f;
            return source;
        }
    }
}
