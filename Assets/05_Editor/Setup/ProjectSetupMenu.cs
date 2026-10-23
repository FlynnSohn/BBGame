using System.Collections.Generic;
using System.IO;
using BBGame.Presentation.Common;
using TMPro;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Localization.Tables;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace BBGame.EditorTools.Setup
{
    /// <summary>
    /// M0 프로젝트 셋업 메뉴 (Docs/work/match_m0_foundation_plan.md).
    /// 씬·Localization·TMP 에셋은 YAML을 손으로 쓰지 않고 Unity API로 만든다.
    /// 모든 메뉴는 여러 번 실행해도 결과가 같다 — 이미 있는 것은 건너뛴다.
    /// 순서: 1 → (도메인 리로드 대기) → 2 ~ 5 (또는 "2~5 한 번에")
    /// </summary>
    public static class ProjectSetupMenu
    {
        private const string k_Menu = "BBGame/Setup/";

        // 폰트 (Q12, E3)
        private const string k_FontDir = "Assets/03_Asset/Fonts/Pretendard";
        private const string k_FontRegular = k_FontDir + "/Pretendard-Regular.otf";
        private const string k_FontBold = k_FontDir + "/Pretendard-Bold.otf";
        private const string k_FontAssetRegular = k_FontDir + "/Pretendard-Regular SDF.asset";
        private const string k_FontAssetBold = k_FontDir + "/Pretendard-Bold SDF.asset";

        // String Table (Q15, E2)
        private const string k_TableRoot = "Assets/Localization/Tables";

        // 씬 (match_loop_plan §6-1)
        private const string k_SourceScene = "Assets/00_Scenes/SampleScene.unity";
        private const string k_MatchScene = "Assets/00_Scenes/Match.unity";

        /// <summary>분류별 String Table과 키 시작 번호 (decisions 2026-10-13 텍스트 + E2)</summary>
        private static readonly (string Name, long Start)[] k_Tables =
        {
            ("UI", 9910001),        // 메뉴·HUD·버튼·공통
            ("Reveal", 9920001),    // 리빌 문구, 판정 용어, 결과 태그
            ("Narration", 9930001), // 내레이션 관찰·속내·판정 줄, 등장 중계
            ("Clue", 9940001),      // 자세·상황·스카우팅 단서
            ("Card", 9950001),      // 카드 이름·설명·특수 효과
            ("Bonus", 9960001),     // 이닝 보너스·진화
            ("Batter", 9970001),    // 타자 유형·선수·학교 이름
            ("Weather", 9980001),   // 날씨·컨디션
            ("System", 9990001),    // 시스템·오류·예비
        };

        /// <summary>캔버스 구성 — 이름, 정렬 순서, 레이캐스터, SafeArea (match_loop_plan §6-1)</summary>
        private static readonly (string Name, int Order, bool Raycaster, bool SafeArea)[] k_Canvases =
        {
            ("Canvas_Field", 0, false, false),  // 필드 그림 — 화면 끝까지 덮으므로 SafeArea 밖
            ("Canvas_HUD", 10, true, true),     // 전광판 줄·타자 패널 (잘 안 바뀜)
            ("Canvas_Play", 20, true, true),    // 존·손패·버튼 줄 (투구마다 바뀜)
            ("Canvas_Reveal", 30, true, true),  // 리빌
            ("Canvas_Modal", 40, true, true),   // 타임 메뉴·확인 창·3택1
        };

        // ───────────────────────── 1. TMP 기본 리소스 ─────────────────────────

        /// <summary>TMP Essential Resources만 가져온다 (예제 제외). 가져온 뒤 도메인 리로드가 일어난다</summary>
        [MenuItem(k_Menu + "1. TMP 기본 리소스", false, 1)]
        private static void ImportTmpEssentials()
        {
            // TMP_Settings.instance는 없을 때 가져오기 창을 띄우므로 Resources.Load로 직접 확인한다
            if (Resources.Load<TMP_Settings>("TMP Settings") != null)
            {
                Debug.Log("[Setup] TMP 기본 리소스가 이미 있다 — 건너뜀");
                return;
            }
            TMP_PackageResourceImporter.ImportResources(true, false, false);
            Debug.Log("[Setup] TMP 기본 리소스 가져오기 시작. 리로드가 끝나면 2번부터 실행한다");
        }

        // ───────────────────────── 2. Pretendard 폰트 에셋 ─────────────────────────

        /// <summary>
        /// Pretendard Regular·Bold의 TMP 폰트 에셋을 Dynamic 아틀라스로 만든다 (E3).
        /// Regular의 굵기 표 700에 Bold를 연결하고, TMP 기본 폰트를 Regular로 바꾼다.
        /// </summary>
        [MenuItem(k_Menu + "2. Pretendard 폰트 에셋", false, 2)]
        private static void CreatePretendardFontAssets()
        {
            var settings = Resources.Load<TMP_Settings>("TMP Settings");
            if (settings == null)
            {
                Debug.LogError("[Setup] TMP 기본 리소스가 없다. 1번을 먼저 실행한다");
                return;
            }

            var regular = CreateOrLoadFontAsset(k_FontRegular, k_FontAssetRegular);
            var bold = CreateOrLoadFontAsset(k_FontBold, k_FontAssetBold);
            if (regular == null || bold == null)
                return;

            // <b> 태그와 FontStyle.Bold가 가짜 굵게(두껍게 그리기) 대신 Bold 글꼴을 쓰게 한다
            var weights = regular.fontWeightTable;
            if (weights != null && weights.Length > 7 && weights[7].regularTypeface != bold)
            {
                weights[7].regularTypeface = bold;  // 인덱스 7 = 굵기 700
                EditorUtility.SetDirty(regular);
            }

            if (TMP_Settings.defaultFontAsset != regular)
            {
                TMP_Settings.defaultFontAsset = regular;
                EditorUtility.SetDirty(settings);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] Pretendard 폰트 에셋 준비 완료");
        }

        /// <summary>폰트 에셋이 있으면 불러오고, 없으면 Dynamic SDF로 만들어 저장한다</summary>
        private static TMP_FontAsset CreateOrLoadFontAsset(string fontPath, string assetPath)
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null)
                return existing;

            var font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
            if (font == null)
            {
                Debug.LogError($"[Setup] 폰트 파일이 없다: {fontPath}");
                return null;
            }

            // Dynamic 아틀라스 2048, 샘플 90pt, 패딩 9, SDF — 한글은 쓰는 글자만 아틀라스에 올린다.
            // 다중 아틀라스를 켜 두어 글자가 많아져도 잘리지 않는다
            var fontAsset = TMP_FontAsset.CreateFontAsset(
                font, 90, 9, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
            if (fontAsset == null)
            {
                Debug.LogError($"[Setup] 폰트 에셋을 만들지 못했다: {fontPath}");
                return null;
            }

            fontAsset.name = Path.GetFileNameWithoutExtension(assetPath);
            AssetDatabase.CreateAsset(fontAsset, assetPath);

            // 아틀라스 텍스처와 머티리얼을 하위 에셋으로 넣어야 에셋을 다시 열어도 온전하다
            fontAsset.atlasTexture.name = fontAsset.name + " Atlas";
            AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
            fontAsset.material.name = fontAsset.name + " Material";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);

            EditorUtility.SetDirty(fontAsset);
            return fontAsset;
        }

        // ───────────────────────── 3. String Table ─────────────────────────

        /// <summary>
        /// 분류별 String Table 컬렉션을 만든다. 새 키는 대역 시작 번호부터 +1로 붙고, 모든 테이블을 Preload한다.
        /// </summary>
        [MenuItem(k_Menu + "3. String Table", false, 3)]
        private static void CreateStringTables()
        {
            EnsureFolder(k_TableRoot);

            foreach (var (name, start) in k_Tables)
            {
                if (LocalizationEditorSettings.GetStringTableCollection(name) != null)
                {
                    Debug.Log($"[Setup] String Table '{name}'이 이미 있다 — 건너뜀");
                    continue;
                }

                string dir = $"{k_TableRoot}/{name}";
                EnsureFolder(dir);
                var collection = LocalizationEditorSettings.CreateStringTableCollection(name, dir);

                // 숫자 키를 손으로 정하지 않는다 — 내장 순번 생성기가 대역 안에서 +1로 붙인다
                collection.SharedData.KeyGenerator = new SequentialIDGenerator(start);
                EditorUtility.SetDirty(collection.SharedData);

                // 첫 화면에서 글자가 비지 않게 미리 읽는다 (localization_research §f)
                foreach (var table in collection.StringTables)
                    LocalizationEditorSettings.SetPreloadTableFlag(table, true);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] String Table 준비 완료");
        }

        // ───────────────────────── 4. 경기 씬 골격 ─────────────────────────

        /// <summary>
        /// SampleScene(URP 2D 카메라·Global Light 2D)을 복사해 Match.unity를 만들고,
        /// EventSystem과 캔버스 5개 골격을 넣은 뒤 빌드 목록 첫 칸에 둔다.
        /// </summary>
        [MenuItem(k_Menu + "4. 경기 씬 골격", false, 4)]
        private static void CreateMatchSceneSkeleton()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(k_MatchScene) == null &&
                !AssetDatabase.CopyAsset(k_SourceScene, k_MatchScene))
            {
                Debug.LogError($"[Setup] 씬을 복사하지 못했다: {k_SourceScene} → {k_MatchScene}");
                return;
            }

            // 열려 있는 씬에 저장하지 않은 변경이 있으면 사용자에게 묻는다
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Scene scene = EditorSceneManager.OpenScene(k_MatchScene, OpenSceneMode.Single);

            EnsureEventSystem(scene);
            foreach (var (name, order, raycaster, safeArea) in k_Canvases)
            {
                var canvas = EnsureCanvas(scene, name, order, raycaster, safeArea);
                if (name == "Canvas_Field")
                    EnsureBackground(canvas.transform);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            PutMatchSceneFirstInBuild();
            Debug.Log("[Setup] 경기 씬 골격 준비 완료");
        }

        /// <summary>EventSystem + InputSystemUIInputModule (구 StandaloneInputModule 금지)</summary>
        private static void EnsureEventSystem(Scene scene)
        {
            var go = FindRoot(scene, "EventSystem");
            if (go == null)
            {
                go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
            }

            var module = go.GetComponent<InputSystemUIInputModule>();
            if (module == null)
                return;

            // 프로젝트 전역 Input Actions(InputSystem_Actions)의 UI 맵을 쓴다.
            // AssignDefaultActions()는 기본 액션의 복사본을 씬 안에 통째로 직렬화하므로 쓰지 않는다.
            // M2의 경기 단축키(Match 맵)도 같은 에셋에 둔다
            var projectWide = UnityEngine.InputSystem.InputSystem.actions;
            if (projectWide != null && projectWide.FindActionMap("UI") != null)
            {
                if (module.actionsAsset != projectWide)
                {
                    module.actionsAsset = projectWide;   // 세터가 Point·Click 등 참조를 새 에셋의 같은 이름 액션으로 다시 잇는다
                    EditorUtility.SetDirty(module);
                }
            }
            else if (module.actionsAsset == null)
            {
                module.AssignDefaultActions();            // 전역 액션이 없을 때만 대비책
            }
        }

        /// <summary>루트 캔버스를 만든다. 공통 CanvasScaler: 1920×1080, 높이 기준 (decisions 2026-10-13)</summary>
        private static Canvas EnsureCanvas(Scene scene, string name, int order, bool raycaster, bool safeArea)
        {
            var existing = FindRoot(scene, name);
            if (existing != null)
                return existing.GetComponent<Canvas>();

            int uiLayer = LayerMask.NameToLayer("UI");
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            go.layer = uiLayer;

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            if (raycaster)
                go.AddComponent<GraphicRaycaster>();

            if (safeArea)
            {
                // 화면 요소는 이 아래에 둔다 — 노치·홈 표시줄을 피한다
                var area = new GameObject("SafeArea", typeof(RectTransform), typeof(SafeAreaFitter));
                area.layer = uiLayer;
                area.transform.SetParent(go.transform, false);
                Stretch((RectTransform)area.transform);
            }

            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            return canvas;
        }

        /// <summary>
        /// 배경 자리. 그림은 넣지 않는다 — 배경 그림은 git 제외 폴더에 있어 씬이 참조하면 다른 PC에서 Missing이 된다 (E5).
        /// 화면비가 16:9보다 넓거나 좁아도 빈틈 없이 덮도록 Envelope Parent로 맞춘다.
        /// </summary>
        private static void EnsureBackground(Transform canvas)
        {
            if (canvas.Find("Background") != null)
                return;

            var go = new GameObject("Background", typeof(RectTransform), typeof(Image), typeof(AspectRatioFitter));
            go.layer = canvas.gameObject.layer;
            go.transform.SetParent(canvas, false);
            Stretch((RectTransform)go.transform);

            var image = go.GetComponent<Image>();
            image.raycastTarget = false;                          // 입력을 받지 않는다
            image.color = new Color(0.61f, 0.78f, 0.91f, 1f);     // 그림을 연결하기 전 임시 하늘색

            var fitter = go.GetComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 16f / 9f;                        // 배경 그림 1920×1080
        }

        /// <summary>Match를 빌드 목록 첫 칸에 두고 SampleScene은 뺀다 (파일은 지우지 않는다)</summary>
        private static void PutMatchSceneFirstInBuild()
        {
            var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(k_MatchScene, true) };
            foreach (var s in EditorBuildSettings.scenes)
            {
                if (s.path != k_MatchScene && s.path != k_SourceScene)
                    scenes.Add(s);
            }
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // ───────────────────────── 5. 화면 방향 ─────────────────────────

        /// <summary>가로 고정 — 자동 회전이되 Landscape Left/Right만 허용 (decisions 2026-10-13)</summary>
        [MenuItem(k_Menu + "5. 화면 방향 (가로 고정)", false, 5)]
        private static void LockLandscape()
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            // PlayerSettings API는 메모리 값만 바꾸고 ProjectSettings.asset에 바로 쓰지 않는다 — 에셋을 더티로 표시해 저장한다
            var playerSettings = AssetDatabase.LoadMainAssetAtPath("ProjectSettings/ProjectSettings.asset");
            if (playerSettings != null)
            {
                EditorUtility.SetDirty(playerSettings);
                AssetDatabase.SaveAssetIfDirty(playerSettings);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] 화면 방향: 가로 두 방향만 허용");
        }

        // ───────────────────────── 2~5 한 번에 ─────────────────────────

        /// <summary>1번(도메인 리로드)이 끝난 뒤 나머지를 차례로 실행한다</summary>
        [MenuItem(k_Menu + "2~5 한 번에", false, 20)]
        private static void RunAfterTmp()
        {
            CreatePretendardFontAssets();
            CreateStringTables();
            CreateMatchSceneSkeleton();
            LockLandscape();
        }

        // ───────────────────────── 도우미 ─────────────────────────

        /// <summary>씬 루트에서 이름으로 찾는다 (비활성 포함)</summary>
        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                    return root;
            }
            return null;
        }

        /// <summary>부모를 꽉 채우도록 앵커·오프셋을 맞춘다</summary>
        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>Assets 아래 폴더를 부모부터 차례로 만든다</summary>
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
