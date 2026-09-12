using UnityEngine;

namespace Gaman
{
    /// <summary>
    /// Pure configuration for an <see cref="AudioPoolManager"/>: how many
    /// AudioSources to pre-create and what to call their container. Knows
    /// nothing about the scene.
    /// </summary>
    [CreateAssetMenu(fileName = "AudioPool", menuName = "SDE/Audio/Audio Pool")]
    public class AudioPoolSO : ScriptableObject
    {
        [SerializeField] private int _poolSize = 16;
        [SerializeField] private string _containerName = "AudioPool";

        public int PoolSize => _poolSize;
        public string ContainerName => _containerName;
    }
}
