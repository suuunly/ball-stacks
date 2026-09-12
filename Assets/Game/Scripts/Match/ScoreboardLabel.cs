using System.Collections.Generic;
using System.Text;
using Gaman;
using TMPro;
using UnityEngine;

namespace BallStacks
{
    /// <summary>
    /// Shows every player's current stack height as one line each
    /// ("P1  84 cm"), tinted with their aura colour, on whatever TMP text it
    /// sits on. Driven purely by the scores-changed event; it never measures
    /// anything itself.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class ScoreboardLabel : MonoBehaviour
    {
        [SerializeField] private GameEventPure _onScoresChanged;

        private TMP_Text _label;
        private readonly StringBuilder _textBuilder = new StringBuilder();

        private void Awake()
        {
            _label = GetComponent<TMP_Text>();

            Assert.IsNotNull(_onScoresChanged, "ScoreboardLabel: _onScoresChanged is not assigned in the inspector!");

            _onScoresChanged.OnRaised += HandleScoresChanged;
        }

        private void OnDestroy()
        {
            if (_onScoresChanged != null) { _onScoresChanged.OnRaised -= HandleScoresChanged; }
        }

        private void HandleScoresChanged(object payload)
        {
            if (payload is not IReadOnlyList<PlayerScore> scores) { return; }

            _textBuilder.Clear();
            foreach (PlayerScore score in scores)
            {
                _textBuilder.AppendLine($"{PlayerLabelFormat.ColouredName(score.Player)}  {score.HeightCentimetres} cm");
            }

            _label.text = _textBuilder.ToString();
        }
    }
}
