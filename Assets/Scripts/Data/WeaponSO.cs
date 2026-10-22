using UnityEngine;

namespace Cellstorm.Data
{
    public enum AbilityKind
    {
        Weapon,
        Skill
    }

    public enum WeaponType
    {
        Ram,
        Jaw,
        Gun,
        Hardening
    }

    /// <summary>
    /// One weapon or skill. Ram / Hardening are instant; Jaw / Gun use the shape -> execute flow.
    /// Values marked "improvable" can be changed by upgrades later (attack levels in V0.2).
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
        public WeaponType type = WeaponType.Ram;

        [Header("Timing (seconds)")]
        [Tooltip("Time holding the key to form the weapon (improvable). Jaw / Gun only.")]
        [Min(0)] public float shapeTime = 0f;
        [Tooltip("Ram: 0. Jaw bite: 0.1. Gun: 0. Hardening: effect duration.")]
        [Min(0)] public float executeTime = 0f;
        [Min(0)] public float cooldown = 1f;
        [Tooltip("Cooldown applied when the key is released before the shape is complete.")]
        [Min(0)] public float cancelCooldown = 0.5f;

        [Header("Cost")]
        [Tooltip("Share of the cells that leave the body to form the weapon (0.1 = 10%).")]
        [Range(0f, 1f)] public float cellShare = 0f;

        [Header("Turning")]
        [Tooltip("Turn rate multiplier while shaping / executing (reduced turning rate).")]
        [Range(0f, 1f)] public float turnRateWhileActive = 0.3f;

        [Header("Ram")]
        public float dashSpeed = 12f;

        [Header("Jaw")]
        public float coneAngle = 90f;
        [Tooltip("How far the bite reaches beyond the swarm's edge.")]
        public float coneRange = 2f;
        public float biteDefMultiplier = 2f;

        [Header("Gun")]
        public float bulletSpeedMultiplier = 2f;
        public float bulletDefMultiplier = 2f;
        [Tooltip("Improvable. Player bullets never pierce bosses.")]
        [Min(0)] public int pierce = 0;
        public float bulletRadius = 0.2f;

        [Header("Skill (Hardening)")]
        public float defMultiplier = 2f;

        [Header("Weapon hit rule")]
        [Tooltip("GDD: for weapons, higher DEF means auto win. True = a hit with higher DEF defeats the target " +
                 "outright. False = uses the contact damage formula as a one-off hit.")]
        public bool autoWin = true;
    }
}
