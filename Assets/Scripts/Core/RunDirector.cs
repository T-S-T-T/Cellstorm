using Cellstorm.Enemies;
using UnityEngine;

namespace Cellstorm.Core
{
    /// <summary>
    /// Owns the run clock, milestones, score and the win condition (GDD section 2).
    /// Milestone N begins at N x 5:00 and triggers Boss N. After the final milestone the clock keeps
    /// running but no new milestone happens until the player chooses Endless after winning.
    /// </summary>
    public class RunDirector : MonoBehaviour
    {
        public static RunDirector Instance { get; private set; }

        /// <summary>Raised when a new milestone starts (the new milestone number, 1 or higher).</summary>
        public static event System.Action<int> MilestoneReached;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Instance = null;
            MilestoneReached = null;
        }

        [SerializeField] float milestoneIntervalSeconds = 300f;
        [Tooltip("Defeating this boss wins the run (GDD: 6).")]
        [SerializeField] int finalMilestone = 6;

        float _nextMilestoneAt;

        public float Elapsed { get; private set; }
        public int Milestone { get; private set; }
        public int Score { get; private set; }
        public bool Endless { get; private set; }
        public int FinalMilestone => finalMilestone;
        public bool AtFinalMilestone => !Endless && Milestone >= finalMilestone;
        public float SecondsToNextMilestone => Mathf.Max(0f, _nextMilestoneAt - Elapsed);

        void Awake()
        {
            Instance = this;
            _nextMilestoneAt = milestoneIntervalSeconds;
        }

        void OnEnable()
        {
            Enemy.Killed += OnEnemyKilled;
        }

        void OnDisable()
        {
            Enemy.Killed -= OnEnemyKilled;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

            Elapsed += Time.deltaTime;

            if (!AtFinalMilestone && Elapsed >= _nextMilestoneAt)
            {
                Milestone++;
                _nextMilestoneAt += milestoneIntervalSeconds;
                MilestoneReached?.Invoke(Milestone);
            }
        }

        void OnEnemyKilled(Enemy e)
        {
            Score += e.ScoreValue;

            if (e.IsBoss && !Endless && e.Spec.milestone >= finalMilestone)
            {
                if (GameManager.Instance != null) GameManager.Instance.SetState(GameState.Won);
            }
        }

        /// <summary>Called after winning: keep playing for a high score.</summary>
        public void StartEndless()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Won) return;

            Endless = true;
            _nextMilestoneAt = Elapsed + milestoneIntervalSeconds;
            GameManager.Instance.SetState(GameState.Playing);
        }

        // Debug helpers
        public void DebugJumpToNextMilestone()
        {
            if (!AtFinalMilestone) Elapsed = Mathf.Max(Elapsed, _nextMilestoneAt);
        }

        public void DebugAddTime(float seconds)
        {
            Elapsed += seconds;
        }
    }
}
