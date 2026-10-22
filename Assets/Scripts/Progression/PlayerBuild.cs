using System.Collections.Generic;
using System.Text;
using Cellstorm.Combat;
using Cellstorm.Data;
using Cellstorm.Weapons;
using UnityEngine;

namespace Cellstorm.Progression
{
    /// <summary>
    /// What the player has taken this run (upgrade stacks, evolutions) and the code that applies upgrades.
    /// Lives on the Player object.
    /// </summary>
    [RequireComponent(typeof(SwarmStats))]
    public class PlayerBuild : MonoBehaviour
    {
        public static PlayerBuild Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Instance = null;
        }

        readonly Dictionary<string, int> _stacks = new Dictionary<string, int>();
        readonly List<UpgradeSO> _taken = new List<UpgradeSO>();
        SwarmStats _stats;
        AbilityController _abilities;
        float _growthEffect = 1f;

        public SwarmStats Stats => _stats;
        public bool TankEvolved { get; private set; }

        void Awake()
        {
            Instance = this;
            _stats = GetComponent<SwarmStats>();
            _abilities = GetComponent<AbilityController>();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public int Stacks(string id)
        {
            return id != null && _stacks.TryGetValue(id, out var n) ? n : 0;
        }

        public int Stacks(UpgradeSO u)
        {
            return u != null ? Stacks(u.id) : 0;
        }

        /// <summary>Can this upgrade appear in the level-up selection right now?</summary>
        public bool IsEligible(UpgradeSO u)
        {
            if (u == null) return false;
            if (!Cellstorm.Core.UnlockState.IsUnlocked(u.unlockId)) return false;
            if (!u.repeatable && Stacks(u) > 0) return false;

            if (u.effect == UpgradeEffect.GrantWeapon)
            {
                if (u.weapon == null) return false;
                if (OwnsWeaponType(u.weapon.type)) return false; // GDD: owned items never reappear
            }

            if (u.prerequisites != null)
            {
                foreach (var p in u.prerequisites)
                    if (p != null && Stacks(p) < 1) return false;
            }

            if (u.requirementUpgrade != null && Stacks(u.requirementUpgrade) < u.requirementStacks) return false;

            if (u.lockedOutBy != null)
            {
                foreach (var l in u.lockedOutBy)
                    if (l != null && Stacks(l) > 0) return false;
            }
            return true;
        }

        bool OwnsWeaponType(WeaponType type)
        {
            if (_abilities == null) return false;
            for (int i = 0; i < AbilityController.SlotCount; i++)
            {
                var a = _abilities.GetSlot(i);
                if (a != null && a.Data.type == type) return true;
            }
            return false;
        }

        /// <summary>First empty slot of the right kind, or -1 if the weapon / skill would replace something.</summary>
        public int FindFreeSlot(UpgradeSO u)
        {
            if (_abilities == null || u.weapon == null) return -1;
            int start = u.weapon.kind == AbilityKind.Weapon ? 0 : 2;
            for (int i = start; i < start + 2; i++)
                if (_abilities.GetSlot(i) == null) return i;
            return -1;
        }

        public bool NeedsReplaceTarget(UpgradeSO u)
        {
            return u.effect == UpgradeEffect.GrantWeapon && FindFreeSlot(u) < 0;
        }

        /// <summary>Slot indices a weapon / skill of this upgrade can go into.</summary>
        public static int[] SlotsFor(UpgradeSO u)
        {
            return u.weapon != null && u.weapon.kind == AbilityKind.Skill ? new[] { 2, 3 } : new[] { 0, 1 };
        }

        public AbilityController Abilities => _abilities;

        /// <summary>Apply an upgrade. For weapons, slot = -1 uses the first free slot.</summary>
        public bool Apply(UpgradeSO u, int slot = -1)
        {
            if (u == null) return false;
            if (_abilities == null) _abilities = GetComponent<AbilityController>();

            switch (u.effect)
            {
                case UpgradeEffect.StatAdd:
                {
                    float amount = u.amount * EffectMultiplier(u.stat);
                    _stats.AddToBase(u.stat, amount);
                    if (u.stat == StatType.MaxCells && u.addCurrentCells) _stats.Heal(Mathf.RoundToInt(amount));
                    break;
                }

                case UpgradeEffect.StatPercent:
                    if (!Mathf.Approximately(u.percent, 0f))
                        _stats.AddModifier(u.stat, ModifierOp.Multiply, 1f + u.percent, -1f, this);
                    if (!Mathf.Approximately(u.secondaryPercent, 0f))
                        _stats.AddModifier(u.secondaryStat, ModifierOp.Multiply, 1f + u.secondaryPercent, -1f, this);
                    break;

                case UpgradeEffect.GrantWeapon:
                    if (_abilities == null || u.weapon == null) return false;
                    if (slot < 0) slot = FindFreeSlot(u);
                    if (slot < 0) return false;
                    if (!_abilities.Equip(slot, u.weapon)) return false;
                    break;

                case UpgradeEffect.EvolveTank:
                    TankEvolved = true;
                    _stats.AddModifier(StatType.Defense, ModifierOp.Multiply, u.evolutionDefMultiplier, -1f, this);
                    _growthEffect = u.evolutionGrowthEffect;
                    break;
            }

            _stacks[u.id] = Stacks(u) + 1;
            _taken.Add(u);
            return true;
        }

        /// <summary>Tank evolution: Max Cells and Regen upgrades are weaker after evolving.</summary>
        float EffectMultiplier(StatType stat)
        {
            if (TankEvolved && (stat == StatType.MaxCells || stat == StatType.CellRegen)) return _growthEffect;
            return 1f;
        }

        public string Describe()
        {
            if (_stacks.Count == 0) return "nothing yet";
            var sb = new StringBuilder();
            foreach (var kv in _stacks)
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(kv.Key).Append(" x").Append(kv.Value);
            }
            return sb.ToString();
        }
    }
}
