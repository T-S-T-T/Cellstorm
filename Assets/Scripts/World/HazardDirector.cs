using Cellstorm.Core;
using Cellstorm.Swarm;
using UnityEngine;

namespace Cellstorm.World
{
    /// <summary>
    /// Spawns hazard patches (GDD V0.2 section 8.3): one every max(20, 45 - 5 x milestone) seconds (milestones above 6
    /// reuse milestone 6), at most 3 at once, 6 to 14 units from the player, radius 2 to 3, toxic or current 50/50.
    /// Timers follow the pause because they use scaled time and only run while playing.
    /// </summary>
    public class HazardDirector : MonoBehaviour
    {
        public static HazardDirector Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Instance = null;
        }

        public const int MaxActive = 3;

        float _timer = -1f;
        SwarmBody _player;

        public static float IntervalFor(int milestone)
        {
            return Mathf.Max(20f, 45f - 5f * Mathf.Min(milestone, 6));
        }

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.IsPlaying) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            if (_player == null) _player = FindAnyObjectByType<SwarmBody>();
            if (_player == null || _player.IsDead) return;

            int milestone = RunDirector.Instance != null ? RunDirector.Instance.Milestone : 0;
            if (_timer < 0f) _timer = IntervalFor(milestone);

            _timer -= dt;
            if (_timer > 0f) return;
            _timer = IntervalFor(milestone);

            if (HazardPatch.Active.Count >= MaxActive) return;
            SpawnRandom();
        }

        public HazardPatch SpawnRandom()
        {
            if (_player == null) _player = FindAnyObjectByType<SwarmBody>();
            if (_player == null) return null;
            var kind = Random.value < 0.5f ? HazardKind.Toxic : HazardKind.Current;
            return Spawn(kind);
        }

        public HazardPatch Spawn(HazardKind kind)
        {
            if (_player == null) _player = FindAnyObjectByType<SwarmBody>();
            Vector2 center = _player != null ? (Vector2)_player.transform.position : Vector2.zero;
            float angle = Random.value * Mathf.PI * 2f;
            float dist = Random.Range(6f, 14f);
            Vector2 pos = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
            float radius = Random.Range(2f, 3f);
            float dirAngle = Random.value * Mathf.PI * 2f;
            return HazardPatch.Create(pos, kind, radius, new Vector2(Mathf.Cos(dirAngle), Mathf.Sin(dirAngle)));
        }
    }
}
