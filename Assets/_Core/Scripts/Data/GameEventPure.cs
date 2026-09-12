using System;
using UnityEngine;

namespace Gaman
{
    /// <summary>
    /// A code-subscribed event asset carrying an object payload. Senders call
    /// <see cref="Raise"/>; receivers subscribe to <see cref="OnRaised"/> in
    /// Awake and must unsubscribe in OnDestroy. Use this for system-to-system
    /// data passing; use <see cref="GameEvent"/> when a designer should wire
    /// the responses in the Inspector instead.
    /// </summary>
    [CreateAssetMenu(fileName = "PureGameEvent", menuName = "SDE/Data/Pure Game Event")]
    public class GameEventPure : ScriptableObject
    {
        public event Action<object> OnRaised;

        public void Raise(object param = null)
        {
            OnRaised?.Invoke(param);
        }

        private void OnDisable()
        {
            // Clear all subscribers when the asset unloads (scene unload,
            // editor stop) — stale MonoBehaviour references must not
            // accumulate on a persistent SO asset.
            if (OnRaised == null) { return; }

            foreach (Delegate subscriber in OnRaised.GetInvocationList())
            {
                OnRaised -= (Action<object>)subscriber;
            }
        }
    }
}
