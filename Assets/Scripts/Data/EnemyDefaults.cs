using UnityEngine;

namespace Cellstorm.Data
{
    /// <summary>
    /// GDD default enemy types and spawn table, created in code. Used by the editor menu to create
    /// assets, and as a runtime fallback when the spawner has no table assigned.
    /// </summary>
    public static class EnemyDefaults
    {
        public static EnemyTypeSO[] CreateTypes()
        {
            var single = ScriptableObject.CreateInstance<EnemyTypeSO>();
            single.displayName = "Single Cell";
            single.color = new Color(0.95f, 0.35f, 0.35f, 1f);
            single.maxCells = 1; single.cellRegen = 0f; single.defense = 1f; single.speed = 1f;
            single.behavior = EnemyBehavior.Chase;
            single.scoreValue = 1; single.scorePerCell = false; single.expValue = 1;

            var multi = ScriptableObject.CreateInstance<EnemyTypeSO>();
            multi.displayName = "Multi Cell";
            multi.color = new Color(0.95f, 0.60f, 0.30f, 1f);
            multi.maxCells = 10; multi.cellRegen = 1f; multi.defense = 1f; multi.speed = 1f;
            multi.behavior = EnemyBehavior.Chase;
            multi.scoreValue = 1; multi.scorePerCell = true; multi.expValue = 3;

            var ranged = ScriptableObject.CreateInstance<EnemyTypeSO>();
            ranged.displayName = "Ranged Cell";
            ranged.color = new Color(0.35f, 0.60f, 0.95f, 1f);
            ranged.maxCells = 50; ranged.cellRegen = 1f; ranged.defense = 1f; ranged.speed = 1f;
            ranged.behavior = EnemyBehavior.KeepDistance; ranged.preferredRange = 6f;
            ranged.scoreValue = 2; ranged.scorePerCell = true; ranged.expValue = 10;

            var exploding = ScriptableObject.CreateInstance<EnemyTypeSO>();
            exploding.displayName = "Exploding Cell";
            exploding.color = new Color(1f, 0.90f, 0.20f, 1f);
            exploding.maxCells = 1; exploding.cellRegen = 0f; exploding.defense = 1f; exploding.speed = 3f;
            exploding.behavior = EnemyBehavior.Explode;
            exploding.explosionTriggerRange = 0.6f; exploding.explosionRadius = 1.8f; exploding.explosionDamage = 2;
            exploding.scoreValue = 2; exploding.scorePerCell = false; exploding.expValue = 2;

            return new[] { single, multi, ranged, exploding };
        }

        /// <param name="types">Same order as CreateTypes: single, multi, ranged, exploding.</param>
        public static SpawnTableSO CreateTable(EnemyTypeSO[] types)
        {
            var table = ScriptableObject.CreateInstance<SpawnTableSO>();
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
