using Gaman;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace BallStacks
{
    /// <summary>
    /// The title screen: waits for any button on any device, then raises the
    /// match-started GameEvent and hides itself. Wire the responses in the
    /// Inspector — starting the countdown (MatchTimer.BeginMatch), start
    /// fanfare, music.
    /// </summary>
    public class MainMenuScreen : MonoBehaviour
    {
        [Tooltip("Raised once when a player presses any button to leave the menu. Wire MatchTimer.BeginMatch (and any start fanfare) here.")]
        [SerializeField] private GameEvent _onMatchStarted;

        private System.IDisposable _anyButtonListener;

        private void Awake()
        {
            Assert.IsNotNull(_onMatchStarted, "MainMenuScreen: _onMatchStarted is not assigned in the inspector!");
        }

        private void OnEnable()
        {
            // "Any button on any device" cannot live as a binding in the
            // central .inputactions asset, so this is the one sanctioned
            // deviation from the input standard (same as GameOverScreen):
            // the Input System's own any-button hook, armed for one press.
            _anyButtonListener?.Dispose();
            _anyButtonListener = InputSystem.onAnyButtonPress.CallOnce(HandleAnyButtonPressed);
        }

        private void OnDisable()
        {
            _anyButtonListener?.Dispose();
        }

        private void HandleAnyButtonPressed(InputControl control)
        {
            _onMatchStarted.Raise();
            gameObject.SetActive(false);
        }
    }
}
