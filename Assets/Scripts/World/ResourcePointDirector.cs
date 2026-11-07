using Cellstorm.Core;
using Cellstorm.Enemies;
using Cellstorm.Swarm;
using UnityEngine;

namespace Cellstorm.World
{
    /// <summary>
    /// Spawns resource points (GDD V0.2 section 8.2): the first at 1:30 and then every 90 s, 15 to 25 units from the
    /// player, at most 2 at once. Not while 40 or more enemies are alive: it tries again 10 s later.
    /// </summary>
    public class ResourcePointDirector : MonoBehaviour
    {
        public static ResourcePointDirector Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Instance = null;
        }

        public const float FirstAt = 90f;
        public const float Interval = 90f;
        public const float RetrySeconds = 10f;
        public const int MaxActive = 2;
        public const int EnemyLimit = 40;

        float _timer = FirstAt;
        SwarmBody _player;

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

            _timer -= dt;
            if (_timer > 0f) return;

            if (ResourcePoint.Active.Count >= MaxActive || Enemy.Active.Count >= EnemyLimit)
            {
                _timer = RetrySeconds;       // tries again 10 seconds later
                return;
            }

            SpawnRandom();
            _timer = Interval;
        }

        public ResourcePoint SpawnRandom()
        {
            var kind = (GuardKind)Random.Range(0, 3);        // equal chance
            return Spawn(kind);
        }

        public ResourcePoint Spawn(GuardKind kind)
        {
            if (_player == null) _player = FindAnyObjectByType<SwarmBody>();
            var spawner = EnemySpawner.Instance;
            if (_player == null || spawner == null) return null;

            float angle = Random.value * Mathf.PI * 2f;
            float dist = Random.Range(15f, 25f);
            Vector2 pos = (Vector2)_player.transform.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
            int milestone = RunDirector.Instance != null ? RunDirector.Instance.Milestone : 0;
            return ResourcePoint.Create(pos, kind, milestone, spawner);
        }
    }
}
