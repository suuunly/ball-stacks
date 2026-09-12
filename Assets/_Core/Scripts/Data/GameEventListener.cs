using UnityEngine;
using UnityEngine.Events;

namespace Gaman
{
    /// <summary>
    /// Inspector-wired receiver for a <see cref="GameEvent"/> asset. Listens
    /// only while the GameObject is active, so disabling an object silences
    /// its responses automatically.
    /// </summary>
    public class GameEventListener : MonoBehaviour
    {
        [SerializeField] private GameEvent _event;
        [SerializeField] private UnityEvent _response;

        private void Awake()
        {
            Assert.IsNotNull(_event, "GameEventListener: _event is not assigned in the inspector!");
        }

        private void OnEnable()
        {
            _event.RegisterListener(this);
        }

        private void OnDisable()
        {
            _event.UnregisterListener(this);
        }

        public void OnEventRaised()
        {
            _response.Invoke();
        }
    }
}
