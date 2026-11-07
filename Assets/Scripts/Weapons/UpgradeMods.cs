using System.Collections.Generic;
using Cellstorm.Combat;
using Cellstorm.Data;
using UnityEngine;

namespace Cellstorm.Weapons
{
    /// <summary>
    /// The owned upgrade cards and evolutions of one swarm, and the single place that turns "base value + cards"
    /// into a final value (GDD V0.2 sections 3.2 and 6.3):
    ///   final = (base + sum of flat adds + base x sum of percent adds) x product of multipliers.
    /// Enemies have an empty UpgradeMods, so they always get the base numbers.
    /// </summary>
    public class UpgradeMods
    {
        readonly List<UpgradeSO> _cards = new List<UpgradeSO>();
        readonly List<int> _stacks = new List<int>();

        public int CardCount => _cards.Count;

        public void Clear()
        {
            _cards.Clear();
            _stacks.Clear();
        }

        /// <summary>One more stack of this card (cards and evolutions both go through here).</summary>
        public void Add(UpgradeSO u)
        {
            if (u == null || u.effects == null || u.effects.Length == 0) return;
            int i = _cards.IndexOf(u);
            if (i < 0)
            {
                _cards.Add(u);
                _stacks.Add(1);
            }
            else
            {
                _stacks[i]++;
            }
        }

        public int Stacks(UpgradeSO u)
        {
            int i = _cards.IndexOf(u);
            return i < 0 ? 0 : _stacks[i];
        }

        /// <summary>Does this card apply to this weapon or skill?</summary>
        public static bool Matches(UpgradeSO card, WeaponSO w)
        {
            if (card == null || w == null) return false;
            if (card.appliesToAll) return true;
            if (card.targetTypes != null)
                for (int i = 0; i < card.targetTypes.Length; i++)
                    if (card.targetTypes[i] == w.type) return true;
            return (card.targetTags & WeaponTags.Of(w.type)) != WeaponTag.None;
        }

        /// <summary>The final value of one parameter for a weapon or skill.</summary>
        public float Get(Param p, WeaponSO w, float baseValue)
        {
            float add = 0f, pct = 0f, mul = 1f;
            for (int c = 0; c < _cards.Count; c++)
            {
                var card = _cards[c];
                if (!Matches(card, w)) continue;
                int n = _stacks[c];

                var effects = card.effects;
                for (int e = 0; e < effects.Length; e++)
                {
                    var fx = effects[e];
                    if (fx.param != p || !fx.AppliesTo(w.type)) continue;
                    switch (fx.op)
                    {
                        case EffectOp.AddFlat: add += fx.perStack * n + fx.firstBonus; break;
                        case EffectOp.AddPercentOfBase: pct += fx.perStack * n + fx.firstBonus; break;
                        default: mul *= Mathf.Pow(fx.perStack, n); break;
                    }
                }
            }
            return (baseValue + add + baseValue * pct) * mul;
        }

        /// <summary>True when any owned card has an effect on this parameter (global flags such as Toxic spread).</summary>
        public bool Has(Param p)
        {
            for (int c = 0; c < _cards.Count; c++)
            {
                var effects = _cards[c].effects;
                for (int e = 0; e < effects.Length; e++)
                    if (effects[e].param == p && _stacks[c] > 0) return true;
            }
            return false;
        }

        public float Get(Param p, Ability a, float baseValue)
        {
            return Get(p, a != null ? a.Data : null, baseValue);
        }

        /// <summary>True when a flag-like parameter (Homing, SecondBite, ExplosiveRounds, ToxicSpread) is on for this item.</summary>
        public bool Flag(Param p, WeaponSO w)
        {
            return Get(p, w, 0f) > 0.0001f;
        }

        /// <summary>
        /// The status payload a hit of this weapon carries: the weapon's own base effect plus every matching card
        /// (Toxic Tips, Chilling Touch, Stunning Blow, Heavy Impact, the Toxic evolution).
        /// </summary>
        public StatusPayload Status(WeaponSO w, StatusPayload baseP, Vector2 sourcePos)
        {
            var p = baseP;
            p.sourcePos = sourcePos;

            p.toxicDps = Get(Param.ToxicDps, w, baseP.toxicDps);
            p.toxicDuration = p.toxicDps > 0f ? Get(Param.ToxicDuration, w, baseP.toxicDuration) : 0f;

            p.slowPct = Mathf.Min(0.8f, Get(Param.SlowPct, w, baseP.slowPct));
            p.slowDuration = p.slowPct > 0f ? Get(Param.SlowDuration, w, baseP.slowDuration) : 0f;

            p.stunChance = Mathf.Clamp01(Get(Param.StunChance, w, baseP.stunChance));
            p.stunDuration = p.stunChance > 0f ? Get(Param.StunDuration, w, baseP.stunDuration) : 0f;

            p.knockDistance = Get(Param.KnockDistance, w, baseP.knockDistance);
            return p;
        }
    }
}
