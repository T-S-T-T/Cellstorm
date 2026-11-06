using UnityEngine;

namespace Cellstorm.Data
{
    /// <summary>
    /// GDD V0.1.1 default enemy types and spawn table, created in code. Used by the editor menu to create
    /// assets, and as a runtime fallback when the spawner has no table assigned.
    /// </summary>
    public static class EnemyDefaults
    {
        public static EnemyTypeSO[] CreateTypes()
        {
            var single = ScriptableObject.CreateInstance<EnemyTypeSO>();
            single.name = "SingleCell";
            single.displayName = "Single Cell";
            single.color = new Color(0.95f, 0.35f, 0.35f, 1f);
            single.maxHp = 10f; single.defense = 1f; single.speed = 1f; single.contactDps = 5f;
            single.behavior = EnemyBehavior.Chase;
            single.scoreValue = 1; single.expValue = 1;

            var multi = ScriptableObject.CreateInstance<EnemyTypeSO>();
            multi.name = "MultiCell";
            multi.displayName = "Multi Cell";
            multi.color = new Color(0.95f, 0.60f, 0.30f, 1f);
            multi.maxHp = 100f; multi.defense = 1f; multi.speed = 1f; multi.contactDps = 10f;
            multi.behavior = EnemyBehavior.Chase;
            multi.scoreValue = 10; multi.expValue = 3;

            var ranged = ScriptableObject.CreateInstance<EnemyTypeSO>();
            ranged.name = "RangedCell";
            ranged.displayName = "Ranged Cell";
            ranged.color = new Color(0.35f, 0.60f, 0.95f, 1f);
            ranged.maxHp = 500f; ranged.defense = 1f; ranged.speed = 1f; ranged.contactDps = 10f;
            ranged.behavior = EnemyBehavior.KeepDistance; ranged.preferredRange = 6f;
            ranged.scoreValue = 100; ranged.expValue = 10;

            var exploding = ScriptableObject.CreateInstance<EnemyTypeSO>();
            exploding.name = "ExplodingCell";
            exploding.displayName = "Exploding Cell";
            exploding.color = new Color(1f, 0.90f, 0.20f, 1f);
            exploding.maxHp = 10f; exploding.defense = 1f; exploding.speed = 3f; exploding.contactDps = 5f;
            exploding.behavior = EnemyBehavior.Explode;
            exploding.explosionTriggerRange = 0.6f; exploding.explosionRadius = 1.8f; exploding.explosionDamage = 20f;
            exploding.scoreValue = 2; exploding.expValue = 2;

            return new[] { single, multi, ranged, exploding };
        }

        /// <param name="types">Same order as CreateTypes: single, multi, ranged, exploding.</param>
        public static SpawnTableSO CreateTable(EnemyTypeSO[] types)
        {
            var table = ScriptableObject.CreateInstance<SpawnTableSO>();
            table.name = "SpawnTable";
            table.entries = new[]
            {
                new SpawnTableSO.Entry { type = types[0], ratesPerSecond = new[] { 0.5f, 0.6f, 0.7f, 0.8f, 0.9f, 1.0f, 0f } },
                new SpawnTableSO.Entry { type = types[1], ratesPerSecond = new[] { 0f, 0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 2f } },
                new SpawnTableSO.Entry { type = types[2], ratesPerSecond = new[] { 0f, 0f, 0f, 0.1f, 0.2f, 0.2f, 1f } },
                new SpawnTableSO.Entry { type = types[3], ratesPerSecond = new[] { 0f, 0f, 0f, 0f, 0f, 0.1f, 1f } },
            };
            return table;
        }
    }
}
