using System;
using System.Collections.Generic;
using System.IO;
using BBGame.Core;
using BBGame.EditorTools.Text;
using BBGame.Presentation.Common;
using BBGame.Presentation.Data;
using BBGame.Presentation.Match;
using BBGame.Presentation.Views;
using TMPro;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BBGame.EditorTools.Setup
{
    /// <summary>
    /// M2 경기 화면 자리 만들기 (match_m2_screen_plan F1) — 레퍼런스 좌표(1920×1080)대로 View 프리팹을 만들고 Match 씬에 놓아 연결한다.
    /// · 프리팹이 이미 있으면 다시 만들지 않는다 — 사용자가 고친 모양을 덮어쓰지 않는다 (새로 만들려면 프리팹을 지우고 실행)
    /// · 씬의 "[M2]" 오브젝트는 실행할 때마다 지우고 다시 놓는다 (연결이 빠지지 않게)
    /// · 모양은 자리 표시다 — 기본 둥근 스프라이트와 UiPalette 색. 최종 아트는 사용자 UI 작업을 따른다
    /// 순서: 데이터 가져오기 → 문구 가져오기 → 7. 경기 단축키 → 이 메뉴
    /// </summary>
    public static class MatchScreenBuilder
    {
        private const string k_Scene = "Assets/00_Scenes/Match.unity";
        private const string k_PrefabDir = "Assets/02_Prefab/Match";
        private const string k_UiDir = "Assets/03_Asset/UI";
        private const string k_PalettePath = k_UiDir + "/UiPalette.asset";
        private const string k_FramePath = k_UiDir + "/Frame2px.png";
        private const string k_WhitePath = k_UiDir + "/White.png";
        private const string k_BoldFont = "Assets/03_Asset/Fonts/Pretendard/Pretendard-Bold SDF.asset";
        private const string k_SetupPath = "Assets/04_Data/Presentation/TestMatchSetup.asset";
        private const string k_DatabasePath = "Assets/04_Data/Database.asset";
        private const string k_FieldArt = "Assets/04_Data/Art/field_base.png";
        private const string k_BatterArt = "Assets/04_Data/Art/batter_right.png";
        private const string k_Actions = "Assets/InputSystem_Actions.inputactions";
        private const string k_Mark = "[M2] ";

        // 레퍼런스 해상도 (decisions: 1920×1080, 높이 기준)
        private const float W = 1920f, H = 1080f;
        // 필드 그림 속 타자 자리 (배경 1920×1080 기준 픽셀) — 좌타자는 존 가운데(x 1151)를 기준으로 좌우 대칭
        private static readonly Rect k_RightBatter = new Rect(1221, 110, 353, 768);
        private static readonly Rect k_LeftBatter = new Rect(728, 110, 353, 768);

        /// <summary>앵커 묶음 — 레퍼런스 좌표를 그대로 넣고, 화면비가 바뀌면 이 앵커 기준으로 붙는다</summary>
        private enum Anchor { TopLeft, Top, TopRight, Center, Bottom, Stretch }

        private static UiPalette s_pal;
        private static DisplayCatalog s_cat;
        private static Sprite s_round, s_knob, s_frame, s_white;
        private static TMP_FontAsset s_bold;

        [MenuItem("BBGame/Setup/6. 경기 화면 자리 만들기", false, 6)]
        private static void Build()
        {
            s_cat = AssetDatabase.LoadAssetAtPath<DisplayCatalog>(StringImporter.CatalogPath);
            if (s_cat == null) { Debug.LogError("[M2] 표시용 데이터가 없다 — 먼저 BBGame/Data/문구 가져오기"); return; }
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(k_Actions);
            if (actions == null || actions.FindActionMap("Match") == null) { Debug.LogError("[M2] Match 맵이 없다 — 먼저 BBGame/Setup/7. 경기 단축키"); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EnsureFolder(k_PrefabDir);
            EnsureFolder(k_UiDir);
            s_pal = EnsureAsset<UiPalette>(k_PalettePath);
            s_frame = EnsureGeneratedSprite(k_FramePath, 16, 2, true);
            s_white = EnsureGeneratedSprite(k_WhitePath, 4, 0, false);
            s_round = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            s_knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            s_bold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(k_BoldFont);
            var setup = EnsureTestSetup();
            var fieldSprite = PrepareArt(k_FieldArt);
            var batterSprite = PrepareArt(k_BatterArt);

            // 프리팹 — 없을 때만 만든다
            var cardPrefab = EnsurePrefab("Card", BuildCard);
            var prefabs = new Dictionary<string, GameObject>
            {
                ["Scoreboard"] = EnsurePrefab("Scoreboard", BuildScoreboard),
                ["PitchCount"] = EnsurePrefab("PitchCount", BuildPitchCount),
                ["TodayChips"] = EnsurePrefab("TodayChips", BuildToday),
                ["ResourceBar"] = EnsurePrefab("ResourceBar", BuildResourceBar),
                ["BatterPanel"] = EnsurePrefab("BatterPanel", BuildBatterPanel),
                ["StrikeZone"] = EnsurePrefab("StrikeZone", BuildStrikeZone),
                ["Hand"] = EnsurePrefab("Hand", () => BuildHand(cardPrefab)),
                ["ActionBar"] = EnsurePrefab("ActionBar", BuildActionBar),
                ["ThrowButton"] = EnsurePrefab("ThrowButton", BuildThrowButton),
                ["Reveal"] = EnsurePrefab("Reveal", BuildReveal),
                ["Banner"] = EnsurePrefab("Banner", BuildBanner),
                ["DragGhost"] = EnsurePrefab("DragGhost", BuildGhost),
                ["BonusPick"] = EnsurePrefab("BonusPick", BuildBonusPick),       // M3
                ["MatchResult"] = EnsurePrefab("MatchResult", BuildResult),     // M3
            };

            // 씬 — [M2] 오브젝트를 다시 놓는다
            var scene = EditorSceneManager.OpenScene(k_Scene, OpenSceneMode.Single);
            RemoveGenerated(scene);
            var hud = SafeArea(scene, "Canvas_HUD");
            var play = SafeArea(scene, "Canvas_Play");
            var reveal = SafeArea(scene, "Canvas_Reveal");
            var modal = SafeArea(scene, "Canvas_Modal");
            var fieldCanvas = Root(scene, "Canvas_Field").transform;

            var views = new MatchViews
            {
                scoreboard = Place<ScoreboardView>(prefabs["Scoreboard"], hud),
                pitchCount = Place<PitchCountView>(prefabs["PitchCount"], hud),
                today = Place<TodayChipsView>(prefabs["TodayChips"], hud),
                stamina = Place<ResourceBarView>(prefabs["ResourceBar"], hud, "Stamina"),
                focus = Place<ResourceBarView>(prefabs["ResourceBar"], hud, "Focus"),
                batter = Place<BatterPanelView>(prefabs["BatterPanel"], hud),
                zone = Place<StrikeZoneView>(prefabs["StrikeZone"], play),
                hand = Place<HandView>(prefabs["Hand"], play),
                actions = Place<ActionBarView>(prefabs["ActionBar"], play),
                throwButton = Place<ThrowButtonView>(prefabs["ThrowButton"], play),
                reveal = Place<SimpleRevealView>(prefabs["Reveal"], reveal),
                banner = Place<MatchBannerView>(prefabs["Banner"], modal),
                bonus = Place<BonusPickView>(prefabs["BonusPick"], modal),
                result = Place<MatchResultView>(prefabs["MatchResult"], modal),
            };
            var ghost = Place<DragGhostView>(prefabs["DragGhost"], modal);   // 맨 위 — 마지막에 놓는다

            // 집중력 칸 — 같은 프리팹을 오른쪽에 하나 더 (제목만 다르다)
            var focusRect = views.focus.GetComponent<RectTransform>();
            SetRect(focusRect, Anchor.TopRight, 1503, 35, 215, 110);
            Record(focusRect);
            var focusTitle = views.focus.transform.Find("Title").GetComponent<TextMeshProUGUI>();
            SetLabel(focusTitle, s_cat.Ui.focusTitle);
            Record(focusTitle);
            Record(focusTitle.GetComponent<LocalizedLabel>());

            views.hand._ghost = ghost;
            Record(views.hand);
            views.reveal.gameObject.SetActive(false);
            Record(views.reveal.gameObject);
            views.banner.gameObject.SetActive(false);
            Record(views.banner.gameObject);
            views.bonus.gameObject.SetActive(false);
            Record(views.bonus.gameObject);
            views.result.gameObject.SetActive(false);
            Record(views.result.gameObject);
            views.field = BuildField(fieldCanvas, fieldSprite, batterSprite);

            // 입력 — PlayerInput(Invoke C# Events) + Match 맵
            var inputGo = new GameObject(k_Mark + "Input", typeof(PlayerInput), typeof(MatchInputRouter));
            SceneManager.MoveGameObjectToScene(inputGo, scene);
            var pi = inputGo.GetComponent<PlayerInput>();
            pi.actions = actions;
            pi.defaultActionMap = "Match";
            pi.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
            var router = inputGo.GetComponent<MatchInputRouter>();
            router._input = pi;

            // 시작
            var bootGo = new GameObject(k_Mark + "Match", typeof(MatchBootstrap));
            SceneManager.MoveGameObjectToScene(bootGo, scene);
            var boot = bootGo.GetComponent<MatchBootstrap>();
            boot._setup = setup;
            boot._catalog = s_cat;
            boot._palette = s_pal;
            boot._input = router;
            boot._views = views;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[M2] 경기 화면 자리 완료 — Play로 0단계 경기를 연다");
        }

        // ───────────────────────── 준비물 ─────────────────────────

        private static T EnsureAsset<T>(string path) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a != null) return a;
            a = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(a, path);
            return a;
        }

        private static TestMatchSetup EnsureTestSetup()
        {
            var s = AssetDatabase.LoadAssetAtPath<TestMatchSetup>(k_SetupPath);
            if (s != null) return s;
            s = ScriptableObject.CreateInstance<TestMatchSetup>();
            s._database = AssetDatabase.LoadAssetAtPath<GameDatabase>(k_DatabasePath);
            AssetDatabase.CreateAsset(s, k_SetupPath);
            return s;
        }

        /// <summary>단색·테두리 스프라이트를 만든다 (9-slice). 아트가 오기 전 자리 표시용</summary>
        private static Sprite EnsureGeneratedSprite(string path, int size, int border, bool hollow)
        {
            if (!File.Exists(path))
            {
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    bool edge = x < border || y < border || x >= size - border || y >= size - border;
                    tex.SetPixel(x, y, hollow && !edge ? Color.clear : Color.white);
                }
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
            }
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            var b = new Vector4(border + 1, border + 1, border + 1, border + 1);
            if (imp.textureType != TextureImporterType.Sprite || imp.spriteBorder != b)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.spriteBorder = hollow ? b : Vector4.zero;
                imp.mipmapEnabled = false;
                imp.alphaIsTransparency = true;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>비공개 아트(서브모듈)를 단일 스프라이트로. 없으면 null — 그림 없이 자리만 만든다</summary>
        private static Sprite PrepareArt(string path)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) { Debug.LogWarning($"[M2] 그림이 없다: {path} — 자리만 만든다"); return null; }
            if (imp.textureType != TextureImporterType.Sprite || imp.spriteImportMode != SpriteImportMode.Single || imp.maxTextureSize < 2048)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.mipmapEnabled = false;
                imp.maxTextureSize = 2048;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static GameObject EnsurePrefab(string name, Func<GameObject> build)
        {
            string path = $"{k_PrefabDir}/{name}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            var go = build();
            var asset = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return asset;
        }

        private static T Place<T>(GameObject prefab, Transform parent, string name = null) where T : Component
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = k_Mark + (name ?? prefab.name);
            return go.GetComponent<T>();
        }

        private static void RemoveGenerated(Scene scene)
        {
            var doomed = new List<GameObject>();
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name.StartsWith(k_Mark, StringComparison.Ordinal)) doomed.Add(t.gameObject);
            foreach (var go in doomed)
                if (go != null) Object.DestroyImmediate(go);
        }

        private static GameObject Root(Scene scene, string name)
        {
            foreach (var go in scene.GetRootGameObjects()) if (go.name == name) return go;
            throw new InvalidOperationException($"씬에 {name}이 없다 — BBGame/Setup/4. 경기 씬 골격");
        }

        private static Transform SafeArea(Scene scene, string canvas) => Root(scene, canvas).transform.Find("SafeArea");

        // ───────────────────────── 필드 ─────────────────────────

        /// <summary>배경 그림 + 타자 그림 (D16 a). 타자는 배경 안 비율 앵커로 — 배경이 커지면 따라간다</summary>
        private static FieldView BuildField(Transform canvas, Sprite fieldSprite, Sprite batterSprite)
        {
            var background = canvas.Find("Background");
            var bgImage = background.GetComponent<Image>();
            if (fieldSprite != null)
            {
                bgImage.sprite = fieldSprite;
                bgImage.color = Color.white;
            }

            var batter = NewUi(k_Mark + "Batter", background);
            var img = batter.gameObject.AddComponent<Image>();
            img.sprite = batterSprite;
            img.raycastTarget = false;
            img.enabled = batterSprite != null;
            batter.pivot = new Vector2(0.5f, 0.5f);

            var fieldGo = new GameObject(k_Mark + "Field", typeof(FieldView));
            fieldGo.transform.SetParent(canvas, false);
            var field = fieldGo.GetComponent<FieldView>();
            field._batter = batter;
            field._rightBox = Ratio(k_RightBatter);
            field._leftBox = Ratio(k_LeftBatter);
            field.SetBatterHand(Hand.Right);
            return field;
        }

        private static Vector4 Ratio(Rect px) => new Vector4(px.xMin / W, 1f - px.yMax / H, px.xMax / W, 1f - px.yMin / H);

        // ───────────────────────── 상단 줄 ─────────────────────────

        private static GameObject BuildScoreboard()
        {
            var root = Panel("Scoreboard", null, Anchor.TopLeft, 60, 35, 637, 130);
            var v = root.gameObject.AddComponent<ScoreboardView>();
            for (int i = 0; i < 9; i++) MakeText(root, "Inning" + (i + 1), 112 + i * 28, 10, 28, 22, 15, s_pal.textSub, TextAlignmentOptions.Center, false, (i + 1).ToString());
            MakeLabel(root, "R", 372, 10, 30, 22, 15, s_pal.intentText, TextAlignmentOptions.Center, true, s_cat.Ui.runsHeader);
            MakeLabel(root, "H", 402, 10, 30, 22, 15, s_pal.intentText, TextAlignmentOptions.Center, true, s_cat.Ui.hitsHeader);
            MakeLabel(root, "E", 432, 10, 30, 22, 15, s_pal.intentText, TextAlignmentOptions.Center, true, s_cat.Ui.errorsHeader);
            v._awayName = MakeText(root, "AwayName", 12, 42, 100, 30, 18, s_pal.text, TextAlignmentOptions.MidlineLeft, true, "솔뫼공고");
            v._homeName = MakeText(root, "HomeName", 12, 82, 100, 30, 18, s_pal.text, TextAlignmentOptions.MidlineLeft, true, "해신고");
            v._awayInnings = new TMP_Text[9];
            v._homeInnings = new TMP_Text[9];
            for (int i = 0; i < 9; i++)
            {
                v._awayInnings[i] = MakeText(root, "Away" + (i + 1), 112 + i * 28, 42, 28, 30, 19, s_pal.text, TextAlignmentOptions.Center, true, "0");
                v._homeInnings[i] = MakeText(root, "Home" + (i + 1), 112 + i * 28, 82, 28, 30, 19, s_pal.text, TextAlignmentOptions.Center, true, "");
            }
            v._awayR = MakeText(root, "AwayR", 372, 42, 30, 30, 19, s_pal.text, TextAlignmentOptions.Center, true, "0");
            v._awayH = MakeText(root, "AwayH", 402, 42, 30, 30, 19, s_pal.text, TextAlignmentOptions.Center, true, "0");
            v._awayE = MakeText(root, "AwayE", 432, 42, 30, 30, 19, s_pal.text, TextAlignmentOptions.Center, true, "0");
            v._homeR = MakeText(root, "HomeR", 372, 82, 30, 30, 19, s_pal.text, TextAlignmentOptions.Center, true, "0");
            v._homeH = MakeText(root, "HomeH", 402, 82, 30, 30, 19, s_pal.text, TextAlignmentOptions.Center, true, "0");
            v._homeE = MakeText(root, "HomeE", 432, 82, 30, 30, 19, s_pal.text, TextAlignmentOptions.Center, true, "0");
            Img(root, "Divider1", 470, 14, 2, 102, s_pal.panelBorder, s_white);

            var labels = new[] { s_cat.Ui.ballsLabel, s_cat.Ui.strikesLabel, s_cat.Ui.outsLabel };
            var counts = new[] { 3, 2, 2 };
            var pips = new Image[3][];
            for (int k = 0; k < 3; k++)
            {
                MakeLabel(root, "Label" + k, 482, 14 + k * 36, 20, 28, 15, s_pal.textSub, TextAlignmentOptions.Center, true, labels[k]);
                pips[k] = new Image[counts[k]];
                for (int m = 0; m < counts[k]; m++)
                    pips[k][m] = Img(root, $"Pip{k}_{m}", 506 + m * 22, 19 + k * 36, 17, 17, s_pal.pipOff, s_knob);
            }
            v._ballPips = pips[0];
            v._strikePips = pips[1];
            v._outPips = pips[2];
            Img(root, "Divider2", 576, 14, 2, 102, s_pal.panelBorder, s_white);

            // 주자 — 전광판 관례: 오른쪽 1루 · 위 2루 · 왼쪽 3루 (Q3)
            v._bases = new[]
            {
                Diamond(root, "Base1", 620, 70),
                Diamond(root, "Base2", 604, 52),
                Diamond(root, "Base3", 588, 70),
            };
            return root.gameObject;
        }

        private static Image Diamond(RectTransform parent, string name, float cx, float cy)
        {
            var rt = NewUi(name, parent);
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(15, 15);
            rt.anchoredPosition = new Vector2(cx, -cy);
            rt.localRotation = Quaternion.Euler(0, 0, 45);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = s_white;
            img.color = s_pal.baseOff;
            img.raycastTarget = false;
            var outline = rt.gameObject.AddComponent<Outline>();
            outline.effectColor = s_pal.warnText;
            outline.effectDistance = new Vector2(1, -1);
            return img;
        }

        private static GameObject BuildPitchCount()
        {
            var root = Panel("PitchCount", null, Anchor.Top, 722, 35, 270, 130);
            var v = root.gameObject.AddComponent<PitchCountView>();
            MakeLabel(root, "Title", 18, 12, 200, 26, 18, s_pal.text, TextAlignmentOptions.MidlineLeft, true, s_cat.Ui.pitchTitle);
            v._count = MakeText(root, "Count", 18, 40, 200, 46, 36, s_pal.text, TextAlignmentOptions.MidlineLeft, true, "0");
            v._rest = MakeText(root, "Rest", 18, 88, 240, 34, 13.5f, s_pal.textSub, TextAlignmentOptions.TopLeft, false, "휴식 0일 · 31구 더 던지면 1일");
            v._warnBar = Img(root, "Warn", 8, 122, 254, 4, s_pal.warn, s_white);
            return root.gameObject;
        }

        private static GameObject BuildToday()
        {
            var root = Panel("TodayChips", null, Anchor.Top, 1008, 35, 237, 130);
            var v = root.gameObject.AddComponent<TodayChipsView>();
            MakeLabel(root, "Title", 16, 12, 200, 26, 18, s_pal.text, TextAlignmentOptions.MidlineLeft, true, s_cat.Ui.todayTitle);
            v._chips = new Image[6];
            v._labels = new TMP_Text[6];
            for (int i = 0; i < 6; i++)
            {
                int c = i % 2, r = i / 2;
                var chip = Img(root, "Chip" + i, 14 + c * 106, 44 + r * 28, 100, 24, s_pal.panelBorder, s_round);
                chip.type = Image.Type.Sliced;
                v._chips[i] = chip;
                v._labels[i] = MakeText((RectTransform)chip.transform, "Text", 2, 0, 96, 24, 12, s_pal.text, TextAlignmentOptions.Center, false, "");
                chip.gameObject.SetActive(i == 0);
            }
            v._labels[0].text = "주심";
            return root.gameObject;
        }

        private static GameObject BuildResourceBar()
        {
            var root = Panel("ResourceBar", null, Anchor.TopRight, 1263, 35, 215, 110);
            var v = root.gameObject.AddComponent<ResourceBarView>();
            MakeLabel(root, "Title", 14, 10, 180, 24, 17, s_pal.text, TextAlignmentOptions.MidlineLeft, true, s_cat.Ui.staminaTitle);
            v._value = MakeText(root, "Value", 14, 34, 186, 34, 26, s_pal.text, TextAlignmentOptions.MidlineLeft, true, "90 / 90");
            Img(root, "Track", 14, 72, 187, 10, s_pal.barTrack, s_white);
            v._fill = Img(root, "Fill", 14, 72, 187, 10, s_pal.stamina, s_white);
            v._fill.type = Image.Type.Filled;
            v._fill.fillMethod = Image.FillMethod.Horizontal;
            v._fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            v._sub = MakeText(root, "Sub", 14, 86, 190, 20, 13, s_pal.textSub, TextAlignmentOptions.MidlineLeft, false, "구위 유지");
            return root.gameObject;
        }

        // ───────────────────────── 타자 패널 ─────────────────────────

        private static GameObject BuildBatterPanel()
        {
            var root = Panel("BatterPanel", null, Anchor.TopLeft, 60, 222, 256, 383);
            var v = root.gameObject.AddComponent<BatterPanelView>();
            var bar = Img(root, "HeaderBar", 8, 8, 240, 46, s_pal.accent, s_round);
            bar.type = Image.Type.Sliced;
            v._header = MakeText(root, "Header", 18, 8, 224, 46, 23, s_pal.textOnAccent, TextAlignmentOptions.MidlineLeft, true, "1번 노승재");
            v._kindChip = Img(root, "KindChip", 10, 64, 74, 24, s_pal.intent, s_round);
            v._kindChip.type = Image.Type.Sliced;
            v._kind = MakeText((RectTransform)v._kindChip.transform, "Kind", 0, 0, 74, 24, 12.5f, s_pal.textOnAccent, TextAlignmentOptions.Center, true, "작전형");
            v._info = MakeText(root, "Info", 90, 64, 160, 24, 12, s_pal.textSub, TextAlignmentOptions.MidlineLeft, false, "3학년 · 2루수 · 우타 · 첫 번째 타석");
            v._season = MakeText(root, "Season", 10, 94, 236, 20, 13, s_pal.text, TextAlignmentOptions.MidlineLeft, false, "시즌 .250 · 0홈런 · 3타점");

            var persona = NewUi("PersonaRow", root);
            SetRect(persona, 10, 118, 236, 20);
            MakeLabel(persona, "Label", 0, 0, 40, 20, 13, s_pal.text, TextAlignmentOptions.MidlineLeft, true, s_cat.Ui.personaLabel);
            v._persona = MakeText(persona, "Value", 44, 0, 192, 20, 13, s_pal.textDim, TextAlignmentOptions.MidlineLeft, false, "미획득");
            v._personaRow = persona.gameObject;

            var feature = NewUi("FeatureRow", root);
            SetRect(feature, 10, 140, 236, 46);
            MakeLabel(feature, "Label", 0, 0, 40, 20, 13, s_pal.text, TextAlignmentOptions.MidlineLeft, true, s_cat.Ui.featureLabel);
            v._feature = MakeText(feature, "Value", 44, 1, 192, 46, 12.5f, s_pal.textDim, TextAlignmentOptions.TopLeft, false, "??? — 이번 타석이 끝나면 열린다");

            var scout = Img(root, "ScoutRow", 10, 188, 236, 52, s_pal.barTrack, s_round);
            scout.type = Image.Type.Sliced;
            MakeLabel((RectTransform)scout.transform, "Title", 8, 4, 220, 20, 12, s_pal.text, TextAlignmentOptions.MidlineLeft, true, s_cat.Ui.scoutTitle);
            v._scoutRow = scout.gameObject;

            var g = NewUi("Gauge", root);
            SetRect(g, 10, 244, 236, 132);
            var gauge = g.gameObject.AddComponent<GaugeView>();
            MakeLabel(g, "Title", 0, 0, 200, 20, 14, s_pal.text, TextAlignmentOptions.MidlineLeft, true, s_cat.Ui.gaugeTitle);
            gauge._value = MakeText(g, "Value", 0, 20, 110, 42, 34, s_pal.text, TextAlignmentOptions.MidlineLeft, true, "0");
            gauge._target = MakeText(g, "Target", 96, 38, 140, 20, 12, s_pal.textSub, TextAlignmentOptions.MidlineRight, false, "목표 56 · 압도 112");
            var track = Img(g, "Track", 0, 66, 236, 12, s_pal.barTrack, s_white);
            gauge._fill = Img(g, "Fill", 0, 66, 236, 12, s_pal.warn, s_white);
            gauge._fill.type = Image.Type.Filled;
            gauge._fill.fillMethod = Image.FillMethod.Horizontal;
            gauge._fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            gauge._fill.fillAmount = 0f;
            gauge._markT = Marker((RectTransform)track.transform, "MarkT", s_pal.text, 0.667f);
            gauge._mark2T = Marker((RectTransform)track.transform, "Mark2T", s_pal.goldBorder, 1f);
            gauge._status = MakeText(g, "Status", 0, 84, 236, 18, 12, s_pal.warnText, TextAlignmentOptions.MidlineLeft, false, "목표 미달 — 2S에서도 커트, 맞으면 위험");
            gauge._notes = MakeText(g, "Notes", 0, 104, 236, 28, 10.5f, s_pal.textDim, TextAlignmentOptions.TopLeft, false, "");
            v._gauge = gauge;
            return root.gameObject;
        }

        private static RectTransform Marker(RectTransform track, string name, Color color, float x)
        {
            var rt = NewUi(name, track);
            rt.anchorMin = new Vector2(x, 0);
            rt.anchorMax = new Vector2(x, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(3, 6);
            rt.anchoredPosition = Vector2.zero;
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = s_white;
            img.color = color;
            img.raycastTarget = false;
            return rt;
        }

        // ───────────────────────── 존 ─────────────────────────

        /// <summary>
        /// 투수 시점 17분할 (레퍼런스 x 976~1326, y 292~722). 바깥 고리 위·아래는 "모서리 | 가운데 넓은 칸 | 모서리", 좌우는 세로로 긴 칸.
        /// 칸 이름은 화면 기준 — In = 화면 왼쪽
        /// </summary>
        private static GameObject BuildStrikeZone()
        {
            var root = NewUi("StrikeZone", null);
            SetRect(root, Anchor.Center, 976, 292, 350, 430);
            var v = root.gameObject.AddComponent<StrikeZoneView>();
            const float ringL = 52, ringR = 50, ringT = 56, ringB = 54, innerW = 350 - ringL - ringR, innerH = 430 - ringT - ringB;
            float cw = innerW / 3f, ch = innerH / 3f;
            v._cells = new ZoneCellView[Zone.Count];
            for (int i = 0; i < Zone.Count; i++)
            {
                var id = (ZoneId)i;
                var z = new Zone(id);
                float x, y, w, h;
                int col = (int)z.Col, row = (int)z.Row;
                if (!z.Outside) { x = ringL + col * cw; y = ringT + row * ch; w = cw; h = ch; }
                else
                {
                    x = col == 0 ? 0 : col == 1 ? ringL : ringL + innerW;
                    w = col == 0 ? ringL : col == 1 ? innerW : ringR;
                    y = row == 0 ? 0 : row == 1 ? ringT : ringT + innerH;
                    h = row == 0 ? ringT : row == 1 ? innerH : ringB;
                }
                v._cells[i] = ZoneCell(root, v, id, x, y, w, h, z.Outside);
            }
            v._boxLabelRight = MakeText(root, "BoxRight", 356, 196, 80, 24, 15, Color.white, TextAlignmentOptions.MidlineLeft, true, "우타");
            v._boxLabelLeft = MakeText(root, "BoxLeft", -86, 196, 80, 24, 15, Color.white, TextAlignmentOptions.MidlineRight, true, "좌타");
            v._boxLabelLeft.gameObject.SetActive(false);
            return root.gameObject;
        }

        private static ZoneCellView ZoneCell(RectTransform parent, StrikeZoneView owner, ZoneId id, float x, float y, float w, float h, bool outside)
        {
            var rt = NewUi("Cell_" + id, parent);
            SetRect(rt, x, y, w, h);
            var cell = rt.gameObject.AddComponent<ZoneCellView>();
            cell._owner = owner;
            cell._visual = id;
            cell._fill = Img(rt, "Fill", 1, 1, w - 2, h - 2, outside ? s_pal.zoneIdle : s_pal.zoneInsideIdle, s_white);
            cell._fill.raycastTarget = true;   // 누르기·놓기를 받는다 (투명해도 레이캐스트는 잡힌다)
            cell._frame = Img(rt, "Frame", 0, 0, w, h, outside ? s_pal.zoneRingLine : s_pal.zoneLine, s_frame);
            cell._frame.type = Image.Type.Sliced;
            cell._count = MakeText(rt, "Count", 0, 0, w, h, 26, s_pal.zoneCountText, TextAlignmentOptions.Center, true, "");
            return cell;
        }

        // ───────────────────────── 손패 ─────────────────────────

        /// <summary>카드 한 장 (140×197) — 등급 배지 · 구종 태그 · 이름 · 코스 · 특수 효과 · 구위·스태</summary>
        private static GameObject BuildCard()
        {
            var root = NewUi("Card", null);
            root.sizeDelta = new Vector2(140, 197);
            var v = root.gameObject.AddComponent<CardView>();
            v._group = root.gameObject.AddComponent<CanvasGroup>();
            var content = NewUi("Content", root);
            content.anchorMin = Vector2.zero;
            content.anchorMax = Vector2.one;
            content.offsetMin = content.offsetMax = Vector2.zero;
            v._content = content;
            v._background = Img(content, "Background", 0, 0, 140, 197, s_pal.card, s_round, true);
            v._background.type = Image.Type.Sliced;
            v._background.raycastTarget = true;   // 누르기·끌기를 받는다
            v._frame = Img(content, "Frame", 0, 0, 140, 197, s_pal.panelBorder, s_frame, true);
            v._frame.type = Image.Type.Sliced;
            v._gradeBadge = Img(content, "GradeBadge", 8, 8, 26, 26, s_pal.gradeN, s_round);
            v._gradeBadge.type = Image.Type.Sliced;
            v._grade = MakeText((RectTransform)v._gradeBadge.transform, "Grade", 0, 0, 26, 26, 15, Color.white, TextAlignmentOptions.Center, true, "N");
            v._type = MakeText(content, "Type", 40, 10, 92, 20, 11, s_pal.gradeN, TextAlignmentOptions.MidlineRight, true, "직구");
            v._name = MakeText(content, "Name", 8, 38, 124, 44, 15, s_pal.text, TextAlignmentOptions.TopLeft, true, "몸쪽 높은 직구");
            v._course = MakeText(content, "Course", 8, 84, 124, 34, 12, s_pal.textSub, TextAlignmentOptions.TopLeft, false, "몸쪽 높게");
            v._special = MakeText(content, "Special", 8, 120, 124, 48, 11, s_pal.warnText, TextAlignmentOptions.TopLeft, false, "");
            v._stats = MakeText(content, "Stats", 8, 172, 124, 18, 12, s_pal.text, TextAlignmentOptions.MidlineLeft, false, "구위 10 · 스태 1");
            return root.gameObject;
        }

        /// <summary>손패 8장 (7 + 「넓은 손패」 1) — 가로 줄. 카드는 Card 프리팹의 인스턴스</summary>
        private static GameObject BuildHand(GameObject cardPrefab)
        {
            var root = NewUi("Hand", null);
            SetRect(root, Anchor.Bottom, 440, 848, 1038, 197);
            var layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            var v = root.gameObject.AddComponent<HandView>();
            v._cards = new CardView[8];
            for (int i = 0; i < 8; i++)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(cardPrefab, root);
                go.name = "Card" + (i + 1);
                var card = go.GetComponent<CardView>();
                card._owner = v;
                card._index = i;
                Record(card);
                v._cards[i] = card;
            }
            return root.gameObject;
        }

        private static GameObject BuildActionBar()
        {
            var root = NewUi("ActionBar", null);
            SetRect(root, Anchor.Stretch, 0, 0, W, H);
            var v = root.gameObject.AddComponent<ActionBarView>();
            v._time = MakeButton(root, "Time", Anchor.Bottom, 248, 850, 166, 55, s_cat.Ui.timeButton, 20);
            v._intentionalWalk = MakeButton(root, "IntentionalWalk", Anchor.Bottom, 1505, 855, 167, 55, s_cat.Ui.ibbButton, 18);
            v._sign = MakeButton(root, "Sign", Anchor.Bottom, 1505, 923, 167, 55, s_cat.Ui.signButton, 18);
            return root.gameObject;
        }

        /// <summary>[던진다] — 임시 자리: 존 아래 오른쪽 (F5). 왼쪽에 미리보기 한 줄</summary>
        private static GameObject BuildThrowButton()
        {
            var root = NewUi("ThrowButton", null);
            SetRect(root, Anchor.Center, 840, 730, 490, 60);
            var v = root.gameObject.AddComponent<ThrowButtonView>();
            v._preview = MakeText(root, "Preview", 0, 4, 330, 32, 14, Color.white, TextAlignmentOptions.MidlineRight, true, "구위 10 × 1 = 기본 제압치 10 × ?");
            v._cost = MakeText(root, "Cost", 0, 36, 330, 20, 12, Color.white, TextAlignmentOptions.MidlineRight, false, "스태미나 −1");
            AddShadow(v._preview);
            AddShadow(v._cost);
            var holder = NewUi("ButtonHolder", root);
            SetRect(holder, 340, 4, 150, 52);
            v._button = MakeButton(holder, "Button", Anchor.TopLeft, 0, 0, 150, 52, s_cat.Ui.throwButton, 21, local: true);
            return root.gameObject;
        }

        // ───────────────────────── 리빌 · 안내 · 고스트 ─────────────────────────

        /// <summary>간이 리빌 — 손패 자리(위로 펼침, 존은 가리지 않는다 D12). 위에서 아래로 §20-2 순서</summary>
        private static GameObject BuildReveal()
        {
            var root = Panel("Reveal", null, Anchor.Bottom, 440, 730, 1038, 315);
            var v = root.gameObject.AddComponent<SimpleRevealView>();
            v._verdict = MakeText(root, "1_Verdict", 0, 6, 1038, 46, 40, s_pal.read, TextAlignmentOptions.Center, true, "찔렀다");
            MakeLabel(root, "2_IntentLabel", 40, 58, 120, 26, 14, s_pal.textSub, TextAlignmentOptions.MidlineLeft, true, s_cat.Ui.intentLabel);
            v._intentName = MakeText(root, "2_IntentName", 160, 56, 260, 30, 20, s_pal.intentText, TextAlignmentOptions.MidlineLeft, true, "직구 노림");
            v._stabLine = MakeText(root, "2_StabLine", 430, 58, 580, 26, 15, s_pal.readText, TextAlignmentOptions.MidlineLeft, false, "찔렀다 — 노림 때문에 손해 보는 공");
            v._switchNote = MakeText(root, "2_SwitchNote", 40, 86, 960, 20, 13, s_pal.warnText, TextAlignmentOptions.MidlineLeft, false, "");
            v._gainLine = MakeText(root, "3_GainLine", 0, 108, 1038, 36, 28, s_pal.text, TextAlignmentOptions.Center, true, "10 × 1.5 = +15");
            v._gaugeLine = MakeText(root, "3_GaugeLine", 0, 146, 1038, 22, 15, s_pal.textSub, TextAlignmentOptions.Center, false, "게이지 0 → 15 / 목표 56");
            v._resultLine = MakeText(root, "4_ResultLine", 0, 172, 1038, 26, 18, s_pal.text, TextAlignmentOptions.Center, false, "몸쪽 높은 직구 · 139km/h · 몸쪽 높게 · 0-0");
            v._tag = MakeText(root, "4_Tag", 0, 200, 1038, 30, 22, s_pal.readText, TextAlignmentOptions.Center, true, "헛스윙");
            v._notes = MakeText(root, "5_Notes", 40, 234, 800, 76, 12.5f, s_pal.textSub, TextAlignmentOptions.TopLeft, false, "");
            var holder = NewUi("6_NextHolder", root);
            SetRect(holder, 858, 252, 160, 50);
            v._next = MakeButton(holder, "Next", Anchor.TopLeft, 0, 0, 160, 50, s_cat.Ui.nextPitch, 18, local: true);
            v._nextLabel = v._next.GetComponentInChildren<TMP_Text>();
            Object.DestroyImmediate(v._nextLabel.GetComponent<LocalizedLabel>());   // 다음 공 / 다음 타자 — Presenter가 정한다
            return root.gameObject;
        }

        private static GameObject BuildBanner()
        {
            var root = NewUi("Banner", null);
            SetRect(root, Anchor.Stretch, 0, 0, W, H);
            var dim = root.gameObject.AddComponent<Image>();
            dim.sprite = s_white;
            dim.color = new Color(0, 0, 0, 0.35f);
            dim.raycastTarget = true;   // 뒤쪽 입력을 막는다
            var v = root.gameObject.AddComponent<MatchBannerView>();
            var panel = Panel("Panel", root, Anchor.Center, 660, 390, 600, 260);
            v._title = MakeText(panel, "Title", 0, 26, 600, 44, 32, s_pal.text, TextAlignmentOptions.Center, true, "1회초 종료");
            v._body = MakeText(panel, "Body", 40, 84, 520, 90, 17, s_pal.textSub, TextAlignmentOptions.Top, false, "");
            var holder = NewUi("ButtonHolder", panel);
            SetRect(holder, 220, 188, 160, 52);
            v._button = MakeButton(holder, "Button", Anchor.TopLeft, 0, 0, 160, 52, s_cat.Ui.nextInning, 18, local: true);
            v._buttonLabel = v._button.GetComponentInChildren<TMP_Text>();
            Object.DestroyImmediate(v._buttonLabel.GetComponent<LocalizedLabel>());   // 상황마다 다르다 — Presenter가 정한다
            return root.gameObject;
        }

        /// <summary>이닝 보너스 3택1 (§13) — 카드 3장, 진화는 골드 테두리. 화면 가운데</summary>
        private static GameObject BuildBonusPick()
        {
            var root = NewUi("BonusPick", null);
            SetRect(root, Anchor.Stretch, 0, 0, W, H);
            var dim = root.gameObject.AddComponent<Image>();
            dim.sprite = s_white;
            dim.color = new Color(0, 0, 0, 0.35f);
            dim.raycastTarget = true;
            var v = root.gameObject.AddComponent<BonusPickView>();
            var panel = Panel("Panel", root, Anchor.Center, 540, 300, 840, 420);
            MakeLabel(panel, "Title", 0, 22, 840, 40, 26, s_pal.text, TextAlignmentOptions.Center, true, s_cat.Ui.bonusTitle);
            v._choices = new Button[3];
            v._frames = new Image[3];
            v._names = new TMP_Text[3];
            v._levels = new TMP_Text[3];
            v._effects = new TMP_Text[3];
            v._notes = new TMP_Text[3];
            for (int i = 0; i < 3; i++)
            {
                var card = NewUi("Choice" + (i + 1), panel);
                SetRect(card, 30 + i * 268, 80, 244, 300);
                var bg = card.gameObject.AddComponent<Image>();
                bg.sprite = s_round;
                bg.type = Image.Type.Sliced;
                bg.color = s_pal.card;
                var button = card.gameObject.AddComponent<Button>();
                button.targetGraphic = bg;
                var nav = button.navigation;
                nav.mode = Navigation.Mode.None;
                button.navigation = nav;
                v._choices[i] = button;
                v._frames[i] = Img(card, "Frame", 0, 0, 244, 300, s_pal.panelBorder, s_frame, true);
                v._frames[i].type = Image.Type.Sliced;
                MakeText(card, "Key", 14, 12, 30, 24, 14, s_pal.textDim, TextAlignmentOptions.MidlineLeft, true, (i + 1).ToString());
                v._levels[i] = MakeText(card, "Level", 50, 12, 180, 24, 14, s_pal.accent, TextAlignmentOptions.MidlineRight, true, "Lv 0 → 1");
                v._names[i] = MakeText(card, "Name", 14, 52, 216, 60, 22, s_pal.text, TextAlignmentOptions.TopLeft, true, "묵직한 직구");
                v._effects[i] = MakeText(card, "Effect", 14, 120, 216, 100, 16, s_pal.textSub, TextAlignmentOptions.TopLeft, false, "직구 구위 +3");
                v._notes[i] = MakeText(card, "Note", 14, 228, 216, 56, 13, s_pal.goldBorder, TextAlignmentOptions.TopLeft, false, "");
            }
            return root.gameObject;
        }

        /// <summary>경기 결과 (간이) — 승·패·무, 점수, 투수 기록 한 줄, 구원 한 줄, [처음부터]</summary>
        private static GameObject BuildResult()
        {
            var root = NewUi("MatchResult", null);
            SetRect(root, Anchor.Stretch, 0, 0, W, H);
            var dim = root.gameObject.AddComponent<Image>();
            dim.sprite = s_white;
            dim.color = new Color(0, 0, 0, 0.45f);
            dim.raycastTarget = true;
            var v = root.gameObject.AddComponent<MatchResultView>();
            var panel = Panel("Panel", root, Anchor.Center, 610, 320, 700, 380);
            v._title = MakeText(panel, "Title", 0, 28, 700, 60, 48, s_pal.readText, TextAlignmentOptions.Center, true, "승리");
            v._score = MakeText(panel, "Score", 0, 100, 700, 40, 26, s_pal.text, TextAlignmentOptions.Center, true, "솔뫼공고 2 : 3 해신고");
            v._pitcher = MakeText(panel, "Pitcher", 40, 160, 620, 50, 16, s_pal.textSub, TextAlignmentOptions.Top, false, "9이닝 · 120구 · 피안타 6 · 실점 2 (자책 2) · 삼진 9 · 4사구 3");
            v._relief = MakeText(panel, "Relief", 40, 214, 620, 40, 15, s_pal.warnText, TextAlignmentOptions.Top, false, "");
            var holder = NewUi("ButtonHolder", panel);
            SetRect(holder, 270, 296, 160, 52);
            v._button = MakeButton(holder, "Button", Anchor.TopLeft, 0, 0, 160, 52, s_cat.Ui.restart, 18, local: true);
            v._buttonLabel = v._button.GetComponentInChildren<TMP_Text>();
            Object.DestroyImmediate(v._buttonLabel.GetComponent<LocalizedLabel>());   // Presenter가 정한다
            return root.gameObject;
        }

        private static GameObject BuildGhost()
        {
            var root = NewUi("DragGhost", null);
            SetRect(root, Anchor.Stretch, 0, 0, W, H);
            root.pivot = new Vector2(0.5f, 0.5f);
            var v = root.gameObject.AddComponent<DragGhostView>();
            v._group = root.gameObject.AddComponent<CanvasGroup>();
            v._group.blocksRaycasts = false;
            v._group.interactable = false;
            var ghost = NewUi("Ghost", root);
            ghost.anchorMin = ghost.anchorMax = ghost.pivot = new Vector2(0.5f, 0.5f);
            ghost.sizeDelta = new Vector2(140, 84);
            var bg = ghost.gameObject.AddComponent<Image>();
            bg.sprite = s_round;
            bg.type = Image.Type.Sliced;
            bg.color = new Color(1, 1, 1, 0.9f);
            bg.raycastTarget = false;
            v._name = MakeText(ghost, "Name", 8, 8, 124, 68, 15, s_pal.text, TextAlignmentOptions.Center, true, "");
            v._root = ghost;
            ghost.gameObject.SetActive(false);
            return root.gameObject;
        }

        // ───────────────────────── 만들기 도우미 ─────────────────────────

        /// <summary>프리팹 인스턴스를 코드로 고친 뒤 — 기록하지 않으면 씬을 다시 열 때 프리팹 값으로 돌아간다</summary>
        private static void Record(Object o) => PrefabUtility.RecordPrefabInstancePropertyModifications(o);

        private static RectTransform NewUi(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;   // UI
            if (parent != null) go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>
        /// 레퍼런스 좌표(왼쪽 위 기준 x, y, 폭, 높이)를 앵커 기준 위치로. 기준점은 언제나 왼쪽 위(pivot 0,1)라
        /// 앵커만 바꾸면 같은 계산으로 붙는다
        /// </summary>
        private static void SetRect(RectTransform rt, Anchor a, float x, float y, float w, float h)
        {
            if (a == Anchor.Stretch)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                return;
            }
            var anchor = a switch
            {
                Anchor.TopLeft => new Vector2(0, 1),
                Anchor.Top => new Vector2(0.5f, 1),
                Anchor.TopRight => new Vector2(1, 1),
                Anchor.Bottom => new Vector2(0.5f, 0),
                _ => new Vector2(0.5f, 0.5f),
            };
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0, 1);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x - anchor.x * W, (1 - anchor.y) * H - y);
        }

        /// <summary>부모 왼쪽 위 기준 자리</summary>
        private static void SetRect(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, -y);
        }

        private static RectTransform Panel(string name, Transform parent, Anchor a, float x, float y, float w, float h)
        {
            var rt = NewUi(name, parent);
            SetRect(rt, a, x, y, w, h);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = s_round;
            img.type = Image.Type.Sliced;
            img.color = s_pal.panel;
            img.raycastTarget = true;   // 패널 뒤 필드·존을 누르지 않게
            var outline = rt.gameObject.AddComponent<Outline>();
            outline.effectColor = s_pal.panelBorder;
            outline.effectDistance = new Vector2(2, -2);
            return rt;
        }

        private static Image Img(RectTransform parent, string name, float x, float y, float w, float h, Color color, Sprite sprite, bool stretch = false)
        {
            var rt = NewUi(name, parent);
            if (stretch)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = rt.offsetMax = Vector2.zero;
            }
            else SetRect(rt, x, y, w, h);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static TextMeshProUGUI MakeText(RectTransform parent, string name, float x, float y, float w, float h, float size, Color color,
            TextAlignmentOptions align, bool bold, string sample)
        {
            var rt = NewUi(name, parent);
            SetRect(rt, x, y, w, h);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (bold && s_bold != null) t.font = s_bold;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            t.text = sample;   // 에디터 미리보기용 — 실행하면 Presenter·LocalizedLabel이 덮어쓴다
            return t;
        }

        /// <summary>고정 문구 라벨 — LocalizedLabel + 한국어 미리보기</summary>
        private static TextMeshProUGUI MakeLabel(RectTransform parent, string name, float x, float y, float w, float h, float size, Color color,
            TextAlignmentOptions align, bool bold, LocalizedString text)
        {
            var t = MakeText(parent, name, x, y, w, h, size, color, align, bold, string.Empty);
            t.gameObject.AddComponent<LocalizedLabel>();
            SetLabel(t, text);
            return t;
        }

        private static void SetLabel(TextMeshProUGUI t, LocalizedString text)
        {
            t.GetComponent<LocalizedLabel>().SetText(text);
            t.text = KoreanOf(text);
            EditorUtility.SetDirty(t.GetComponent<LocalizedLabel>());
        }

        private static Button MakeButton(RectTransform parent, string name, Anchor a, float x, float y, float w, float h, LocalizedString label, float size,
            bool local = false)
        {
            var rt = NewUi(name, parent);
            if (local) SetRect(rt, x, y, w, h);
            else SetRect(rt, a, x, y, w, h);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = s_round;
            img.type = Image.Type.Sliced;
            img.color = s_pal.accent;
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            var nav = b.navigation;
            nav.mode = Navigation.Mode.None;   // 방향키·Enter로 버튼 사이를 옮겨 다니지 않는다 (단축키는 Match 맵)
            b.navigation = nav;
            MakeLabel(rt, "Label", 0, 0, w, h, size, s_pal.textOnAccent, TextAlignmentOptions.Center, true, label);
            return b;
        }

        private static void AddShadow(TMP_Text t)
        {
            var s = t.gameObject.AddComponent<Shadow>();
            s.effectColor = new Color(0, 0, 0, 0.6f);
            s.effectDistance = new Vector2(1, -1);
        }

        /// <summary>에디터 미리보기용 한국어 — String Table에서 읽는다</summary>
        private static string KoreanOf(LocalizedString text)
        {
            if (text == null || text.IsEmpty) return string.Empty;
            var collection = LocalizationEditorSettings.GetStringTableCollection(text.TableReference);
            var table = collection != null ? collection.GetTable("ko") as StringTable : null;
            var entry = table != null ? table.GetEntry(text.TableEntryReference.KeyId) : null;
            return entry != null ? entry.Value : string.Empty;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
