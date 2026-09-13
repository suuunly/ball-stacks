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
        /// <summary>One spawnable ball variant, its relative rarity, and how much its size may vary.</summary>
        [System.Serializable]
        public class SpawnEntry
        {
            [Tooltip("The ball variant prefab. Must have a Ball component on its root.")]
            [SerializeField] private Ball _prefab;

            [Tooltip("Relative chance of this variant, against the other entries' weights. 2 is twice as common as 1; 0 never spawns.")]
            [Min(0f)]
            [SerializeField] private float _weight = 1f;

            [Tooltip("Smallest size multiplier this variant can spawn at. 1 = the prefab's own size.")]
            [Min(0.05f)]
            [SerializeField] private float _minSize = 1f;

            [Tooltip("Largest size multiplier this variant can spawn at. Keep both at 1 for a fixed-size variant.")]
            [Min(0.05f)]
            [SerializeField] private float _maxSize = 1f;

            public Ball Prefab => _prefab;
            public float Weight => _weight;

            /// <summary>Rolls a random size multiplier within this variant's range.</summary>
            public float RollSizeMultiplier()
            {
                return Random.Range(_minSize, _maxSize);
            }
        }

        [Tooltip("How many balls the burst drops into the arena at level start.")]
        [Min(1)]
        [SerializeField] private int _spawnCount = 30;

        [Tooltip("The ball variants that can spawn, each with its relative weight.")]
        [SerializeField] private List<SpawnEntry> _balls = new List<SpawnEntry>();

        [Tooltip("How strongly a ball's mass follows its size multiplier. 3 = true volume (a 1.8x bigger ball is ~6x heavier, so big cargo yanks a small carrier around and its jumps suffer); 1 = mass grows in step with size; 0 = every ball weighs the same. Only mass RATIOS matter here — movement, jumps and the stack magnet are all mass-independent, so this mainly controls how hard big balls shove small ones.")]
        [Range(0f, 3f)]
        [SerializeField] private float _massSizeExponent = 1f;

        public int SpawnCount => _spawnCount;
        public IReadOnlyList<SpawnEntry> Balls => _balls;

        /// <summary>Mass multiplier for a ball spawned at the given size multiplier.</summary>
        public float MassMultiplierForSize(float sizeMultiplier)
        {
            return Mathf.Pow(sizeMultiplier, _massSizeExponent);
        }

        /// <summary>Picks a spawn entry at random, honouring the entry weights.</summary>
        public SpawnEntry PickRandomEntry()
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
                if (roll <= 0f) { return entry; }
            }

            // Float rounding can leave a sliver of roll after the last entry.
            return _balls[_balls.Count - 1];
        }
    }
}
