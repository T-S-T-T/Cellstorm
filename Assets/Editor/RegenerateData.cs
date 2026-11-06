using System;
using System.Collections.Generic;
using System.IO;
using Cellstorm.Data;
using Cellstorm.Progression;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Cellstorm.EditorTools
{
    /// <summary>
    /// V0.1.1: rebuilds every data asset (weapons, enemy types, loadouts, upgrades) from the code defaults, IN PLACE:
    /// the files and their GUIDs stay the same, so the spawn table, the upgrade pool and the scene keep working.
    /// A backup of Assets/Data is copied to Backups/ (outside Assets) first.
    /// Needed once after the HP model replaced the cell model, because the old assets hold values such as "5 cells".
    /// </summary>
    public static class RegenerateData
    {
        public const string DataVersion = "0.1.1-phase1";
        const string Root = "Assets/Data";
        const string VersionFile = "Assets/Data/data_version.txt";

        [MenuItem("Cellstorm/Data/Regenerate data (V0.1.1)")]
        public static void Run()
        {
            if (!EditorUtility.DisplayDialog("Regenerate Cellstorm data",
                    "This rebuilds all weapons, enemy types, loadouts and upgrades in Assets/Data from the V0.1.1 defaults.\n\n" +
                    "A backup of Assets/Data is saved to the Backups folder next to Assets first. Continue?",
                    "Regenerate", "Cancel"))
                return;
            Execute();
        }

        public static void Execute()
        {
            string backup = Backup();

            EnsureFolder(Root);
            EnsureFolder(Root + "/Weapons");
            EnsureFolder(Root + "/Enemies");
            EnsureFolder(Root + "/Loadouts");
            EnsureFolder(Root + "/Upgrades");

            var pairs = new List<KeyValuePair<UnityEngine.Object, UnityEngine.Object>>(); // fresh -> final asset
            var map = new Dictionary<UnityEngine.Object, UnityEngine.Object>();

            // ---- Weapons
            var ram = Upsert(Root + "/Weapons/Ram.asset", WeaponDefaults.Create(WeaponDefaults.RamKey), pairs, map, KeepWeapon);
            var jaw = Upsert(Root + "/Weapons/Jaw.asset", WeaponDefaults.Create(WeaponDefaults.JawKey), pairs, map, KeepWeapon);
            var gun = Upsert(Root + "/Weapons/Gun.asset", WeaponDefaults.Create(WeaponDefaults.GunKey), pairs, map, KeepWeapon);
            var hardening = Upsert(Root + "/Weapons/Hardening.asset", WeaponDefaults.Create(WeaponDefaults.HardeningKey), pairs, map, KeepWeapon);
            var enemyGun = Upsert(Root + "/Weapons/EnemyGun.asset", WeaponDefaults.Create(WeaponDefaults.EnemyGunKey), pairs, map, KeepWeapon);

            // ---- Enemy types
            string[] enemyFiles = { "SingleCell", "MultiCell", "RangedCell", "ExplodingCell" };
            var freshTypes = EnemyDefaults.CreateTypes();
            var types = new EnemyTypeSO[freshTypes.Length];
            for (int i = 0; i < freshTypes.Length; i++)
            {
                types[i] = Upsert(Root + "/Enemies/" + enemyFiles[i] + ".asset", freshTypes[i], pairs, map, KeepEnemy);
                if (i == 2 && types[i].weapon == null) types[i].weapon = enemyGun; // ranged cell carries the enemy gun
            }

            string tablePath = Root + "/Enemies/SpawnTable.asset";
            if (AssetDatabase.LoadAssetAtPath<SpawnTableSO>(tablePath) == null)
                AssetDatabase.CreateAsset(EnemyDefaults.CreateTable(types), tablePath);

            // ---- Loadouts
            var freshLoadouts = LoadoutDefaults.CreateAll();
            var loadouts = new StatBlockSO[freshLoadouts.Length];
            for (int i = 0; i < freshLoadouts.Length; i++)
                loadouts[i] = Upsert(Root + "/Loadouts/" + freshLoadouts[i].name + ".asset", freshLoadouts[i], pairs, map, null);

            // ---- Upgrades (they reference each other and the weapons)
            var freshUpgrades = UpgradeDefaults.CreateAll(jaw, gun, hardening);
            var upgrades = new UpgradeSO[freshUpgrades.Length];
            for (int i = 0; i < freshUpgrades.Length; i++)
                upgrades[i] = Upsert(Root + "/Upgrades/" + freshUpgrades[i].id + ".asset", freshUpgrades[i], pairs, map, KeepUpgrade);

            // Point references that still target the temporary copies (for example "requirement: Dense Cells") at the real assets.
            foreach (var pair in pairs)
                if (pair.Value != null) Remap(pair.Value, map);

            // ---- Upgrade pool
            string poolPath = Root + "/Upgrades/UpgradePool.asset";
            var pool = AssetDatabase.LoadAssetAtPath<UpgradePoolSO>(poolPath);
            if (pool == null)
            {
                pool = ScriptableObject.CreateInstance<UpgradePoolSO>();
                AssetDatabase.CreateAsset(pool, poolPath);
            }
            pool.upgrades = upgrades;
            EditorUtility.SetDirty(pool);

            // Destroy the temporary copies that were merged into existing assets.
            foreach (var pair in pairs)
                if (pair.Key != null && pair.Key != pair.Value) UnityEngine.Object.DestroyImmediate(pair.Key);

            File.WriteAllText(VersionFile, DataVersion);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            bool wired = WireScene(loadouts);
            Debug.Log("Cellstorm: data regenerated (" + DataVersion + "). Backup: " + backup +
                      (wired ? "" : "\nOpen the Game scene and run Cellstorm > Data > Wire loadouts into open scene."));
        }

        [MenuItem("Cellstorm/Data/Wire loadouts into open scene")]
        public static void WireMenu()
        {
            var loadouts = LoadLoadouts();
            if (!WireOpenScene(loadouts, false))
                EditorUtility.DisplayDialog("Wire loadouts", "No RunSetup found in the open scene. Open the Game scene first.", "OK");
        }

        // ---------------- Keep fields that are not defaults ----------------

        static void KeepWeapon(WeaponSO existing, WeaponSO fresh) { fresh.icon = existing.icon; }
        static void KeepEnemy(EnemyTypeSO existing, EnemyTypeSO fresh) { fresh.weapon = existing.weapon; }
        static void KeepUpgrade(UpgradeSO existing, UpgradeSO fresh) { fresh.icon = existing.icon; }

        // ---------------- Helpers ----------------

        static T Upsert<T>(string path, T fresh, List<KeyValuePair<UnityEngine.Object, UnityEngine.Object>> pairs,
            Dictionary<UnityEngine.Object, UnityEngine.Object> map, Action<T, T> keep) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(fresh, path);
                pairs.Add(new KeyValuePair<UnityEngine.Object, UnityEngine.Object>(fresh, fresh));
                map[fresh] = fresh;
                return fresh;
            }

            string assetName = existing.name;
            if (keep != null) keep(existing, fresh);
            EditorUtility.CopySerialized(fresh, existing);
            existing.name = assetName;
            EditorUtility.SetDirty(existing);

            pairs.Add(new KeyValuePair<UnityEngine.Object, UnityEngine.Object>(fresh, existing));
            map[fresh] = existing;
            return existing;
        }

        static void Remap(UnityEngine.Object asset, Dictionary<UnityEngine.Object, UnityEngine.Object> map)
        {
            var so = new SerializedObject(asset);
            var it = so.GetIterator();
            bool enter = true;
            bool changed = false;
            while (it.Next(enter))
            {
                enter = true;
                if (it.propertyType != SerializedPropertyType.ObjectReference) continue;
                var value = it.objectReferenceValue;
                if (value != null && map.TryGetValue(value, out var replacement) && replacement != value)
                {
                    it.objectReferenceValue = replacement;
                    changed = true;
                }
            }
            if (changed)
            {
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
            }
        }

        static StatBlockSO[] LoadLoadouts()
        {
            var names = new[] { "Default", "Speedy", "Tank", "Giant" };
            var list = new List<StatBlockSO>();
            foreach (var n in names)
            {
                var b = AssetDatabase.LoadAssetAtPath<StatBlockSO>(Root + "/Loadouts/" + n + ".asset");
                if (b != null) list.Add(b);
            }
            return list.ToArray();
        }

        static bool WireScene(StatBlockSO[] loadouts)
        {
            if (WireOpenScene(loadouts, true)) return true;

            // Not open: open the Game scene from the build settings and wire it.
            foreach (var s in EditorBuildSettings.scenes)
            {
                if (!s.path.EndsWith("/Game.unity")) continue;
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;
                EditorSceneManager.OpenScene(s.path, OpenSceneMode.Single);
                return WireOpenScene(loadouts, true);
            }
            return false;
        }

        static bool WireOpenScene(StatBlockSO[] loadouts, bool save)
        {
            var setup = UnityEngine.Object.FindAnyObjectByType<RunSetup>();
            if (setup == null) return false;

            var so = new SerializedObject(setup);
            var prop = so.FindProperty("loadouts");
            if (prop == null) return false;

            prop.arraySize = loadouts.Length;
            for (int i = 0; i < loadouts.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = loadouts[i];
            so.ApplyModifiedProperties();

            var scene = setup.gameObject.scene;
            EditorSceneManager.MarkSceneDirty(scene);
            if (save) EditorSceneManager.SaveScene(scene);
            return true;
        }

        static string Backup()
        {
            string project = Directory.GetParent(Application.dataPath).FullName;
            string source = Path.Combine(project, "Assets", "Data");
            string target = Path.Combine(project, "Backups", "Data_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            if (Directory.Exists(source)) CopyDirectory(source, target);
            return target;
        }

        static void CopyDirectory(string source, string target)
        {
            Directory.CreateDirectory(target);
            foreach (var file in Directory.GetFiles(source))
                File.Copy(file, Path.Combine(target, Path.GetFileName(file)), true);
            foreach (var dir in Directory.GetDirectories(source))
                CopyDirectory(dir, Path.Combine(target, Path.GetFileName(dir)));
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }
    }

    /// <summary>Asks once per editor session to regenerate when the data version does not match the code.</summary>
    [InitializeOnLoad]
    static class DataVersionCheck
    {
        const string AskedKey = "Cellstorm.DataVersionAsked";

        static DataVersionCheck()
        {
            EditorApplication.delayCall += Check;
        }

        static void Check()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SessionState.GetBool(AskedKey, false)) return;
            if (!AssetDatabase.IsValidFolder("Assets/Data/Weapons")) return; // fresh project: nothing to migrate

            string project = Directory.GetParent(Application.dataPath).FullName;
            string file = Path.Combine(project, "Assets", "Data", "data_version.txt");
            string current = File.Exists(file) ? File.ReadAllText(file).Trim() : "";
            if (current == RegenerateData.DataVersion) return;

            SessionState.SetBool(AskedKey, true);
            if (EditorUtility.DisplayDialog("Cellstorm data is out of date",
                    "The assets in Assets/Data still use the old cell model (version '" + (current == "" ? "V0.1" : current) +
                    "'). Regenerate them now?\n\nA backup is saved first. You can also do this later from Cellstorm > Data.",
                    "Regenerate now", "Later"))
            {
                RegenerateData.Execute();
            }
        }
    }
}
