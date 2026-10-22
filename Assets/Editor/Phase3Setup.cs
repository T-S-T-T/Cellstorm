using Cellstorm.Core;
using Cellstorm.Data;
using Cellstorm.Debugging;
using Cellstorm.Enemies;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cellstorm.EditorTools
{
    public static class Phase3Setup
    {
        const string Folder = "Assets/Data/Enemies";

        [MenuItem("Cellstorm/Phase 3/Setup Game Scene")]
        public static void SetupScene()
        {
            var table = EnsureData();

            if (Object.FindAnyObjectByType<RunDirector>() == null)
                new GameObject("RunDirector").AddComponent<RunDirector>();

            var spawner = Object.FindAnyObjectByType<EnemySpawner>();
            if (spawner == null)
                spawner = new GameObject("EnemySpawner").AddComponent<EnemySpawner>();

            var so = new SerializedObject(spawner);
            var prop = so.FindProperty("table");
            if (prop != null)
            {
                prop.objectReferenceValue = table;
                so.ApplyModifiedProperties();
            }

            if (Object.FindAnyObjectByType<RunDebug>() == null)
                new GameObject("RunDebug").AddComponent<RunDebug>();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("Cellstorm Phase 3 scene setup done. Enemy data is in " + Folder + ". Save the scene and press Play.");
        }

        static SpawnTableSO EnsureData()
        {
            EnsureFolder("Assets/Data");
            EnsureFolder(Folder);

            string[] names = { "SingleCell", "MultiCell", "RangedCell", "ExplodingCell" };
            var defaults = EnemyDefaults.CreateTypes();
            var types = new EnemyTypeSO[defaults.Length];

            for (int i = 0; i < defaults.Length; i++)
            {
                string path = Folder + "/" + names[i] + ".asset";
                var existing = AssetDatabase.LoadAssetAtPath<EnemyTypeSO>(path);
                if (existing != null)
                {
                    types[i] = existing;
                    Object.DestroyImmediate(defaults[i]);
                }
                else
                {
                    AssetDatabase.CreateAsset(defaults[i], path);
                    types[i] = defaults[i];
                }
            }

            string tablePath = Folder + "/SpawnTable.asset";
            var table = AssetDatabase.LoadAssetAtPath<SpawnTableSO>(tablePath);
            if (table == null)
            {
                table = EnemyDefaults.CreateTable(types);
                AssetDatabase.CreateAsset(table, tablePath);
            }

            AssetDatabase.SaveAssets();
            return table;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }
    }
}
