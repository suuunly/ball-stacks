using System.Collections.Generic;
using Gaman;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BallStacks
{
    /// <summary>
    /// Turns every connected gamepad into two player seats, Overcooked style:
    /// the left half (left stick + d-pad down) and the right half (right stick
    /// + south face button). A seat spawns its player ball the first time that
    /// half of the pad is touched.
    /// </summary>
    public class PlayerSeatSpawner : MonoBehaviour
    {
        private const float WakeStickDeadzone = 0.2f;

        [Tooltip("The central input asset. Each gamepad gets its own runtime copy so pads drive their seats independently.")]
        [SerializeField] private InputActionAsset _inputActions;

        [Header("Seat Actions (from the central input asset)")]
        [SerializeField] private InputActionReference _leftSeatMove;
        [SerializeField] private InputActionReference _leftSeatJump;
        [SerializeField] private InputActionReference _rightSeatMove;
        [SerializeField] private InputActionReference _rightSeatJump;

        [Header("Spawning")]
        [SerializeField] private PlayerController _playerPrefab;
        [SerializeField] private Ball _ballPrefab;
        [SerializeField] private Transform[] _spawnPoints;
        [SerializeField] private int _maxPlayers = 4;

        private readonly List<Seat> _seats = new List<Seat>();
        private readonly List<InputActionAsset> _actionInstances = new List<InputActionAsset>();
        private readonly HashSet<int> _claimedDeviceIds = new HashSet<int>();
        private int _spawnedPlayerCount;

        private void Awake()
        {
            Assert.IsNotNull(_inputActions, "PlayerSeatSpawner: _inputActions is not assigned in the inspector!");
            Assert.IsNotNull(_leftSeatMove, "PlayerSeatSpawner: _leftSeatMove is not assigned in the inspector!");
            Assert.IsNotNull(_leftSeatJump, "PlayerSeatSpawner: _leftSeatJump is not assigned in the inspector!");
            Assert.IsNotNull(_rightSeatMove, "PlayerSeatSpawner: _rightSeatMove is not assigned in the inspector!");
            Assert.IsNotNull(_rightSeatJump, "PlayerSeatSpawner: _rightSeatJump is not assigned in the inspector!");
            Assert.IsNotNull(_playerPrefab, "PlayerSeatSpawner: _playerPrefab is not assigned in the inspector!");
            Assert.IsNotNull(_ballPrefab, "PlayerSeatSpawner: _ballPrefab is not assigned in the inspector!");
            Assert.IsTrue(_spawnPoints != null && _spawnPoints.Length > 0,
                "PlayerSeatSpawner: no spawn points assigned in the inspector!");
        }

        private void OnEnable()
        {
            foreach (Gamepad gamepad in Gamepad.all)
            {
                CreateSeatsForGamepad(gamepad);
            }

            InputSystem.onDeviceChange += HandleDeviceChange;
        }

        private void OnDisable()
        {
            InputSystem.onDeviceChange -= HandleDeviceChange;
        }

        private void OnDestroy()
        {
            DisposeSeats();
        }

        private void HandleDeviceChange(InputDevice device, InputDeviceChange change)
        {
            bool isNewGamepad = change == InputDeviceChange.Added && device is Gamepad;
            if (!isNewGamepad) { return; }

            CreateSeatsForGamepad((Gamepad)device);
        }

        private void CreateSeatsForGamepad(Gamepad gamepad)
        {
            bool isAlreadySeated = _claimedDeviceIds.Contains(gamepad.deviceId);
            if (isAlreadySeated) { return; }

            _claimedDeviceIds.Add(gamepad.deviceId);

            InputActionAsset instance = Instantiate(_inputActions);
            instance.devices = new InputDevice[] { gamepad };
            _actionInstances.Add(instance);

            CreateSeat(instance, _leftSeatMove, _leftSeatJump);
            CreateSeat(instance, _rightSeatMove, _rightSeatJump);
        }

        private void CreateSeat(InputActionAsset instance, InputActionReference moveReference, InputActionReference jumpReference)
        {
            InputAction move = FindInstanceAction(instance, moveReference);
            InputAction jump = FindInstanceAction(instance, jumpReference);
            move.Enable();
            jump.Enable();

            var seat = new Seat(move, jump);
            seat.WakeRequested += HandleSeatWake;
            _seats.Add(seat);
        }

        // Instantiate preserves action ids, so a reference into the central
        // asset resolves to the matching action on this gamepad's own copy.
        private static InputAction FindInstanceAction(InputActionAsset instance, InputActionReference reference)
        {
            return instance.FindAction(reference.action.id.ToString(), throwIfNotFound: true);
        }

        private void HandleSeatWake(Seat seat)
        {
            seat.WakeRequested -= HandleSeatWake;

            bool hasRoomForAnotherPlayer = _spawnedPlayerCount < _maxPlayers;
            if (!hasRoomForAnotherPlayer) { return; }

            SpawnPlayer(seat);
        }

        private void SpawnPlayer(Seat seat)
        {
            Transform spawnPoint = _spawnPoints[_spawnedPlayerCount % _spawnPoints.Length];
            _spawnedPlayerCount++;

            Ball ball = Instantiate(_ballPrefab, spawnPoint.position, spawnPoint.rotation);
            PlayerController player = Instantiate(_playerPrefab);
            player.Initialize(seat.MoveAction, seat.JumpAction, ball);
        }

        private void DisposeSeats()
        {
            foreach (Seat seat in _seats)
            {
                seat.WakeRequested -= HandleSeatWake;
                seat.Dispose();
            }

            _seats.Clear();

            foreach (InputActionAsset instance in _actionInstances)
            {
                Destroy(instance);
            }

            _actionInstances.Clear();
            _claimedDeviceIds.Clear();
        }

        /// <summary>One half of a gamepad: its two actions and a one-shot wake signal.</summary>
        private class Seat
        {
            public InputAction MoveAction { get; }
            public InputAction JumpAction { get; }

            public event System.Action<Seat> WakeRequested;

            private bool _hasWoken;

            public Seat(InputAction moveAction, InputAction jumpAction)
            {
                MoveAction = moveAction;
                JumpAction = jumpAction;
                MoveAction.performed += HandleMovePerformed;
                JumpAction.performed += HandleJumpPerformed;
            }

            public void Dispose()
            {
                MoveAction.performed -= HandleMovePerformed;
                JumpAction.performed -= HandleJumpPerformed;
            }

            private void HandleMovePerformed(InputAction.CallbackContext context)
            {
                bool isDeliberateInput = context.ReadValue<Vector2>().magnitude >= WakeStickDeadzone;
                if (!isDeliberateInput) { return; }

                Wake();
            }

            private void HandleJumpPerformed(InputAction.CallbackContext context)
            {
                Wake();
            }

            private void Wake()
            {
                if (_hasWoken) { return; }

                _hasWoken = true;
                WakeRequested?.Invoke(this);
            }
        }
    }
}
