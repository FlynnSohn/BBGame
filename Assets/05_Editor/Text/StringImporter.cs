using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BBGame.Presentation.Data;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Tables;

namespace BBGame.EditorTools.Text
{
    /// <summary>
    /// 문구 가져오기 (match_m2_screen_plan F2) — 원문 목록(strings_ko.json) → String Table(한국어) + 주석 + 표시용 데이터(DisplayCatalog).
    /// · 번호는 원문 목록의 번호 그대로 쓴다 (키 이름 = 번호 문자열). 다시 실행해도 같은 결과
    /// · 모든 키에 Comment를 단다 (decisions 2026-10-13). 변수가 있는 문구는 Smart로
    /// · 각 테이블의 순번 생성기를 (가장 큰 번호 + 1)로 맞춘다 — Tables 창에서 손으로 더해도 번호가 겹치지 않는다
    /// · 다른 언어는 한국어로 대체해 보여 준다 (F6 — 개발 중 빈칸 방지)
    /// </summary>
    public static class StringImporter
    {
        private const string k_Source = "Assets/04_Data/Presentation/strings_ko.json";
        internal const string CatalogPath = "Assets/04_Data/Presentation/DisplayCatalog.asset";
        private const string k_Korean = "ko";

        [MenuItem("BBGame/Data/문구 가져오기", false, 20)]
        private static void Import()
        {
            if (!File.Exists(k_Source))
            {
                Debug.LogError($"[문구] 원문 목록이 없다: {k_Source} — node Docs/tools/strings_export.js 로 만든다");
                return;
            }
            var root = JObject.Parse(File.ReadAllText(k_Source));
            var entries = (JArray)root["entries"];
            var maxIds = root["maxIds"] as JObject;

            var korean = FindLocale(k_Korean);
            if (korean == null) { Debug.LogError("[문구] ko Locale이 없다"); return; }

            var catalog = LoadOrCreateCatalog();
            var binder = new CatalogBinder(catalog);
            var touched = new HashSet<StringTableCollection>();
            var maxInTable = new Dictionary<StringTableCollection, long>();
            int count = 0;

            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (JObject e in entries)
                {
                    long id = (long)e["id"];
                    string tableName = (string)e["table"];
                    string ko = (string)e["ko"];
                    string comment = (string)e["comment"];
                    var collection = LocalizationEditorSettings.GetStringTableCollection(tableName);
                    if (collection == null) throw new InvalidOperationException($"String Table '{tableName}'이 없다 (메뉴 BBGame/Setup/3)");

                    var shared = collection.SharedData;
                    var sharedEntry = shared.GetEntry(id) ?? shared.AddKey(id.ToString(), id);
                    SetComment(sharedEntry.Metadata, comment);

                    var table = (StringTable)collection.GetTable(korean.Identifier);
                    if (table == null) throw new InvalidOperationException($"'{tableName}'에 한국어 테이블이 없다");
                    var entry = table.GetEntry(id) ?? table.AddEntry(id, ko);
                    entry.Value = ko;
                    entry.IsSmart = ko.IndexOf('{') >= 0;
                    SetComment(entry, comment);

                    touched.Add(collection);
                    maxInTable[collection] = Math.Max(maxInTable.TryGetValue(collection, out var m) ? m : 0, id);

                    var text = new LocalizedString(shared.TableCollectionNameGuid, id);
                    foreach (var b in (JArray)e["bind"]) binder.Bind((string)b, text);
                    count++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            // 순번 생성기 — 지운 번호도 다시 쓰지 않도록 원문 목록의 기록(maxIds)과 비교해 큰 쪽 + 1
            foreach (var collection in touched)
            {
                long next = maxInTable[collection];
                if (maxIds != null && maxIds.TryGetValue(collection.SharedData.TableCollectionName, out var recorded)) next = Math.Max(next, (long)recorded);
                collection.SharedData.KeyGenerator = new SequentialIDGenerator(next + 1);
                EditorUtility.SetDirty(collection.SharedData);
                foreach (var t in collection.StringTables) EditorUtility.SetDirty(t);
            }

            binder.Finish();
            EditorUtility.SetDirty(catalog);
            UseKoreanFallback(korean);
            AssetDatabase.SaveAssets();
            Debug.Log($"[문구] {count}개 가져옴 · 테이블 {touched.Count}개 · 연결 {binder.Bound}개 → {CatalogPath}");
        }

        private static Locale FindLocale(string code)
        {
            foreach (var l in LocalizationEditorSettings.GetLocales())
                if (l.Identifier.Code == code) return l;
            return null;
        }

        private static DisplayCatalog LoadOrCreateCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<DisplayCatalog>(CatalogPath);
            if (catalog != null) return catalog;
            Directory.CreateDirectory(Path.GetDirectoryName(CatalogPath));
            catalog = ScriptableObject.CreateInstance<DisplayCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
            return catalog;
        }

