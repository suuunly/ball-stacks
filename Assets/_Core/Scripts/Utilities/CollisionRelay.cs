using System;
using UnityEngine;

namespace Gaman
{
    /// <summary>
    /// Forwards physics collision callbacks as a C# event, so a system that
    /// does not live on the colliding object can react to its collisions.
    /// </summary>
    public class CollisionRelay : MonoBehaviour
    {
        public event Action<Collision> CollisionEntered;
        public event Action<Collision> CollisionStayed;

        private void OnCollisionEnter(Collision collision)
        {
            CollisionEntered?.Invoke(collision);
        }

        private void OnCollisionStay(Collision collision)
        {
            CollisionStayed?.Invoke(collision);
        }
    }
}
