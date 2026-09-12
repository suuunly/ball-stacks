using System.Collections.Generic;
using UnityEngine;

namespace Gaman
{
    /// <summary>
    /// A designer-wireable event asset with no payload. Senders call
    /// <see cref="Raise"/>; receivers add a <see cref="GameEventListener"/>
    /// component, assign this asset, and wire responses in the Inspector —
    /// sender and receivers never know about each other.
    /// </summary>
    [CreateAssetMenu(fileName = "GameEvent", menuName = "SDE/Data/Game Event")]
    public class GameEvent : ScriptableObject
    {
        private readonly List<GameEventListener> _listeners = new List<GameEventListener>();

        public void Raise()
        {
            // Iterate in reverse — safe if a listener unregisters itself during the loop.
            for (int i = _listeners.Count - 1; i >= 0; i--)
            {
                _listeners[i].OnEventRaised();
            }
        }

        public void RegisterListener(GameEventListener listener)
        {
            if (_listeners.Contains(listener)) { return; }

            _listeners.Add(listener);
        }

        public void UnregisterListener(GameEventListener listener)
        {
            _listeners.Remove(listener);
        }
    }
}
