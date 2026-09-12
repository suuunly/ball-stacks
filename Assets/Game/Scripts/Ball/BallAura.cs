using Gaman;
using UnityEngine;

namespace BallStacks
{
    /// <summary>
    /// The player-coloured aura around a ball: pulsing while a joining player
    /// targets it, steady while a player controls it. The colour comes from
    /// the player, so rendering uses a per-renderer property block instead of
    /// touching the shared material.
    /// </summary>
    public class BallAura : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int PulseAmountId = Shader.PropertyToID("_PulseAmount");

        private const float TargetingPulseAmount = 0.35f;
        private const float ControlledPulseAmount = 0f;

        [SerializeField] private MeshRenderer _auraRenderer;

        private MaterialPropertyBlock _propertyBlock;

        // Lazily created instead of cached in Awake: a mid-play domain reload
        // (script recompile while testing) wipes non-serialized state and
        // Awake does not run again.
        private MaterialPropertyBlock PropertyBlock
        {
            get
            {
                if (_propertyBlock == null) { _propertyBlock = new MaterialPropertyBlock(); }
                return _propertyBlock;
            }
        }

        private void Awake()
        {
            Assert.IsNotNull(_auraRenderer, "BallAura: _auraRenderer is not assigned in the inspector!");

            Hide();
        }

        /// <summary>Pulsing aura while a joining player is targeting this ball.</summary>
        public void ShowTargeting(Color color)
        {
            Show(color, TargetingPulseAmount);
        }

        /// <summary>Steady aura while a player is controlling this ball.</summary>
        public void ShowControlled(Color color)
        {
            Show(color, ControlledPulseAmount);
        }

        private void Show(Color color, float pulseAmount)
        {
            PropertyBlock.SetColor(BaseColorId, color);
            PropertyBlock.SetFloat(PulseAmountId, pulseAmount);
            _auraRenderer.SetPropertyBlock(PropertyBlock);
            _auraRenderer.enabled = true;
        }

        public void Hide()
        {
            _auraRenderer.enabled = false;
        }
    }
}
