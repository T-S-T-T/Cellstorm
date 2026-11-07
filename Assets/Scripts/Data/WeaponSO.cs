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
        Hardening,
        // V0.2
        Whip,
        SpreadShot,
        SporeCloud,
        Mine,
        Minion,
        Convert,
        Shockwave
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
        [Tooltip("Ram: dash window 0.3. Jaw bite: 0.1. Gun: 0. Hardening: effect duration.")]
        [Min(0)] public float executeTime = 0f;
        [Min(0)] public float cooldown = 1f;
        [Tooltip("Cooldown applied when the key is released before the shape is complete.")]
        [Min(0)] public float cancelCooldown = 0.5f;

        [Header("Damage")]
        [Tooltip("Damage of one hit (Ram dash hit, Jaw bite, Gun bullet), reduced by the target's DEF. " +
                 "V0.1.1 defaults: Ram 30, Jaw 150, Gun 100, enemy Ram 20 / Jaw 40 / Gun 20.")]
        [Min(0)] public float damage = 100f;

        [Header("Cost")]
        [Tooltip("Share of MAX HP the deployed cells hold (0.1 = 10%). Potent Cells raises it to 0.3. Ram: 0.")]
        [Range(0f, 1f)] public float cellShare = 0f;

        [Header("Turning")]
        [Tooltip("Turn rate multiplier while shaping / executing (reduced turning rate).")]
        [Range(0f, 1f)] public float turnRateWhileActive = 0.3f;

        [Header("Ram")]
        [Tooltip("Dash speed as a multiple of the swarm's moving speed. The dash window is executeTime (0.3 s).")]
        public float dashMultiplier = 5f;

        [Header("Jaw")]
        public float coneAngle = 90f;
        [Tooltip("How far the bite reaches beyond the swarm's edge.")]
        public float coneRange = 2f;

        [Header("Gun")]
        public float bulletSpeedMultiplier = 2f;
        [Tooltip("Improvable. Player bullets never pierce bosses.")]
        [Min(0)] public int pierce = 0;
        public float bulletRadius = 0.2f;

        [Header("Skill (Hardening)")]
        public float defMultiplier = 2f;

        [Header("Whip (V0.2)")]
        public float whipArc = 120f;
        [Tooltip("How far the lash reaches beyond the swarm's edge.")]
        public float whipRange = 3f;
        [Tooltip("Pull toward the swarm, in units.")]
        public float pullDistance = 1.5f;

        [Header("Spread Shot (V0.2)")]
        public int pellets = 5;
        public float fanAngle = 40f;
        public float pelletRange = 6f;

        [Header("Spore Cloud (V0.2)")]
        public float cloudRadius = 2.5f;
        public float cloudDuration = 4f;
        [Tooltip("Toxic HP per second applied to everything in the cloud (lasts toxicDuration after leaving).")]
        public float toxicDps = 5f;
        public float toxicDuration = 1f;

        [Header("Mine (V0.2)")]
        public float mineArmTime = 1f;
        public float mineTriggerRange = 1.2f;
        public float mineLifetime = 15f;
        public float blastRadius = 2f;

        [Header("Minion skill (V0.2)")]
        [Tooltip("Share of the main swarm's max HP each minion costs (0.025 = 2.5%).")]
        public float minionCostFraction = 0.025f;
        public float minionContactDps = 10f;
        public int minionCount = 1;

        [Header("Convert skill (V0.2)")]
        [Tooltip("Share of the damage dealt that comes back as an HP orb (0.1 = 10%).")]
        public float orbFraction = 0.1f;

        [Header("Shockwave skill (V0.2)")]
        public float shockRadius = 5f;
        public float shockPush = 4f;
        public float shockStun = 1f;
    }
}
