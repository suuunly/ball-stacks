using System.Text;
using Gaman;
using TMPro;
using UnityEngine;

namespace BallStacks
{
    /// <summary>
    /// Announces the match result on whatever TMP text it sits on — "P2 wins
    /// with 5 balls!", or names everyone tied on a draw. Stays empty until the
    /// winner-decided event fires.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class WinnerLabel : MonoBehaviour
    {
        [SerializeField] private GameEventPure _onWinnerDecided;

        private TMP_Text _label;

        private void Awake()
        {
            _label = GetComponent<TMP_Text>();

            Assert.IsNotNull(_onWinnerDecided, "WinnerLabel: _onWinnerDecided is not assigned in the inspector!");

            _onWinnerDecided.OnRaised += HandleWinnerDecided;
            _label.text = string.Empty;
        }

        private void OnDestroy()
        {
            if (_onWinnerDecided != null) { _onWinnerDecided.OnRaised -= HandleWinnerDecided; }
        }

        private void HandleWinnerDecided(object payload)
        {
            if (payload is not MatchResult result) { return; }

            _label.text = BuildAnnouncement(result);
        }

        private static string BuildAnnouncement(MatchResult result)
        {
            bool nobodyPlayed = result.Winners.Count == 0;
            if (nobodyPlayed) { return "Time! Nobody joined the match."; }

            if (result.IsDraw) { return BuildDrawAnnouncement(result); }

            PlayerScore winner = result.Winners[0];
            return $"{PlayerLabelFormat.ColouredName(winner.Player)} wins at {winner.HeightCentimetres} cm!";
        }

        private static string BuildDrawAnnouncement(MatchResult result)
        {
            var names = new StringBuilder();
            for (int i = 0; i < result.Winners.Count; i++)
            {
                bool isFirstName = i == 0;
                if (!isFirstName) { names.Append(" & "); }

                names.Append(PlayerLabelFormat.ColouredName(result.Winners[i].Player));
            }

            int tiedCentimetres = result.Winners[0].HeightCentimetres;
            return $"Draw! {names} tied at {tiedCentimetres} cm.";
        }
    }
}
