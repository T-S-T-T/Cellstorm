using Cellstorm.Combat;
using UnityEngine;

namespace Cellstorm.Data
{
    /// <summary>
    /// Every upgrade of GDD V0.2 section 15, created in code: swarm upgrades, weapon and skill items, shared cards,
    /// specific weapon cards, skill cards and the five evolutions. The editor menu saves them as assets; at runtime
    /// they are the fallback. All values are placeholders from the GDD.
    /// </summary>
    public static class CardCatalog
    {
        static CardEffect E(Param p, EffectOp op, float perStack, float firstBonus = 0f)
        {
            return new CardEffect { param = p, op = op, perStack = perStack, firstBonus = firstBonus };
        }

        static CardEffect Only(CardEffect e, WeaponTag tags)
        {
            e.onlyTags = tags;
            return e;
        }

        static UpgradeSO Make(string id, string displayName, string description, UpgradeCategory category, string glyph)
        {
            var u = ScriptableObject.CreateInstance<UpgradeSO>();
            u.name = id;
            u.id = id;
            u.displayName = displayName;
            u.description = description;
            u.category = category;
            u.iconGlyph = glyph;
            u.iconColor = UpgradeDefaults.CategoryColor(category);
            return u;
        }

        /// <summary>A card: effect list, targets, cap. max = 1 means once.</summary>
        static UpgradeSO Card(string id, string name, string desc, string glyph, UpgradeCategory cat, int max,
            StackForm form, string stackText, WeaponTag tags, WeaponType[] types, params CardEffect[] effects)
        {
            var u = Make(id, name, desc, cat, glyph);
            u.effect = UpgradeEffect.Card;
            u.effects = effects;
            u.maxStacks = max;
            u.repeatable = max != 1;
            u.form = max == 1 ? StackForm.Once : form;
            u.stackText = stackText;
            u.targetTags = tags;
            u.targetTypes = types ?? new WeaponType[0];
            return u;
        }

        static UpgradeSO Shared(string id, string name, string desc, string glyph, WeaponTag tags, int max,
            StackForm form, string stackText, params CardEffect[] effects)
        {
            var cat = (tags & WeaponTag.Skill) != 0 ? UpgradeCategory.Skill : UpgradeCategory.Attack;
            return Card(id, name, desc, glyph, cat, max, form, stackText, tags, null, effects);
        }

        static UpgradeSO Specific(string id, string name, string desc, string glyph, WeaponType type, int max,
            StackForm form, string stackText, params CardEffect[] effects)
        {
            var cat = WeaponTags.Of(type) == WeaponTag.Skill ? UpgradeCategory.Skill : UpgradeCategory.Attack;
            return Card(id, name, desc, glyph, cat, max, form, stackText, WeaponTag.None, new[] { type }, effects);
        }

        static UpgradeSO Item(string id, string name, string desc, string glyph, WeaponSO weapon, string unlockId)
        {
            var u = Make(id, name, desc, weapon.kind == AbilityKind.Skill ? UpgradeCategory.Skill : UpgradeCategory.Attack, glyph);
            u.effect = UpgradeEffect.GrantWeapon;
            u.weapon = weapon;
            u.repeatable = false;
            u.unlockId = unlockId;
            return u;
        }

        const EffectOp Flat = EffectOp.AddFlat;
        const EffectOp Pct = EffectOp.AddPercentOfBase;
        const EffectOp Mul = EffectOp.Multiply;

