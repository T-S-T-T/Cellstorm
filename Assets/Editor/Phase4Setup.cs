using Cellstorm.Data;
using Cellstorm.Debugging;
using Cellstorm.Enemies;
using Cellstorm.Swarm;
using Cellstorm.Weapons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cellstorm.EditorTools
{
    public static class Phase4Setup
    {
        const string Folder = "Assets/Data/Weapons";

        [MenuItem("Cellstorm/Phase 4/Setup Game Scene")]
        public static void SetupScene()
        {
            var body = Object.FindAnyObjectByType<SwarmBody>();
            if (body == null)
            {
                EditorUtility.DisplayDialog("Cellstorm",
                    "No SwarmBody found in the open scene. Open the Game scene (Phases 1-3 set up) first.", "OK");
                return;
            }

            var ram = EnsureWeapon(WeaponDefaults.RamKey, "Ram");
            var jaw = EnsureWeapon(WeaponDefaults.JawKey, "Jaw");
            var gun = EnsureWeapon(WeaponDefaults.GunKey, "Gun");
            var hardening = EnsureWeapon(WeaponDefaults.HardeningKey, "Hardening");
            var enemyGun = EnsureWeapon(WeaponDefaults.EnemyGunKey, "EnemyGun");

            // Player abilities, starting with Ram on LMB.
            var controller = body.GetComponent<AbilityController>();
            if (controller == null) controller = body.gameObject.AddComponent<AbilityController>();
            var so = new SerializedObject(controller);
            var loadout = so.FindProperty("startingLoadout");
            if (loadout != null)
            {
                loadout.arraySize = AbilityController.SlotCount;
                loadout.GetArrayElementAtIndex(0).objectReferenceValue = ram;
                so.ApplyModifiedProperties();
            }

            // Test harness with the saved assets.
            var debug = Object.FindAnyObjectByType<AbilityDebug>();
            if (debug == null) debug = new GameObject("AbilityDebug").AddComponent<AbilityDebug>();
            var dso = new SerializedObject(debug);
            SetRef(dso, "ram", ram);
            SetRef(dso, "jaw", jaw);
            SetRef(dso, "gun", gun);
            SetRef(dso, "hardening", hardening);
            dso.ApplyModifiedProperties();

            // Ranged cell data gets the enemy gun.
            var ranged = AssetDatabase.LoadAssetAtPath<EnemyTypeSO>("Assets/Data/Enemies/RangedCell.asset");
            if (ranged != null)
            {
                ranged.weapon = enemyGun;
                EditorUtility.SetDirty(ranged);
            }

            // Boss weapon pool.
            var spawner = Object.FindAnyObjectByType<EnemySpawner>();
            if (spawner != null)
            {
                var sso = new SerializedObject(spawner);
                var pool = sso.FindProperty("bossWeapons");
                if (pool != null)
                {
                    pool.arraySize = 3;
                    pool.GetArrayElementAtIndex(0).objectReferenceValue = ram;
                    pool.GetArrayElementAtIndex(1).objectReferenceValue = jaw;
                    pool.GetArrayElementAtIndex(2).objectReferenceValue = enemyGun;
                    sso.ApplyModifiedProperties();
                }
            }

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = body.gameObject;
            Debug.Log("Cellstorm Phase 4 scene setup done. Weapon data is in " + Folder + ". Save the scene and press Play.");
        }

        static void SetRef(SerializedObject so, string field, Object value)
        {
            var p = so.FindProperty(field);
            if (p != null) p.objectReferenceValue = value;
        }

        static WeaponSO EnsureWeapon(string key, string fileName)
        {
            EnsureFolder("Assets/Data");
            EnsureFolder(Folder);

            string path = Folder + "/" + fileName + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<WeaponSO>(path);
            if (existing != null) return existing;

            var created = WeaponDefaults.Create(key);
            AssetDatabase.CreateAsset(created, path);
            return created;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }
    }
}
