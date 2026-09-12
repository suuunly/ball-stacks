using Gaman;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BallStacks
{
    /// <summary>
    /// One player's score badge: tints its background with the player's aura
    /// colour and shows their current stacked height in centimetres. Spawned
    /// and driven by <see cref="ScoreHud"/> — one badge per joined player.
    /// </summary>
    public class PlayerScoreBadge : MonoBehaviour
    {
        [Tooltip("The graphic tinted with the player's aura colour.")]
        [SerializeField] private Graphic _background;

        [Tooltip("The text showing the player's stacked centimetres.")]
        [SerializeField] private TMP_Text _valueLabel;

        private void Awake()
        {
            Assert.IsNotNull(_background, "PlayerScoreBadge: _background is not assigned in the inspector!");
            Assert.IsNotNull(_valueLabel, "PlayerScoreBadge: _valueLabel is not assigned in the inspector!");
        }

        /// <summary>Colours the badge for its player. Called once when the badge is spawned.</summary>
        public void Bind(PlayerController player)
        {
            _background.color = player.AuraColor;
        }

        public void ShowScore(PlayerScore score)
        {
            _valueLabel.text = $"{score.HeightCentimetres} cm";
        }
    }
}
