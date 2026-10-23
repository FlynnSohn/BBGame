using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace BBGame.EditorTools.Data
{
    /// <summary>
    /// 프로토타입 v12 데이터 가져오기 (match_m1_core_plan N1).
    /// Assets/04_Data/Import/proto_v12_data.json → Assets/04_Data/**/*.asset. 있는 에셋은 값만 덮어써 GUID가 유지된다.
    /// JSON은 Docs/tools/proto_export.js가 만든다.
    /// </summary>
    public static class PrototypeDataImporter
    {
        private const string k_Root = "Assets/04_Data";
        private const string k_Json = k_Root + "/Import/proto_v12_data.json";

        [MenuItem("BBGame/Data/프로토타입 v12 데이터 가져오기", false, 100)]
        private static void Import()
        {
            if (!File.Exists(k_Json))
            {
                Debug.LogError($"[Data] {k_Json}이 없다. Docs/tools/proto_export.js를 먼저 실행한다");
                return;
            }
            var data = JsonConvert.DeserializeObject<ProtoData>(File.ReadAllText(k_Json));
            var src = new AssetSource();
            ProtoDataMapper.Apply(data, src);
            foreach (var a in src.Touched) EditorUtility.SetDirty(a);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Data] 가져오기 완료 — 에셋 {src.Touched.Count}개 (새로 {src.Created}개) · 원본 {data.source}");
        }

        /// <summary>Assets/04_Data/{이름}.asset — 있으면 불러오고 없으면 만든다</summary>
        private sealed class AssetSource : ProtoDataMapper.IAssetSource
        {
            public readonly List<ScriptableObject> Touched = new List<ScriptableObject>();
            public int Created;

            public T Get<T>(string name) where T : ScriptableObject
            {
                string path = $"{k_Root}/{name}.asset";
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset == null)
                {
                    EnsureFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'));
                    asset = ScriptableObject.CreateInstance<T>();
                    AssetDatabase.CreateAsset(asset, path);
                    Created++;
                }
                if (!Touched.Contains(asset)) Touched.Add(asset);
                return asset;
            }

            private static void EnsureFolder(string path)
            {
                if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path)) return;
                string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
                EnsureFolder(parent);
                AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
            }
        }
    }
}
