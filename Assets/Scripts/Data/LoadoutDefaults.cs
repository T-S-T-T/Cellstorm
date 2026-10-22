using UnityEngine;

namespace Cellstorm.Data
{
    /// <summary>GDD starting loadouts. They change stats only; every run starts with Ram.</summary>
    public static class LoadoutDefaults
    {
        public static StatBlockSO[] CreateAll()
        {
            return new[]
            {
                Make("Default", 5, 1f, 2f, 2f),
                Make("Speedy", 5, 1f, 2f, 5f),
                Make("Tank", 5, 1f, 5f, 1f)
            };
        }

        static StatBlockSO Make(string name, int cells, float regen, float def, float speed)
        {
            var b = ScriptableObject.CreateInstance<StatBlockSO>();
            b.name = name;
            b.maxCells = cells;
            b.cellRegen = regen;
            b.defense = def;
            b.speed = speed;
            return b;
        }
    }
}
