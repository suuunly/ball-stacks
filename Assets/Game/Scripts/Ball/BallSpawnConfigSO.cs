using System.Collections.Generic;
using Gaman;
using UnityEngine;

namespace BallStacks
{
    /// <summary>
    /// Designer-tunable recipe for the level-start ball burst: which ball
    /// variants can appear, how likely each one is, and how many balls the
    /// arena gets in total. Referenced by a BallSpawnVolume in the scene.
    /// </summary>
    [CreateAssetMenu(fileName = "BallSpawnConfig", menuName = "Ball Stacks/Ball Spawn Config")]
    public class BallSpawnConfigSO : ScriptableObject
    {
        /// <summary>One spawnable ball variant and its relative rarity.</summary>
        [System.Serializable]
        public class SpawnEntry
        {
            [Tooltip("The ball variant prefab. Must have a Ball component on its root.")]
            [SerializeField] private Ball _prefab;

            [Tooltip("Relative chance of this variant, against the other entries' weights. 2 is twice as common as 1; 0 never spawns.")]
            [Min(0f)]
            [SerializeField] private float _weight = 1f;

            public Ball Prefab => _prefab;
            public float Weight => _weight;
        }

        [Tooltip("How many balls the burst drops into the arena at level start.")]
        [Min(1)]
        [SerializeField] private int _spawnCount = 30;

        [Tooltip("The ball variants that can spawn, each with its relative weight.")]
        [SerializeField] private List<SpawnEntry> _balls = new List<SpawnEntry>();

        public int SpawnCount => _spawnCount;
        public IReadOnlyList<SpawnEntry> Balls => _balls;

        /// <summary>Picks a ball prefab at random, honouring the entry weights.</summary>
        public Ball PickRandomPrefab()
        {
            float totalWeight = 0f;
            foreach (SpawnEntry entry in _balls)
            {
                totalWeight += entry.Weight;
            }

            Assert.IsTrue(totalWeight > 0f,
                "BallSpawnConfigSO: every spawn entry has zero weight — nothing can spawn!");

            float roll = Random.Range(0f, totalWeight);
            foreach (SpawnEntry entry in _balls)
            {
                roll -= entry.Weight;
                if (roll <= 0f) { return entry.Prefab; }
            }

            // Float rounding can leave a sliver of roll after the last entry.
            return _balls[_balls.Count - 1].Prefab;
        }
    }
}
