using Gaman;
using UnityEngine;

namespace BallStacks
{
    /// <summary>
    /// A box volume that fills the arena with loose balls in one burst at
    /// level start. Each ball is a random variant from the spawn config,
    /// dropped at a random point inside the box — place the box above the
    /// ground so balls rain in from varying heights. Move, rotate, and
    /// resize the volume in the Scene view via its transform and the
    /// wire-box gizmo. Balls may land on anything below, including player
    /// stacks — that chaos is intended.
    /// </summary>
    public class BallSpawnVolume : MonoBehaviour
    {
        private static readonly Color GizmoOutlineColor = new Color(1f, 0.25f, 0.2f);
        private static readonly Color GizmoFillColor = new Color(1f, 0.25f, 0.2f, 0.08f);

        [Tooltip("What to spawn: ball variants, weights, and total count.")]
        [SerializeField] private BallSpawnConfigSO _config;

        [Tooltip("Size of the spawn box in local space, centred on this transform.")]
        [SerializeField] private Vector3 _size = new Vector3(10f, 4f, 10f);

        private void Awake()
        {
            Assert.IsNotNull(_config, "BallSpawnVolume: _config is not assigned in the inspector!");
            Assert.IsTrue(_config.Balls.Count > 0, "BallSpawnVolume: the spawn config has no ball entries!");
        }

        private void Start()
        {
            SpawnBurst();
        }

        private void SpawnBurst()
        {
            for (int i = 0; i < _config.SpawnCount; i++)
            {
                Ball prefab = _config.PickRandomPrefab();
                Assert.IsNotNull(prefab, "BallSpawnVolume: a spawn entry in the config has no prefab assigned!");

                Instantiate(prefab, RandomPointInVolume(), Random.rotation);
            }
        }

        private Vector3 RandomPointInVolume()
        {
            Vector3 halfSize = _size * 0.5f;
            var localPoint = new Vector3(
                Random.Range(-halfSize.x, halfSize.x),
                Random.Range(-halfSize.y, halfSize.y),
                Random.Range(-halfSize.z, halfSize.z));

            return transform.TransformPoint(localPoint);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = GizmoOutlineColor;
            Gizmos.DrawWireCube(Vector3.zero, _size);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = GizmoFillColor;
            Gizmos.DrawCube(Vector3.zero, _size);
        }
#endif
    }
}
