using System;
using Gaman;
using UnityEngine;

namespace BallStacks
{
    /// <summary>
    /// The match countdown. Ticks a pure event with the whole seconds
    /// remaining (for countdown displays), and when the clock hits zero
    /// announces the end of the match: first over the <see cref="MatchEnded"/>
    /// C# event (code reactions like the referee deciding a winner), then over
    /// the designer-wireable GameEvent (end screens, sounds, confetti).
    /// </summary>
    public class MatchTimer : MonoBehaviour
    {
        [SerializeField] private MatchConfigSO _config;

        [Tooltip("Raised with the whole seconds remaining (int payload) whenever the displayed second changes.")]
        [SerializeField] private GameEventPure _onCountdownTicked;

        [Tooltip("Raised once when the countdown reaches zero. Wire end-of-match responses here in the Inspector.")]
        [SerializeField] private GameEvent _onMatchEnded;

        /// <summary>Raised at zero, before <see cref="_onMatchEnded"/> — code
        /// subscribers (the referee) settle the result before designer
        /// responses run.</summary>
        public event Action MatchEnded;

        public float SecondsRemaining { get; private set; }
        public bool HasEnded { get; private set; }

        private int _lastTickedSeconds;

        private void Awake()
        {
            Assert.IsNotNull(_config, "MatchTimer: _config is not assigned in the inspector!");
            Assert.IsNotNull(_onCountdownTicked, "MatchTimer: _onCountdownTicked is not assigned in the inspector!");
            Assert.IsNotNull(_onMatchEnded, "MatchTimer: _onMatchEnded is not assigned in the inspector!");

            enabled = false;
        }

        private void Start()
        {
            if (_config.AutoStartMatch) { BeginMatch(); }
        }

        /// <summary>Starts (or restarts) the countdown from the configured match length.</summary>
        public void BeginMatch()
        {
            SecondsRemaining = _config.MatchDurationSeconds;
            HasEnded = false;
            _lastTickedSeconds = int.MinValue;

            enabled = true;
            RaiseTickIfSecondChanged();
        }

        private void Update()
        {
            SecondsRemaining = Mathf.Max(SecondsRemaining - Time.deltaTime, 0f);
            RaiseTickIfSecondChanged();

            bool timeIsUp = SecondsRemaining <= 0f;
            if (timeIsUp) { EndMatch(); }
        }

        private void RaiseTickIfSecondChanged()
        {
            int wholeSecondsRemaining = Mathf.CeilToInt(SecondsRemaining);
            bool displayedSecondChanged = wholeSecondsRemaining != _lastTickedSeconds;
            if (!displayedSecondChanged) { return; }

            _lastTickedSeconds = wholeSecondsRemaining;
            _onCountdownTicked.Raise(wholeSecondsRemaining);
        }

        private void EndMatch()
        {
            HasEnded = true;
            enabled = false;

            MatchEnded?.Invoke();
            _onMatchEnded.Raise();
        }
    }
}
