using UnityEngine;

namespace Cellstorm.Data
{
    public enum AbilityKind
    {
        Weapon,
        Skill
    }

    /// <summary>
    /// Shell for a weapon or skill (Ram, Jaw, Gun, Hardening). Timing fields map to the shared
    /// Ready -> Shaping -> Executing -> Returning -> Cooldown state machine planned for Phase 4.
    /// </summary>
    [CreateAssetMenu(menuName = "Cellstorm/Weapon or Skill", fileName = "NewWeapon")]
    public class WeaponSO : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;
        public AbilityKind kind = AbilityKind.Weapon;

        [Header("Timing (seconds)")]
        [Min(0)] public float shapeTime = 0f;
        [Min(0)] public float executeTime = 0f;
        [Min(0)] public float cooldown = 1f;

        [Header("Cost")]
        [Range(0f, 1f)] public float cellShare = 0f;   // share of cells used, e.g. 0.1 for Jaw/Gun

        // Weapon-specific values (pierce, bullet speed, DEF multipliers, ...) are added in Phase 4.
    }
}
