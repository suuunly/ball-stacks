using UnityEngine;

namespace Gaman
{
    /// <summary>
    /// A fixed pool of reusable AudioSources for one-shot sound effects.
    /// Callers request playback through <see cref="PlayOneShot"/> without
    /// owning an AudioSource, and the pool cycles through its sources with a
    /// circular index — no Instantiate/Destroy, no GC pressure. If sounds cut
    /// each other off, raise the pool size in the config asset.
    /// </summary>
    public class AudioPoolManager : MonoBehaviour, IRuntime
    {
        [SerializeField] private AudioPoolSO _config;
        [SerializeField] private RuntimeSet _runtimeSet;

        private AudioSource[] _sources;
        private int _currentIndex;

        private void Awake()
        {
            Assert.IsNotNull(_config, "AudioPoolManager: _config is not assigned in the inspector!");
            Assert.IsNotNull(_runtimeSet, "AudioPoolManager: _runtimeSet is not assigned in the inspector!");

            InitialisePool();
            _runtimeSet.Add(this);
        }

        private void OnDestroy()
        {
            if (_runtimeSet != null) { _runtimeSet.Remove(this); }
        }

        private void InitialisePool()
        {
            var container = new GameObject(_config.ContainerName);
            container.transform.SetParent(transform);

            _sources = new AudioSource[_config.PoolSize];

            for (int i = 0; i < _config.PoolSize; i++)
            {
                var go = new GameObject($"AudioSource_{i}");
                go.transform.SetParent(container.transform);

                var source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                _sources[i] = source;
            }
        }

        public void PlayOneShot(
            AudioClip clip,
            Vector3 position,
            float volume = 1f,
            float pitch = 1f,
            float spatialBlend = 1f)
        {
            Assert.IsNotNull(clip, "AudioPoolManager: clip must not be null!");

            AudioSource source = _sources[_currentIndex];
            _currentIndex = (_currentIndex + 1) % _sources.Length;

            source.transform.position = position;
            source.clip = clip;
            source.volume = volume;
            source.pitch = pitch;
            source.spatialBlend = spatialBlend;
            source.Play();
        }
    }
}