        private static void SetComment(IMetadataCollection target, string text)
        {
            var c = target.GetMetadata<Comment>();
            if (c == null) { c = new Comment(); target.AddMetadata(c); }
            c.CommentText = text;
        }

        /// <summary>다른 언어에 번역이 없으면 한국어를 보여 준다 (개발 중). 출시 대체 언어 정책은 미정 (localization_research §8)</summary>
        private static void UseKoreanFallback(Locale korean)
        {
            foreach (var l in LocalizationEditorSettings.GetLocales())
            {
                if (l == korean || l.Metadata.GetMetadata<FallbackLocale>() != null) continue;
                l.Metadata.AddMetadata(new FallbackLocale(korean));
                EditorUtility.SetDirty(l);
            }
            var settings = LocalizationEditorSettings.ActiveLocalizationSettings;
            if (settings != null && !settings.GetStringDatabase().UseFallback)
            {
                settings.GetStringDatabase().UseFallback = true;
                EditorUtility.SetDirty(settings);
            }
        }

        /// <summary>
        /// bind 문자열 → DisplayCatalog 칸. "ui:필드" · "&lt;표&gt;:&lt;enum 이름&gt;"(EnumTable) · "&lt;표&gt;:&lt;id&gt;"(IdTable).
        /// 표는 가져올 때마다 새로 만든다 — 원문 목록에서 빠진 연결이 남지 않게
        /// </summary>
        private sealed class CatalogBinder
        {
            private const BindingFlags k_Fields = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

            private readonly DisplayCatalog _catalog;
            private readonly Dictionary<string, (FieldInfo Field, Type Enum)> _enumTables = new Dictionary<string, (FieldInfo, Type)>();
            private readonly Dictionary<string, (FieldInfo Field, List<IdText> List)> _idTables = new Dictionary<string, (FieldInfo, List<IdText>)>();
            private readonly UiTexts _ui = new UiTexts();

            public int Bound { get; private set; }

            public CatalogBinder(DisplayCatalog catalog)
            {
                _catalog = catalog;
                foreach (var f in typeof(DisplayCatalog).GetFields(k_Fields))
                {
                    var et = f.GetCustomAttribute<EnumTableAttribute>();
                    if (et != null)
                    {
                        _enumTables[et.Key] = (f, et.EnumType);
                        f.SetValue(catalog, new LocalizedString[EnumIndex.Size(et.EnumType)]);
                    }
                    var it = f.GetCustomAttribute<IdTableAttribute>();
                    if (it != null) _idTables[it.Key] = (f, new List<IdText>());
                }
            }

            public void Bind(string bind, LocalizedString text)
            {
                int colon = bind.IndexOf(':');
                if (colon <= 0) throw new FormatException($"bind '{bind}' — '표:이름' 형식이 아니다");
                string key = bind.Substring(0, colon), name = bind.Substring(colon + 1);

                if (key == "ui")
                {
                    var f = typeof(UiTexts).GetField(name, BindingFlags.Instance | BindingFlags.Public);
                    if (f == null || f.FieldType != typeof(LocalizedString)) throw new FormatException($"bind '{bind}' — UiTexts에 그런 필드가 없다");
                    f.SetValue(_ui, text);
                }
                else if (_enumTables.TryGetValue(key, out var et))
                {
                    var value = Enum.Parse(et.Enum, name);
                    var array = (LocalizedString[])et.Field.GetValue(_catalog);
                    array[EnumIndex.OfBoxed(et.Enum, value)] = text;
                }
                else if (_idTables.TryGetValue(key, out var it))
                {
                    it.List.Add(new IdText { id = name, text = text });
                }
                else throw new FormatException($"bind '{bind}' — 모르는 표 '{key}'");
                Bound++;
            }

            /// <summary>모은 목록·화면 문구를 에셋에 넣는다. 비어 있는 화면 문구 칸은 경고</summary>
            public void Finish()
            {
                foreach (var (field, list) in _idTables.Values) field.SetValue(_catalog, list.ToArray());
                foreach (var f in typeof(UiTexts).GetFields(BindingFlags.Instance | BindingFlags.Public))
                {
                    var v = (LocalizedString)f.GetValue(_ui);
                    if (v == null || v.IsEmpty) Debug.LogWarning($"[문구] UiTexts.{f.Name}에 연결된 문구가 없다");
                }
                _catalog._ui = _ui;
            }
        }
    }
}
