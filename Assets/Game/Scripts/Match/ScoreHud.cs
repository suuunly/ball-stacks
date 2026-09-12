using System.Collections.Generic;
using Gaman;
using UnityEngine;
using UnityEngine.UI;

namespace BallStacks
{
    /// <summary>
    /// Spawns one <see cref="PlayerScoreBadge"/> per joined player, colour-
    /// coded with their aura, and keeps every badge's centimetre reading
    /// current. Purely event-driven off the scores-changed event, so it also
    /// picks up players the moment they join mid-round. Badges are laid out
    /// by a LayoutGroup on the container when one exists; without one, each
    /// badge is offset from the previous by a fixed step.
    /// </summary>
    public class ScoreHud : MonoBehaviour
    {
        [Tooltip("Raised by the StackScoreboard whenever any player's stack changes.")]
        [SerializeField] private GameEventPure _onScoresChanged;

        [Tooltip("The badge spawned per player — the Score prefab.")]
        [SerializeField] private PlayerScoreBadge _badgePrefab;

        [Tooltip("Where badges are parented. Empty = this object. Add a LayoutGroup here to control placement; without one, badges step by Badge Step.")]
        [SerializeField] private RectTransform _badgeContainer;

        [Tooltip("Offset from one badge to the next when the container has no LayoutGroup.")]
        [SerializeField] private Vector2 _badgeStep = new Vector2(320f, 0f);

        private readonly Dictionary<PlayerController, PlayerScoreBadge> _badges =
            new Dictionary<PlayerController, PlayerScoreBadge>();
        private readonly List<PlayerController> _departedPlayers = new List<PlayerController>();
        private bool _containerHasLayoutGroup;

        private void Awake()
        {
            if (_badgeContainer == null) { _badgeContainer = (RectTransform)transform; }
            _containerHasLayoutGroup = _badgeContainer.TryGetComponent<LayoutGroup>(out _);

            Assert.IsNotNull(_onScoresChanged, "ScoreHud: _onScoresChanged is not assigned in the inspector!");
            Assert.IsNotNull(_badgePrefab, "ScoreHud: _badgePrefab is not assigned in the inspector!");

            _onScoresChanged.OnRaised += HandleScoresChanged;
        }

        private void OnDestroy()
        {
            if (_onScoresChanged != null) { _onScoresChanged.OnRaised -= HandleScoresChanged; }
        }

        private void HandleScoresChanged(object payload)
        {
            if (payload is not IReadOnlyList<PlayerScore> scores) { return; }

            RemoveDepartedBadges(scores);

            for (int i = 0; i < scores.Count; i++)
            {
                PlayerScore score = scores[i];
                if (!_badges.TryGetValue(score.Player, out PlayerScoreBadge badge))
                {
                    badge = CreateBadge(score.Player, i);
                }

                badge.ShowScore(score);
            }
        }

        private PlayerScoreBadge CreateBadge(PlayerController player, int badgeIndex)
        {
            PlayerScoreBadge badge = Instantiate(_badgePrefab, _badgeContainer);
            badge.Bind(player);
            _badges.Add(player, badge);

            if (!_containerHasLayoutGroup)
            {
                var badgeRect = (RectTransform)badge.transform;
                badgeRect.anchoredPosition += _badgeStep * badgeIndex;
            }

            return badge;
        }

        private void RemoveDepartedBadges(IReadOnlyList<PlayerScore> scores)
        {
            _departedPlayers.Clear();
            foreach (KeyValuePair<PlayerController, PlayerScoreBadge> entry in _badges)
            {
                bool playerStillScored = false;
                foreach (PlayerScore score in scores)
                {
                    if (score.Player == entry.Key)
                    {
                        playerStillScored = true;
                        break;
                    }
                }

                if (!playerStillScored) { _departedPlayers.Add(entry.Key); }
            }

            foreach (PlayerController departedPlayer in _departedPlayers)
            {
                Destroy(_badges[departedPlayer].gameObject);
                _badges.Remove(departedPlayer);
            }
        }
    }
}