        /// <param name="weapon">Looks up the WeaponSO for a key (the editor menu passes the saved assets).</param>
        public static UpgradeSO[] CreateAll(System.Func<string, WeaponSO> weapon)
        {
            var list = new System.Collections.Generic.List<UpgradeSO>();

            // ---------------- 15.1 Swarm upgrades ----------------
            var expand = Make("max_cells", "Expand", "Max HP +20. You heal 20 HP right away.", UpgradeCategory.Swarm, "HP");
            expand.effect = UpgradeEffect.StatAdd; expand.stat = StatType.MaxHp; expand.amount = 20f; expand.addCurrentCells = true;
            expand.stackText = "Additive, no cap";

            var regen = Make("regen", "Regeneration", "HP regen +1 per second.", UpgradeCategory.Swarm, "RG");
            regen.effect = UpgradeEffect.StatAdd; regen.stat = StatType.HpRegen; regen.amount = 1f;
            regen.stackText = "Additive, no cap";

            var def = Make("defense", "Hardened Cells", "DEF +1. You take less damage.", UpgradeCategory.Swarm, "DEF");
            def.effect = UpgradeEffect.StatAdd; def.stat = StatType.Defense; def.amount = 1f;
            def.stackText = "Additive, no cap";

            var speed = Make("speed", "Swift Cells", "Speed +1. You move faster, and weapons deploy and recharge faster.", UpgradeCategory.Swarm, "SPD");
            speed.effect = UpgradeEffect.StatAdd; speed.stat = StatType.Speed; speed.amount = 1f;
            speed.stackText = "Additive, no cap";

            var dense = Make("dense_cells", "Dense Cells", "+20% DEF, -10% max HP. Collect 5 to unlock an evolution.", UpgradeCategory.Swarm, "DNS");
            dense.effect = UpgradeEffect.StatPercent; dense.stat = StatType.Defense; dense.percent = 0.2f;
            dense.secondaryStat = StatType.MaxHp; dense.secondaryPercent = -0.1f;
            dense.form = StackForm.Multiplicative; dense.stackText = "Multiplicative, no cap";

            list.AddRange(new[] { expand, regen, def, speed, dense });

            // ---------------- Weapon and skill items ----------------
            list.Add(Item("ram", "Ram", "Attack. Dash at the cursor for 0.3 s. Everything you touch takes 30 damage once.", "RAM", weapon(WeaponDefaults.RamKey), null));
            list.Add(Item("jaw", "Jaw", "Attack. Hold to form a jaw, release to bite for 150 damage.", "JAW", weapon(WeaponDefaults.JawKey), "jaw"));
            list.Add(Item("gun", "Gun", "Attack. Hold to form a barrel, release to fire a bullet for 100 damage.", "GUN", weapon(WeaponDefaults.GunKey), "gun"));
            list.Add(Item("whip", "Whip", "Attack. Hold to grow a tentacle, release to lash a wide arc for 80 damage and pull enemies in.", "WHP", weapon(WeaponDefaults.WhipKey), "whip"));
            list.Add(Item("spread_shot", "Spread Shot", "Attack. Hold to form a muzzle, release to fire 5 pellets for 40 damage each. Short range.", "SPR", weapon(WeaponDefaults.SpreadShotKey), "spread_shot"));
            list.Add(Item("spore_cloud", "Spore Cloud", "Attack. Press to release a cloud that poisons every enemy inside (Toxic 5 HP/s). Uses no HP.", "SPO", weapon(WeaponDefaults.SporeCloudKey), "spore_cloud"));
            list.Add(Item("mine", "Mine", "Attack. Press to drop a mine that explodes when an enemy comes close: 60 damage in 2 units. Uses no HP.", "MIN", weapon(WeaponDefaults.MineKey), "mine"));
            list.Add(Item("hardening", "Hardening", "Skill. Press to double your DEF for 3 seconds. 10 second cooldown.", "HRD", weapon(WeaponDefaults.HardeningKey), "hardening"));
            list.Add(Item("minion", "Minion", "Skill. Press to split a small swarm off your body for 2.5% of your max HP. It fights for you.", "MNN", weapon(WeaponDefaults.MinionKey), "minion"));
            list.Add(Item("convert", "Convert", "Skill. For 5 s, enemies you damage drop HP orbs that heal you.", "CNV", weapon(WeaponDefaults.ConvertKey), "convert"));
            list.Add(Item("shockwave", "Shockwave", "Skill. A ring pushes enemies away and stuns them for 1 s. No damage.", "SHK", weapon(WeaponDefaults.ShockwaveKey), "shockwave"));

            // ---------------- 15.2 Shared cards ----------------
            const WeaponTag Dmg = WeaponTag.Damage;

            list.Add(Shared("heavy_hits", "Heavy Hits", "+15% damage of base on every weapon that deals direct damage.", "DMG", Dmg, 8,
                StackForm.Additive, "+15% damage of base per stack", E(Param.Damage, Pct, 0.15f)));
            list.Add(Shared("potent_cells", "Potent Cells", "Cell weapons deal +50% damage, but hold 30% of your max HP while deployed.", "PTN", WeaponTag.Cell, 1,
                StackForm.Once, "One time", E(Param.Damage, Pct, 0.5f), E(Param.CostFraction, Flat, 0.2f)));
            list.Add(Shared("tough_cells", "Tough Cells", "Weapon cells get +2 DEF, so they survive interruptions longer.", "TGH", WeaponTag.Cell, 5,
                StackForm.Additive, "+2 cell DEF per stack", E(Param.CellDef, Flat, 2f)));
            list.Add(Shared("wide_reach", "Wide Reach", "Cell weapons reach 15% farther; Spore Cloud and Mine blasts are 15% bigger.", "RCH", WeaponTag.Cell | WeaponTag.Area, 5,
                StackForm.Additive, "+15% size per stack", E(Param.Size, Flat, 0.15f)));
            list.Add(Shared("toxic_tips", "Toxic Tips", "Your hits poison: Toxic 3 HP/s for 4 s. More stacks add +2 HP/s. 4 stacks allow an evolution.", "TOX", Dmg, 6,
                StackForm.Additive, "Toxic 3 HP/s, +2 per extra stack", E(Param.ToxicDps, Flat, 2f, 1f), E(Param.ToxicDuration, Flat, 0f, 4f)));
            list.Add(Shared("chilling_touch", "Chilling Touch", "Your hits slow enemies by 20% for 2 s. More stacks add +10%.", "CHL", Dmg, 5,
                StackForm.Additive, "Slow 20%, +10% per extra stack", E(Param.SlowPct, Flat, 0.1f, 0.1f), E(Param.SlowDuration, Flat, 0f, 2f)));
            list.Add(Shared("stunning_blow", "Stunning Blow", "Your hits have a 10% chance to stun for 0.5 s. More stacks add +5%.", "STN", Dmg, 5,
                StackForm.Additive, "10% chance, +5% per extra stack", E(Param.StunChance, Flat, 0.05f, 0.05f), E(Param.StunDuration, Flat, 0f, 0.5f)));
            list.Add(Shared("heavy_impact", "Heavy Impact", "Your hits knock the target back 0.5 units. More stacks add +0.5.", "IMP", Dmg, 4,
                StackForm.Additive, "+0.5 units per stack", E(Param.KnockDistance, Flat, 0.5f)));
            list.Add(Shared("quick_hands", "Quick Hands", "The cooldown of every skill is 10% shorter.", "QCK", WeaponTag.Skill, 5,
                StackForm.Diminishing, "Skill cooldown x0.9 per stack", E(Param.Cooldown, Mul, 0.9f)));
            var piercing = Shared("piercing_rounds", "Piercing Rounds", "Bullets and pellets pierce +1 target. Never bosses.", "PRC", WeaponTag.Projectile, 3,
                StackForm.Additive, "+1 pierce per stack", E(Param.Pierce, Flat, 1f));
            piercing.cardGroup = "projectile";
            var fast = Shared("fast_rounds", "Fast Rounds", "Projectile speed +25% of base.", "FST", WeaponTag.Projectile, 4,
                StackForm.Additive, "+25% projectile speed per stack", E(Param.ProjectileSpeed, Pct, 0.25f));
            fast.cardGroup = "projectile";
            list.Add(piercing);
            list.Add(fast);

            // ---------------- 15.3 Weapon cards ----------------
            list.Add(Specific("long_dash", "Long Dash", "Ram dash speed +20% of base, so it goes farther.", "LDS", WeaponType.Ram, 5,
                StackForm.Additive, "+20% dash speed per stack", E(Param.DashSpeed, Pct, 0.2f)));
            list.Add(Specific("quick_ram", "Quick Ram", "Ram cooldown x0.85.", "QRM", WeaponType.Ram, 4,
                StackForm.Diminishing, "Cooldown x0.85 per stack", E(Param.Cooldown, Mul, 0.85f)));
            list.Add(Specific("crashing", "Crashing", "The Ram contact window lasts 0.1 s longer.", "CRS", WeaponType.Ram, 3,
                StackForm.Additive, "+0.1 s per stack", E(Param.DashWindow, Flat, 0.1f)));
            list.Add(Specific("wide_jaw", "Wide Jaw", "Bite cone +15 degrees (from 90).", "WJW", WeaponType.Jaw, 4,
                StackForm.Additive, "+15 degrees per stack", E(Param.ConeAngle, Flat, 15f)));
            list.Add(Specific("long_fangs", "Long Fangs", "Bite reach +0.5 units.", "FNG", WeaponType.Jaw, 4,
                StackForm.Additive, "+0.5 units per stack", E(Param.Reach, Flat, 0.5f)));
            list.Add(Specific("second_bite", "Second Bite", "After a bite, bites again 0.3 s later for 50% damage.", "2BT", WeaponType.Jaw, 1,
                StackForm.Once, "One time", E(Param.SecondBite, Flat, 1f)));
            var rifled = Specific("rifled_barrel", "Rifled Barrel", "Gun bullet damage +20% of base.", "RFL", WeaponType.Gun, 5,
                StackForm.Additive, "+20% damage per stack", E(Param.Damage, Pct, 0.2f));
            rifled.cardGroup = "projectile";
            var explosive = Specific("explosive_rounds", "Explosive Rounds", "Gun bullets explode on impact within 1 unit for 50% damage.", "EXP", WeaponType.Gun, 1,
                StackForm.Once, "One time", E(Param.ExplosiveRounds, Flat, 1f));
            explosive.cardGroup = "projectile";
            list.Add(rifled);
            list.Add(explosive);
            list.Add(Specific("long_lash", "Long Lash", "Whip reach +0.5 units.", "LLS", WeaponType.Whip, 4,
                StackForm.Additive, "+0.5 units per stack", E(Param.Reach, Flat, 0.5f)));
            list.Add(Specific("wide_arc", "Wide Arc", "Whip lash arc +20 degrees (from 120).", "WAR", WeaponType.Whip, 4,
                StackForm.Additive, "+20 degrees per stack", E(Param.ArcAngle, Flat, 20f)));
            list.Add(Specific("barbed_lash", "Barbed Lash", "Whip pull distance +0.75 units.", "BRB", WeaponType.Whip, 3,
                StackForm.Additive, "+0.75 units per stack", E(Param.PullDistance, Flat, 0.75f)));
            var pellets = Specific("extra_pellets", "Extra Pellets", "+2 pellets in the Spread Shot fan.", "PEL", WeaponType.SpreadShot, 4,
                StackForm.Additive, "+2 pellets per stack", E(Param.Pellets, Flat, 2f));
            pellets.cardGroup = "projectile";
            var choke = Specific("choke", "Choke", "Spread Shot fan 8 degrees narrower and pellet damage +10% of base.", "CHK", WeaponType.SpreadShot, 3,
                StackForm.Additive, "-8 degrees and +10% damage per stack", E(Param.FanAngle, Flat, -8f), E(Param.Damage, Pct, 0.1f));
            choke.cardGroup = "projectile";
            list.Add(pellets);
            list.Add(choke);
            list.Add(Specific("lingering_spores", "Lingering Spores", "The cloud lasts 1 s longer.", "LNG", WeaponType.SporeCloud, 5,
                StackForm.Additive, "+1 s per stack", E(Param.CloudDuration, Flat, 1f)));
            list.Add(Specific("virulent_spores", "Virulent Spores", "Cloud Toxic +2 HP/s.", "VIR", WeaponType.SporeCloud, 5,
                StackForm.Additive, "+2 HP/s per stack", E(Param.ToxicDps, Flat, 2f)));
            list.Add(Specific("choking_cloud", "Choking Cloud", "Enemies inside the cloud are also slowed by 30%.", "CHC", WeaponType.SporeCloud, 1,
                StackForm.Once, "One time", E(Param.SlowPct, Flat, 0.3f)));
            list.Add(Specific("extra_mines", "Extra Mines", "+1 mine per drop, placed around you.", "XMN", WeaponType.Mine, 4,
                StackForm.Additive, "+1 mine per stack", E(Param.MineCount, Flat, 1f)));
            list.Add(Specific("quick_fuse", "Quick Fuse", "Arming time -0.25 s (never below 0.25 s).", "FUS", WeaponType.Mine, 3,
                StackForm.Additive, "-0.25 s per stack", E(Param.ArmTime, Flat, -0.25f)));
            list.Add(Specific("proximity", "Proximity", "Mine trigger range +0.5 units.", "PRX", WeaponType.Mine, 3,
                StackForm.Additive, "+0.5 units per stack", E(Param.TriggerRange, Flat, 0.5f)));

            // ---------------- 15.4 Skill cards ----------------
            list.Add(Specific("longer_hardening", "Longer Hardening", "Hardening lasts 1 s longer.", "LHD", WeaponType.Hardening, 4,
                StackForm.Additive, "+1 s per stack", E(Param.EffectDuration, Flat, 1f)));
            list.Add(Specific("stronger_hardening", "Stronger Hardening", "Hardening DEF multiplier +0.5 (from x2).", "SHD", WeaponType.Hardening, 3,
                StackForm.Additive, "+0.5 multiplier per stack", E(Param.DefMultiplier, Flat, 0.5f)));
            var moreMinions = Specific("more_minions", "More Minions", "+1 minion per activation. Each costs HP, so the cost rises with it.", "MM+", WeaponType.Minion, 4,
                StackForm.Additive, "+1 minion per stack", E(Param.MinionCount, Flat, 1f));
            var cheaper = Specific("cheaper_minions", "Cheaper Minions", "Cost per minion -0.5 points of max HP (never below 1%). Minions have less HP.", "MM-", WeaponType.Minion, 3,
                StackForm.Additive, "-0.5% max HP cost per stack", E(Param.MinionCost, Flat, -0.005f));
            var sharp = Specific("sharp_minions", "Sharp Minions", "Minion contact damage +50% of base.", "MSH", WeaponType.Minion, 5,
                StackForm.Additive, "+50% contact damage per stack", E(Param.MinionContact, Pct, 0.5f));
            var sturdy = Specific("sturdy_minions", "Sturdy Minions", "Minion HP +25% of its cost, with no extra cost.", "MST", WeaponType.Minion, 4,
                StackForm.Additive, "+25% minion HP per stack", E(Param.MinionHp, Pct, 0.25f));
            moreMinions.cardGroup = cheaper.cardGroup = sharp.cardGroup = sturdy.cardGroup = "minion";
            list.AddRange(new[] { moreMinions, cheaper, sharp, sturdy });
            list.Add(Specific("rich_orbs", "Rich Orbs", "HP orbs heal +5 points of the damage dealt (from 10%).", "ORB", WeaponType.Convert, 4,
                StackForm.Additive, "+5% per stack", E(Param.OrbPct, Flat, 0.05f)));
            list.Add(Specific("longer_convert", "Longer Convert", "Convert lasts 1 s longer.", "LCV", WeaponType.Convert, 4,
                StackForm.Additive, "+1 s per stack", E(Param.EffectDuration, Flat, 1f)));
            list.Add(Specific("wider_wave", "Wider Wave", "Shockwave radius +20% of base.", "WWV", WeaponType.Shockwave, 5,
                StackForm.Additive, "+20% radius per stack", E(Param.ShockRadius, Pct, 0.2f)));
            list.Add(Specific("hard_shove", "Hard Shove", "Shockwave push distance +1 unit.", "HSH", WeaponType.Shockwave, 4,
                StackForm.Additive, "+1 unit per stack", E(Param.ShockPush, Flat, 1f)));
            list.Add(Specific("long_stun", "Long Stun", "Shockwave stun lasts 0.25 s longer (half on bosses).", "LST", WeaponType.Shockwave, 4,
                StackForm.Additive, "+0.25 s per stack", E(Param.ShockStun, Flat, 0.25f)));

            // ---------------- 6.5 Evolutions ----------------
            var tank = Make("evo_tank", "Evolution: Tank", "DEF x5. Max HP and regen upgrades are half as effective from now on. Locks out Swarm.", UpgradeCategory.Evolution, "TNK");
            tank.effect = UpgradeEffect.Evolution; tank.evo = EvoKind.Tank; tank.repeatable = false;
            tank.condition = EvoCondition.Stacks; tank.requirementId = "dense_cells"; tank.requirementCount = 5;
            tank.lockoutIds = new[] { "evo_swarm" }; tank.stackText = "Needs 5 Dense Cells";
            tank.effects = new CardEffect[0];

            var swarm = Make("evo_swarm", "Evolution: Swarm", "Each Minion activation spawns 2 more minions. Minion contact damage x2. Max HP x0.8. Locks out Tank.", UpgradeCategory.Evolution, "SWM");
            swarm.effect = UpgradeEffect.Evolution; swarm.evo = EvoKind.Swarm; swarm.repeatable = false;
            swarm.condition = EvoCondition.WeaponAndCards; swarm.requirementId = "minion"; swarm.requirementCount = 3;
            swarm.requiredAnyOwned = new[] { WeaponType.Minion };
            swarm.lockoutIds = new[] { "evo_tank" }; swarm.evoMaxHpMultiplier = 0.8f; swarm.stackText = "Needs Minion and 3 Minion cards";
            swarm.targetTypes = new[] { WeaponType.Minion };
            swarm.effects = new[] { E(Param.MinionCount, Flat, 2f), E(Param.MinionContact, Mul, 2f) };

            var toxic = Make("evo_toxic", "Evolution: Toxic", "All Toxic you apply is twice as strong and lasts 50% longer. A poisoned enemy that dies spreads its Toxic within 2 units.", UpgradeCategory.Evolution, "TXC");
            toxic.effect = UpgradeEffect.Evolution; toxic.evo = EvoKind.Toxic; toxic.repeatable = false;
            toxic.condition = EvoCondition.Stacks; toxic.requirementId = "toxic_tips"; toxic.requirementCount = 4;
            toxic.appliesToAll = true; toxic.stackText = "Needs 4 Toxic Tips";
            toxic.effects = new[] { E(Param.ToxicDps, Mul, 2f), E(Param.ToxicDuration, Mul, 1.5f), E(Param.ToxicSpread, Flat, 1f) };

            var hunter = Make("evo_hunter", "Evolution: Hunter", "Projectiles curve toward the nearest enemy, deal +50% damage and pierce 1 more. Cell weapon deploy time x1.3. Locks out Artillery.", UpgradeCategory.Evolution, "HNT");
            hunter.effect = UpgradeEffect.Evolution; hunter.evo = EvoKind.Hunter; hunter.repeatable = false;
            hunter.condition = EvoCondition.WeaponAndCards; hunter.requirementId = "projectile"; hunter.requirementCount = 4;
            hunter.requiredAnyOwned = new[] { WeaponType.Gun, WeaponType.SpreadShot };
            hunter.lockoutIds = new[] { "evo_artillery" }; hunter.stackText = "Needs Gun or Spread Shot and 4 Projectile cards";
            hunter.targetTags = WeaponTag.Projectile | WeaponTag.Cell;
            hunter.effects = new[]
            {
                Only(E(Param.Homing, Flat, 1f), WeaponTag.Projectile),
                Only(E(Param.Damage, Pct, 0.5f), WeaponTag.Projectile),
                Only(E(Param.Pierce, Flat, 1f), WeaponTag.Projectile),
                E(Param.ShapeTime, Mul, 1.3f)
            };

            var artillery = Make("evo_artillery", "Evolution: Artillery", "Explosions you cause are 50% larger and deal 50% more damage. Gun bullets explode. Weapon cooldowns x1.2. Locks out Hunter.", UpgradeCategory.Evolution, "ART");
            artillery.effect = UpgradeEffect.Evolution; artillery.evo = EvoKind.Artillery; artillery.repeatable = false;
            artillery.condition = EvoCondition.BossReward;
            artillery.requiredAnyOwned = new[] { WeaponType.Gun, WeaponType.SpreadShot, WeaponType.Mine };
            artillery.lockoutIds = new[] { "evo_hunter" }; artillery.stackText = "Needs a defeated boss and Gun, Spread Shot or Mine";
            artillery.appliesToAll = true;
            var gunBoom = E(Param.ExplosiveRounds, Flat, 1f);
            gunBoom.filterType = true; gunBoom.onlyType = WeaponType.Gun;
            artillery.effects = new[]
            {
                E(Param.ExplosionSize, Mul, 1.5f), E(Param.ExplosionDamage, Mul, 1.5f),
                Only(E(Param.Cooldown, Mul, 1.2f), WeaponTag.Weapon), gunBoom
            };

            list.AddRange(new[] { tank, swarm, toxic, hunter, artillery });
            return list.ToArray();
        }
    }
}
