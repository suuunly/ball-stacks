using Gaman;
using UnityEngine;

namespace BallStacks
{
    /// <summary>
    /// Tuning data for the stack sound effects: which jump clip plays at
    /// which stack size, the stack-fall clip, and the pitch, volume and
    /// spatial knobs — all designer-editable without code changes.
    /// </summary>
    [CreateAssetMenu(fileName = "StackSfxConfig", menuName = "Ball Stacks/Stack Sfx Config")]
    public class StackSfxConfigSO : ScriptableObject
    {
        [Tooltip("Jump clip per stack size: element 0 plays with no stack (just the controlled ball), element 1 with one ball stacked, and so on. Stacks taller than the list reuse the last clip.")]
        [SerializeField] private AudioClip[] _jumpClipsByStackSize;

        [Tooltip("Played once when a stack that reached Min Stack For Fall collapses back to nothing.")]
        [SerializeField] private AudioClip _stackFallClip;

        [Tooltip("Every play is pitch-shifted by a random amount within ± this range so the clips never sound canned.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _pitchJitter = 0.08f;

        [Range(0f, 1f)]
        [SerializeField] private float _jumpVolume = 1f;

        [Range(0f, 1f)]
        [SerializeField] private float _fallVolume = 1f;

        [Tooltip("0 = flat 2D everywhere, 1 = fully positional 3D at the tower.")]
        [Range(0f, 1f)]
        [SerializeField] private float _spatialBlend = 1f;

        [Tooltip("A collapse only plays the fall clip if the stack reached at least this many balls before dropping back to zero — losing a single ball is not a fallen tower.")]
        [Min(1)]
        [SerializeField] private int _minStackForFall = 2;

        public AudioClip StackFallClip => _stackFallClip;
        public float JumpVolume => _jumpVolume;
        public float FallVolume => _fallVolume;
        public float SpatialBlend => _spatialBlend;
        public int MinStackForFall => _minStackForFall;

        public AudioClip GetJumpClip(int stackSize)
        {
            Assert.IsTrue(_jumpClipsByStackSize != null && _jumpClipsByStackSize.Length > 0,
                "StackSfxConfigSO: _jumpClipsByStackSize is empty!");

            int clipIndex = Mathf.Clamp(stackSize, 0, _jumpClipsByStackSize.Length - 1);
            return _jumpClipsByStackSize[clipIndex];
        }

        public float NextPitch()
        {
            return 1f + Random.Range(-_pitchJitter, _pitchJitter);
        }
    }
}
