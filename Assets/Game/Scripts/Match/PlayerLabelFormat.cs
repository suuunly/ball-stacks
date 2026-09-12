using UnityEngine;

namespace BallStacks
{
    /// <summary>
    /// Shared formatting for naming players in match text: "P2", tinted with
    /// the player's aura colour via a TMP rich-text tag, so every display
    /// names players the same way.
    /// </summary>
    public static class PlayerLabelFormat
    {
        public static string ColouredName(PlayerController player)
        {
            string colourHex = ColorUtility.ToHtmlStringRGB(player.AuraColor);
            return $"<color=#{colourHex}>P{player.PlayerNumber}</color>";
        }
    }
}
