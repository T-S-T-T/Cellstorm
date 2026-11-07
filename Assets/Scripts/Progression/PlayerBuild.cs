using System.Collections.Generic;
using System.Text;
using Cellstorm.Combat;
using Cellstorm.Data;
using Cellstorm.Enemies;
using Cellstorm.Weapons;
using UnityEngine;

namespace Cellstorm.Progression
{
    /// <summary>
    /// What the player has taken this run (upgrade stacks, evolutions) and the code that applies upgrades.
    /// Lives on the Player object. V0.2: cards go to the AbilityController's UpgradeMods; evolutions have a condition
    /// (stacks, a weapon plus cards, a boss reward) and lock each other out.
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
        readonly List<UpgradeSO> _evolutions = new List<UpgradeSO>();
        SwarmStats _stats;
        AbilityController _abilities;
        float _growthEffect = 1f;

        public SwarmStats Stats => _stats;
        public bool TankEvolved { get; private set; }
        /// <summary>Bosses defeated this run (the Artillery evolution needs at least one).</summary>
        public int BossesDefeated { get; private set; }
        public IReadOnlyList<UpgradeSO> Evolutions => _evolutions;

        void Awake()
        {
            Instance = this;
            _stats = GetComponent<SwarmStats>();
            _abilities = GetComponent<AbilityController>();
        }

        void OnEnable()
        {
            Enemy.Killed += OnEnemyKilled;
        }

        void OnDisable()
        {
            Enemy.Killed -= OnEnemyKilled;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void OnEnemyKilled(Enemy e)
        {
            if (e.IsBoss) BossesDefeated++;
        }

        public int Stacks(string id)
        {
            return id != null && _stacks.TryGetValue(id, out var n) ? n : 0;
        }

        public int Stacks(UpgradeSO u)
        {
            return u != null ? Stacks(u.id) : 0;
        }

        /// <summary>How many cards of a card group ("minion", "projectile") the player owns, counting every stack.</summary>
        public int GroupStacks(string group)
        {
            if (string.IsNullOrEmpty(group)) return 0;
            int n = 0;
            for (int i = 0; i < _taken.Count; i++)
                if (_taken[i].cardGroup == group) n++;
            return n;
        }

        /// <summary>Can this upgrade appear in the level-up selection right now?</summary>
        public bool IsEligible(UpgradeSO u)
        {
            if (u == null) return false;
            if (!Cellstorm.Core.UnlockState.IsUnlocked(u.unlockId)) return false;

            int owned = Stacks(u);
            if (!u.repeatable && owned > 0) return false;
            if (u.maxStacks > 0 && owned >= u.maxStacks) return false;       // maxed cards leave the pool

            switch (u.effect)
            {
                case UpgradeEffect.GrantWeapon:
                    if (u.weapon == null) return false;
                    if (OwnsWeaponType(u.weapon.type)) return false;           // owned items never reappear
                    break;

                case UpgradeEffect.Card:
                    if (!OwnsMatching(u)) return false;                        // needs an item the card applies to
                    break;

                case UpgradeEffect.Evolution:
                    if (!EvolutionReady(u)) return false;
                    break;

                case UpgradeEffect.PotentCells:
                case UpgradeEffect.EvolveTank:
                    return false;                                              // V0.1.1 forms, replaced by a Card and an Evolution
            }

            if (u.lockoutIds != null)
            {
                foreach (var id in u.lockoutIds)
                    if (Stacks(id) > 0) return false;                          // locked out by an evolution
            }
            return true;
        }

        bool EvolutionReady(UpgradeSO u)
        {
            switch (u.condition)
            {
                case EvoCondition.Stacks:
                    return Stacks(u.requirementId) >= u.requirementCount;
                case EvoCondition.WeaponAndCards:
                    return OwnsAny(u.requiredAnyOwned) && GroupStacks(u.requirementId) >= u.requirementCount;
                default:
                    return BossesDefeated > 0 && OwnsAny(u.requiredAnyOwned);
            }
        }

        bool OwnsAny(WeaponType[] types)
        {
            if (types == null || types.Length == 0) return true;
            foreach (var t in types)
                if (OwnsWeaponType(t)) return true;
            return false;
        }

        /// <summary>A card needs an owned weapon or skill that it applies to (by name or by tag).</summary>
        bool OwnsMatching(UpgradeSO card)
        {
            if (_abilities == null) return false;
            for (int i = 0; i < AbilityController.SlotCount; i++)
            {
                var a = _abilities.GetSlot(i);
                if (a != null && UpgradeMods.Matches(card, a.Data)) return true;
            }
            return false;
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
                    if (u.stat == StatType.MaxHp && u.addCurrentCells) _stats.Heal(amount);
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

                case UpgradeEffect.Card:
                    if (_abilities == null) return false;
                    _abilities.Mods.Add(u);
                    break;

                case UpgradeEffect.Evolution:
                    ApplyEvolution(u);
                    break;

                default:
                    return false;
            }

            _stacks[u.id] = Stacks(u) + 1;
            _taken.Add(u);
            return true;
        }

        void ApplyEvolution(UpgradeSO u)
        {
            if (_abilities != null) _abilities.Mods.Add(u);           // the evolution's CardEffects (Toxic, Hunter, Artillery, Swarm)
            _evolutions.Add(u);

            switch (u.evo)
            {
                case EvoKind.Tank:
                    TankEvolved = true;
                    _stats.AddModifier(StatType.Defense, ModifierOp.Multiply, u.evolutionDefMultiplier, -1f, this);
                    _growthEffect = u.evolutionGrowthEffect;
                    break;

                case EvoKind.Swarm:
                    if (!Mathf.Approximately(u.evoMaxHpMultiplier, 1f))
                        _stats.AddModifier(StatType.MaxHp, ModifierOp.Multiply, u.evoMaxHpMultiplier, -1f, this);
                    break;
            }
        }

        /// <summary>Tank evolution: Max HP and Regen upgrades are weaker after evolving.</summary>
        float EffectMultiplier(StatType stat)
        {
            if (TankEvolved && (stat == StatType.MaxHp || stat == StatType.HpRegen)) return _growthEffect;
            return 1f;
        }

        /// <summary>Names of the evolutions taken, for the run summary and the leaderboard.</summary>
        public string DescribeEvolutions()
        {
            if (_evolutions.Count == 0) return "";
            var sb = new StringBuilder();
            foreach (var e in _evolutions)
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(e.displayName.Replace("Evolution: ", ""));
            }
            return sb.ToString();
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
