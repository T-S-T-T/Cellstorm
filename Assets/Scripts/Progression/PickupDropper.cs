using Cellstorm.Enemies;
using UnityEngine;

namespace Cellstorm.Progression
{
    /// <summary>Drops EXP, rerolls and shells when an enemy is defeated.</summary>
    public class PickupDropper : MonoBehaviour
    {
        [Tooltip("GDD: 1% chance to drop a reroll when defeating an enemy.")]
        [SerializeField, Range(0f, 1f)] float rerollChance = 0.01f;
        [Tooltip("PLACEHOLDER: the GDD has no drop source for shells yet.")]
        [SerializeField, Range(0f, 1f)] float shellsChance = 0.02f;
        [Tooltip("PLACEHOLDER: bosses always drop shells.")]
        [SerializeField] bool bossAlwaysDropsShells = true;
        [Tooltip("When there are more pickups than this, the oldest EXP orb is merged into the new one.")]
        [SerializeField] int maxPickups = 300;

        void OnEnable()
        {
            Enemy.Killed += OnEnemyKilled;
        }

        void OnDisable()
        {
            Enemy.Killed -= OnEnemyKilled;
        }

        void OnEnemyKilled(Enemy e)
        {
            Vector2 pos = e.transform.position;

            int exp = e.Spec.expValue;
            if (exp > 0) SpawnExp(pos, exp);

            if (Random.value < rerollChance)
                Pickup.Spawn(PickupType.Reroll, pos + Random.insideUnitCircle * 0.4f);

            if ((e.IsBoss && bossAlwaysDropsShells) || Random.value < shellsChance)
                Pickup.Spawn(PickupType.Shells, pos + Random.insideUnitCircle * 0.4f);
        }

        void SpawnExp(Vector2 pos, int value)
        {
            if (Pickup.Active.Count >= maxPickups)
            {
                for (int i = 0; i < Pickup.Active.Count; i++)
                {
                    var old = Pickup.Active[i];
                    if (old != null && old.Type == PickupType.Exp)
                    {
                        value += old.Value;
                        Destroy(old.gameObject);
                        break;
                    }
                }
            }
            Pickup.Spawn(PickupType.Exp, pos + Random.insideUnitCircle * 0.3f, value);
        }
    }
}
