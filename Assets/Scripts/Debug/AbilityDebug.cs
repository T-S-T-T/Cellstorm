using Cellstorm.Data;
using Cellstorm.Enemies;
using Cellstorm.Swarm;
using Cellstorm.Weapons;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Cellstorm.Debugging
{
    /// <summary>
    /// TEMPORARY test harness for Phase 4 (the real equip flow arrives with level-ups and swap mode).
    /// 6 / 7 / 8 = LMB Ram / Jaw / Gun   (hold Shift for RMB)
    /// 9 = Hardening on Q   (Shift = E)       0 = clear all slots
    /// I = spawn ranged cell (gun)   O = spawn jaw cell   P = spawn ram cell
    /// </summary>
    public class AbilityDebug : MonoBehaviour
    {
        [Header("Optional overrides (empty = built-in GDD defaults)")]
        [SerializeField] WeaponSO ram;
        [SerializeField] WeaponSO jaw;
        [SerializeField] WeaponSO gun;
        [SerializeField] WeaponSO hardening;

        SwarmBody _player;
        AbilityController _abilities;

        void Start()
        {
            _player = FindAnyObjectByType<SwarmBody>();
            if (_player != null) _abilities = _player.GetComponent<AbilityController>();
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || _player == null) return;

            bool shift = kb.shiftKey.isPressed;

            if (_abilities != null)
            {
                int weaponSlot = shift ? 1 : 0;
                if (kb.digit6Key.wasPressedThisFrame) _abilities.Equip(weaponSlot, ram != null ? ram : WeaponDefaults.Ram);
                if (kb.digit7Key.wasPressedThisFrame) _abilities.Equip(weaponSlot, jaw != null ? jaw : WeaponDefaults.Jaw);
                if (kb.digit8Key.wasPressedThisFrame) _abilities.Equip(weaponSlot, gun != null ? gun : WeaponDefaults.Gun);
                if (kb.digit9Key.wasPressedThisFrame)
                    _abilities.Equip(shift ? 3 : 2, hardening != null ? hardening : WeaponDefaults.Hardening);
                if (kb.digit0Key.wasPressedThisFrame)
                    for (int i = 0; i < AbilityController.SlotCount; i++) _abilities.Unequip(i);
            }

            if (kb.iKey.wasPressedThisFrame) SpawnRanged();
            if (kb.oKey.wasPressedThisFrame) SpawnWithWeapon("Jaw Cell", 20, WeaponDefaults.Jaw, EnemyBehavior.Chase, new Color(0.95f, 0.55f, 0.25f));
            if (kb.pKey.wasPressedThisFrame) SpawnWithWeapon("Ram Cell", 10, WeaponDefaults.Ram, EnemyBehavior.Chase, new Color(0.95f, 0.85f, 0.25f));
        }

        void SpawnRanged()
        {
            SpawnWithWeapon("Ranged Cell", 50, WeaponDefaults.EnemyGun, EnemyBehavior.KeepDistance, new Color(0.35f, 0.60f, 0.95f));
        }

        void SpawnWithWeapon(string label, int cells, WeaponSO weapon, EnemyBehavior behavior, Color color)
        {
            Vector2 dir = Random.insideUnitCircle.normalized;
            if (dir.sqrMagnitude < 0.01f) dir = Vector2.right;
            Vector2 pos = (Vector2)_player.transform.position + dir * (_player.SmoothedRadius + 7f);

            var spec = new EnemySpec
            {
                name = label,
                color = color,
                maxCells = cells,
                regen = 1f,
                defense = 1f,
                speed = 1f,
                behavior = behavior,
                preferredRange = 6f,
                weapon = weapon,
                scoreValue = 1,
                scorePerCell = true,
                milestone = 0
            };
            Enemy.Create(spec, pos);
        }

        void OnGUI()
        {
            if (!DebugVisibility.Show) return;
            if (_player == null) return;

            if (_abilities == null)
            {
                GUI.Label(new Rect(10, 235, 1100, 25), "Phase 4: no AbilityController on the player - run Cellstorm > Phase 4 > Setup Game Scene.");
                return;
            }

            string line = "";
            for (int i = 0; i < AbilityController.SlotCount; i++)
            {
                var a = _abilities.GetSlot(i);
                string text = a == null ? "-" : a.Data.displayName + " [" + a.Status + "]";
                line += AbilityController.SlotNames[i] + ": " + text + "     ";
            }

            GUI.Label(new Rect(10, 235, 1400, 25), line);
            GUI.Label(new Rect(10, 255, 1400, 25),
                "6/7/8 = LMB Ram/Jaw/Gun (Shift = RMB)   9 = Hardening on Q (Shift = E)   0 = clear   " +
                "I ranged enemy   O jaw enemy   P ram enemy   hold LMB/RMB to shape, release to fire");
            GUI.Label(new Rect(10, 275, 1400, 25),
                "Tip: enemies have DEF 1 so their weapons tie with your DEF 2. Press , to lower your DEF to 1 and see them hurt you.");
        }
    }
}
