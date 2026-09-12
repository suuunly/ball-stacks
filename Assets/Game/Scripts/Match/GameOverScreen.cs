using Gaman;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.SceneManagement;

namespace BallStacks
{
    /// <summary>
    /// Runs the game-over flow: when the winner is decided it flips the
    /// Animator's visible flag so the screen's reveal transition plays, and
    /// once the reveal animation reports completion — via an Animation Event
    /// calling <see cref="EnableRestart"/> — any button on any device
    /// restarts the match by reloading the scene.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class GameOverScreen : MonoBehaviour
    {
        [Tooltip("Raised by the referee with the final result when the match ends.")]
        [SerializeField] private GameEventPure _onWinnerDecided;

        [Tooltip("Bool parameter on the Animator whose true state plays the reveal transition.")]
        [SerializeField] private string _visibleParameter = "visible";

        private Animator _animator;
        private System.IDisposable _anyButtonListener;

        private void Awake()
        {
            _animator = GetComponent<Animator>();

            Assert.IsNotNull(_onWinnerDecided, "GameOverScreen: _onWinnerDecided is not assigned in the inspector!");

            _onWinnerDecided.OnRaised += HandleWinnerDecided;
        }

        private void OnDestroy()
        {
            if (_onWinnerDecided != null) { _onWinnerDecided.OnRaised -= HandleWinnerDecided; }
            _anyButtonListener?.Dispose();
        }

        private void HandleWinnerDecided(object payload)
        {
            _animator.SetBool(_visibleParameter, true);
        }

        /// <summary>
        /// Called by the Animation Event at the end of the reveal animation.
        /// Only from here on may a stray button press restart the match —
        /// pressing during the reveal should not skip the moment.
        /// </summary>
        public void EnableRestart()
        {
            // "Any button on any device" cannot live as a binding in the
            // central .inputactions asset, so this is the one sanctioned
            // deviation from the input standard: the Input System's own
            // any-button hook, armed only for this single press.
            _anyButtonListener?.Dispose();
            _anyButtonListener = InputSystem.onAnyButtonPress.CallOnce(HandleAnyButtonPressed);
        }

        private void HandleAnyButtonPressed(InputControl control)
        {
            RestartMatch();
        }

        private void RestartMatch()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(activeScene.buildIndex);
        }
    }
}
