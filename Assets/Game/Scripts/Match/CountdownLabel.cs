using Gaman;
using TMPro;
using UnityEngine;

namespace BallStacks
{
    /// <summary>
    /// Shows the match countdown as m:ss on whatever TMP text it sits on —
    /// a screen-space HUD label or a world-space 3D text alike. Driven purely
    /// by the countdown-ticked event; it never touches the timer itself.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class CountdownLabel : MonoBehaviour
    {
        private const int SecondsPerMinute = 60;

        [SerializeField] private GameEventPure _onCountdownTicked;

        private TMP_Text _label;

        private void Awake()
        {
            _label = GetComponent<TMP_Text>();

            Assert.IsNotNull(_onCountdownTicked, "CountdownLabel: _onCountdownTicked is not assigned in the inspector!");

            _onCountdownTicked.OnRaised += HandleCountdownTicked;
        }

        private void OnDestroy()
        {
            if (_onCountdownTicked != null) { _onCountdownTicked.OnRaised -= HandleCountdownTicked; }
        }

        private void HandleCountdownTicked(object payload)
        {
            if (payload is not int wholeSecondsRemaining) { return; }

            int minutes = wholeSecondsRemaining / SecondsPerMinute;
            int seconds = wholeSecondsRemaining % SecondsPerMinute;
            _label.text = $"{minutes}:{seconds:00}";
        }
    }
}
