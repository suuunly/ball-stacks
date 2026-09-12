using System.Collections.Generic;
using Gaman;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BallStacks
{
    /// <summary>
    /// Lets a freshly joined player pick their starting ball: flicking the
    /// move stick left/right cycles this player's coloured aura through every
    /// unoccupied ball in the scene, and pressing jump takes control of the
    /// targeted one. If the targeted ball gets taken or occupied mid-pick, the
    /// aura hops to the next free ball automatically.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerBallSelector : MonoBehaviour
    {
        private const float FlickThreshold = 0.5f;
        private const float FlickResetThreshold = 0.2f;

        [SerializeField] private RuntimeSet _balls;

        private PlayerController _player;
        private InputAction _moveAction;
        private InputAction _jumpAction;
        private Color _auraColor;
        private BallOccupancy _target;
        private bool _stickIsCentered = true;

        private void Awake()
        {
            _player = GetComponent<PlayerController>();

            Assert.IsNotNull(_balls, "PlayerBallSelector: _balls is not assigned in the inspector!");

            enabled = false;
        }

        private void OnDestroy()
        {
            if (_jumpAction != null) { _jumpAction.performed -= HandleJumpPerformed; }
        }

        /// <summary>Starts the pick phase for this player's seat.</summary>
        public void BeginSelection(InputAction moveAction, InputAction jumpAction, Color auraColor)
        {
            _moveAction = moveAction;
            _jumpAction = jumpAction;
            _auraColor = auraColor;
            _jumpAction.performed += HandleJumpPerformed;

            enabled = true;
        }

        private void Update()
        {
            EnsureValidTarget();
            HandleStickCycling();
        }

        private void EnsureValidTarget()
        {
            bool targetIsStillFree = _target != null && _target.isActiveAndEnabled && !_target.IsOccupied;
            if (targetIsStillFree) { return; }

            SetTarget(FindFreeBall(_target, forward: true));
        }

        private void HandleStickCycling()
        {
            float stickX = _moveAction.ReadValue<Vector2>().x;

            if (!_stickIsCentered)
            {
                bool stickReleased = Mathf.Abs(stickX) <= FlickResetThreshold;
                if (stickReleased) { _stickIsCentered = true; }
                return;
            }

            bool flicked = Mathf.Abs(stickX) >= FlickThreshold;
            if (!flicked) { return; }

            _stickIsCentered = false;
            SetTarget(FindFreeBall(_target, forward: stickX > 0f));
        }

        private void HandleJumpPerformed(InputAction.CallbackContext context)
        {
            if (_target == null) { return; }

            ConfirmSelection();
        }

        private void ConfirmSelection()
        {
            BallOccupancy chosenBall = _target;
            SetTarget(null);
            StopSelecting();

            _player.Possess(chosenBall.Ball);
        }

        private void StopSelecting()
        {
            _jumpAction.performed -= HandleJumpPerformed;
            enabled = false;
        }

        private void SetTarget(BallOccupancy newTarget)
        {
            if (_target == newTarget) { return; }

            SetAuraVisible(_target, false);
            _target = newTarget;
            SetAuraVisible(_target, true);
        }

        private void SetAuraVisible(BallOccupancy ball, bool visible)
        {
            if (ball == null) { return; }
            if (!ball.TryGetComponent(out BallAura aura)) { return; }

            if (visible)
            {
                aura.ShowTargeting(_auraColor);
            }
            else
            {
                aura.Hide();
            }
        }

        // Walks the balls RuntimeSet once (wrapping around) from the current
        // target and returns the next unoccupied ball in the chosen direction,
        // the current target if it is the only free ball, or null.
        private BallOccupancy FindFreeBall(BallOccupancy current, bool forward)
        {
            LinkedList<IRuntime> balls = _balls.List;
            if (balls.Count == 0) { return null; }

            LinkedListNode<IRuntime> node = null;
            if (current != null) { node = balls.Find(current); }

            for (int i = 0; i < balls.Count; i++)
            {
                node = Step(balls, node, forward);

                bool isFreeCandidate = node.Value is BallOccupancy candidate
                    && candidate != current
                    && !candidate.IsOccupied;
                if (isFreeCandidate) { return (BallOccupancy)node.Value; }
            }

            bool currentIsOnlyFreeBall = current != null && !current.IsOccupied;
            if (currentIsOnlyFreeBall) { return current; }

            return null;
        }

        private static LinkedListNode<IRuntime> Step(LinkedList<IRuntime> balls, LinkedListNode<IRuntime> node, bool forward)
        {
            if (node == null)
            {
                if (forward) { return balls.First; }

                return balls.Last;
            }

            if (forward) { return node.Next ?? balls.First; }

            return node.Previous ?? balls.Last;
        }
    }
}
