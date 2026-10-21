namespace Cellstorm.Combat
{
    public enum StatType
    {
        MaxCells,
        CellRegen,
        Defense,
        Speed
    }

    public enum ModifierOp
    {
        /// <summary>Added to the base value.</summary>
        Add,
        /// <summary>Multiplies the (base + adds) result. Several multipliers multiply together.</summary>
        Multiply
    }

    /// <summary>
    /// A temporary or permanent change to one stat. Final value = (base + sum of Adds) * product of Multiplies.
    /// Hardening is Multiply 2 on Defense for 3 seconds; Shells is Add 2 on Defense for 10 seconds.
    /// </summary>
    public class StatModifier
    {
        public readonly StatType Stat;
        public readonly ModifierOp Op;
        public readonly float Value;
        public readonly bool Timed;
        public readonly object Source;
        public float Remaining;

        public StatModifier(StatType stat, ModifierOp op, float value, float duration, object source)
        {
            Stat = stat;
            Op = op;
            Value = value;
            Timed = duration >= 0f;
            Remaining = duration;
            Source = source;
        }
    }
}
