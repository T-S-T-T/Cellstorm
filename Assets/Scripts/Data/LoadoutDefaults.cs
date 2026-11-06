using UnityEngine;

namespace Cellstorm.Data
{
    /// <summary>GDD V0.1.1 starting loadouts. They change stats, and the Giant also the body size and weapon cell count.</summary>
    public static class LoadoutDefaults
    {
        public static StatBlockSO[] CreateAll()
        {
            return new[]
            {
                Make("Default", 100f, 1f, 2f, 2f, 1f, 100),
                Make("Speedy", 100f, 1f, 2f, 5f, 1f, 100),
                Make("Tank", 100f, 1f, 5f, 1f, 1f, 100),
                Make("Giant", 100f, 1f, 2f, 2f, 2f, 200)
            };
        }

        static StatBlockSO Make(string name, float maxHp, float regen, float def, float speed, float size, int weaponCells)
        {
            var b = ScriptableObject.CreateInstance<StatBlockSO>();
            b.name = name;
            b.maxHp = maxHp;
            b.hpRegen = regen;
            b.defense = def;
            b.speed = speed;
            b.sizeMultiplier = size;
            b.weaponCellCount = weaponCells;
            return b;
        }
    }
}
