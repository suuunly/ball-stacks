using System.Collections.Generic;
using Gaman;
using UnityEngine;

namespace BallStacks
{
    /// <summary>
    /// Spawns one <see cref="PlayerScoreBadge"/> per joined player, colour-
    /// coded with their aura, and keeps every badge's centimetre reading
    /// current. Purely event-driven off the scores-changed event, so it also
    /// picks up players the moment they join mid-round. Badges sit one per
    /// screen corner by player number: P1 top-left, P2 top-right,
    /// P3 bottom-left, P4 bottom-right.
    /// </summary>
    public class ScoreHud : MonoBehaviour
    {
        // Corner per player number (1-based), matching the aura colours'
        // join order: red TL, blue TR, green BL, yellow BR.
        private static readonly Vector2[] CornerAnchors =
        {
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
        };

        [Tooltip("Raised by the StackScoreboard whenever any player's stack changes.")]
        [SerializeField] private GameEventPure _onScoresChanged;

        [Tooltip("The badge spawned per player — the Score prefab.")]
        [SerializeField] private PlayerScoreBadge _badgePrefab;

        [Tooltip("Badges anchor to the corners of this rect. Empty = this object, stretched to fill its parent (the canvas).")]
        [SerializeField] private RectTransform _badgeContainer;

        [Tooltip("How far (px) each badge's centre sits in from its screen corner.")]
        [SerializeField] private Vector2 _cornerInset = new Vector2(180f, 70f);

        private readonly Dictionary<PlayerController, PlayerScoreBadge> _badges =
            new Dictionary<PlayerController, PlayerScoreBadge>();
        private readonly List<PlayerController> _departedPlayers = new List<PlayerController>();

        private void Awake()
        {
            if (_badgeContainer == null)
            {
                _badgeContainer = (RectTransform)transform;
                StretchToFillParent(_badgeContainer);
            }

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

            foreach (PlayerScore score in scores)
            {
                if (!_badges.TryGetValue(score.Player, out PlayerScoreBadge badge))
                {
                    badge = CreateBadge(score.Player);
                }

                badge.ShowScore(score);
            }
        }

        private PlayerScoreBadge CreateBadge(PlayerController player)
        {
            PlayerScoreBadge badge = Instantiate(_badgePrefab, _badgeContainer);
            badge.Bind(player);
            _badges.Add(player, badge);

            PlaceInCorner((RectTransform)badge.transform, player.PlayerNumber);
            return badge;
        }

        private void PlaceInCorner(RectTransform badgeRect, int playerNumber)
        {
            Vector2 corner = CornerAnchors[(playerNumber - 1) % CornerAnchors.Length];
            badgeRect.anchorMin = corner;
            badgeRect.anchorMax = corner;
            badgeRect.pivot = new Vector2(0.5f, 0.5f);

            // Mirror the inset so the badge's centre always sits inward from
            // its corner, whichever side of the screen that is.
            float insetX = _cornerInset.x;
            if (corner.x > 0.5f) { insetX = -insetX; }
            float insetY = _cornerInset.y;
            if (corner.y > 0.5f) { insetY = -insetY; }

            badgeRect.anchoredPosition = new Vector2(insetX, insetY);
        }

        private static void StretchToFillParent(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
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
