using Cellstorm.Data;
using Cellstorm.Debugging;
using Cellstorm.Progression;
using Cellstorm.Swarm;
using Cellstorm.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cellstorm.EditorTools
{
    public static class Phase5Setup
    {
        [MenuItem("Cellstorm/Phase 5/Setup Game Scene")]
        public static void SetupScene()
        {
            var body = Object.FindFirstObjectByType<SwarmBody>();
            if (body == null)
            {
                EditorUtility.DisplayDialog("Cellstorm",
                    "No SwarmBody found in the open scene. Open the Game scene (Phases 1-4 set up) first.", "OK");
                return;
            }

            var jaw = EnsureWeapon(WeaponDefaults.JawKey, "Jaw");
            var gun = EnsureWeapon(WeaponDefaults.GunKey, "Gun");
            var hardening = EnsureWeapon(WeaponDefaults.HardeningKey, "Hardening");
            var pool = EnsurePool(jaw, gun, hardening);
            var loadouts = EnsureLoadouts();

            if (body.GetComponent<PlayerBuild>() == null) body.gameObject.AddComponent<PlayerBuild>();

            var go = GameObject.Find("Progression");
            if (go == null) go = new GameObject("Progression");

            var progress = GetOrAdd<PlayerProgress>(go);
            GetOrAdd<PickupDropper>(go);
            GetOrAdd<LevelUpScreen>(go);
            var setup = GetOrAdd<RunSetup>(go);
            GetOrAdd<ProgressDebug>(go);

            var pso = new SerializedObject(progress);
            var poolProp = pso.FindProperty("pool");
            if (poolProp != null)
            {
                poolProp.objectReferenceValue = pool;
                pso.ApplyModifiedProperties();
            }

            var sso = new SerializedObject(setup);
            var loadProp = sso.FindProperty("loadouts");
            if (loadProp != null)
            {
                loadProp.arraySize = loadouts.Length;
                for (int i = 0; i < loadouts.Length; i++)
                    loadProp.GetArrayElementAtIndex(i).objectReferenceValue = loadouts[i];
                sso.ApplyModifiedProperties();
            }

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = go;
            Debug.Log("Cellstorm Phase 5 scene setup done. Data is in Assets/Data/Upgrades and Assets/Data/Loadouts. Save the scene and press Play.");
        }

        static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }

        static WeaponSO EnsureWeapon(string key, string fileName)
        {
            EnsureFolder("Assets/Data");
            EnsureFolder("Assets/Data/Weapons");
            string path = "Assets/Data/Weapons/" + fileName + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<WeaponSO>(path);
            if (existing != null) return existing;

            var created = WeaponDefaults.Create(key);
            AssetDatabase.CreateAsset(created, path);
            return created;
        }

        static UpgradePoolSO EnsurePool(WeaponSO jaw, WeaponSO gun, WeaponSO hardening)
        {
            EnsureFolder("Assets/Data");
            EnsureFolder("Assets/Data/Upgrades");

            const string poolPath = "Assets/Data/Upgrades/UpgradePool.asset";
            var pool = AssetDatabase.LoadAssetAtPath<UpgradePoolSO>(poolPath);
            if (pool != null) return pool;

            var upgrades = UpgradeDefaults.CreateAll(jaw, gun, hardening);
            foreach (var u in upgrades)
                AssetDatabase.CreateAsset(u, "Assets/Data/Upgrades/" + u.id + ".asset");

            pool = ScriptableObject.CreateInstance<UpgradePoolSO>();
            pool.upgrades = upgrades;
            AssetDatabase.CreateAsset(pool, poolPath);
            return pool;
        }

        static StatBlockSO[] EnsureLoadouts()
        {
            EnsureFolder("Assets/Data");
            EnsureFolder("Assets/Data/Loadouts");

            var defaults = LoadoutDefaults.CreateAll();
            var result = new StatBlockSO[defaults.Length];
            for (int i = 0; i < defaults.Length; i++)
            {
                string path = "Assets/Data/Loadouts/" + defaults[i].name + ".asset";
                var existing = AssetDatabase.LoadAssetAtPath<StatBlockSO>(path);
                if (existing != null)
                {
                    result[i] = existing;
                    Object.DestroyImmediate(defaults[i]);
                }
                else
                {
                    AssetDatabase.CreateAsset(defaults[i], path);
                    result[i] = defaults[i];
                }
            }
            return result;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }
    }
}
