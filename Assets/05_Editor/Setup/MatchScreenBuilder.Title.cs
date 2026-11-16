using System.Collections.Generic;
using System.Linq;
using BBGame.EditorTools.Text;
using BBGame.Presentation.Common;
using BBGame.Presentation.Data;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BBGame.EditorTools.Setup
{
    /// <summary>
    /// 타이틀 화면 (1차 빌드, build1_plan) — 배경 그림 한 장 + 제목 + [게임 시작] · [종료].
    /// 경기 화면 빌더의 버튼·라벨 도구를 같이 쓴다. 다시 실행하면 씬을 새로 만든다
    /// </summary>
    public static partial class MatchScreenBuilder
    {
        private const string k_TitleScene = "Assets/00_Scenes/Title.unity";

        [MenuItem("BBGame/Setup/8. 타이틀 화면 만들기", false, 8)]
        private static void BuildTitle()
        {
            if (StopIfPlaying()) return;
            s_cat = AssetDatabase.LoadAssetAtPath<DisplayCatalog>(StringImporter.CatalogPath);
            if (s_cat == null) { Debug.LogError("[Title] 표시용 데이터가 없다 — 먼저 BBGame/Data/문구 가져오기"); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            // 경기 화면 빌더와 같은 재료 (팔레트·스프라이트·굵은 글꼴)
            EnsureFolder(k_UiDir);
            s_pal = EnsureAsset<UiPalette>(k_PalettePath);
            s_white = EnsureGeneratedSprite(k_WhitePath, 4, 0, false);
            s_round = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            s_bold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(k_BoldFont);
            var background = PrepareArt(k_FieldArt);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 카메라 — 캔버스 뒤 바탕색만 칠한다
            var camGo = new GameObject("Main Camera", typeof(Camera));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = s_pal.button;
            cam.orthographic = true;

            // 입력 — 경기 씬과 같은 InputSystemUIInputModule + 프로젝트 전역 액션의 UI 맵
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var module = es.GetComponent<InputSystemUIInputModule>();
            var projectWide = UnityEngine.InputSystem.InputSystem.actions;
            if (projectWide != null && projectWide.FindActionMap("UI") != null) module.actionsAsset = projectWide;

            // 캔버스 — 1920×1080, 높이 기준 (decisions 2026-10-13)
            var canvasGo = new GameObject("Canvas_Title", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.layer = 5;
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(W, H);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
            var root = (RectTransform)canvasGo.transform;

            // 배경 그림 + 아래쪽을 어둡게 (글자가 읽히도록)
            var bg = NewUi("Background", root);
            SetRect(bg, Anchor.Stretch, 0, 0, W, H);
            var bgImg = bg.gameObject.AddComponent<Image>();
            bgImg.sprite = background;
            bgImg.color = background != null ? Color.white : s_pal.button;
            bgImg.raycastTarget = false;

            var dim = NewUi("Dim", root);
            SetRect(dim, Anchor.Stretch, 0, 0, W, H);
            var dimImg = dim.gameObject.AddComponent<Image>();
            dimImg.sprite = s_white;
            dimImg.color = new Color(s_pal.button.r * 0.4f, s_pal.button.g * 0.4f, s_pal.button.b * 0.4f, 0.45f);
            dimImg.raycastTarget = false;

            // 제목 판 — 가운데 위쪽, 차가운 흰 패널
            var panel = NewUi("TitlePanel", root);
            SetRect(panel, Anchor.Center, 560, 220, 800, 260);
            var panelImg = panel.gameObject.AddComponent<Image>();
            panelImg.sprite = s_round;
            panelImg.type = Image.Type.Sliced;
            panelImg.color = s_pal.panel;
            panelImg.raycastTarget = false;
            var outline = panel.gameObject.AddComponent<Outline>();
            outline.effectColor = s_pal.panelBorder;
            outline.effectDistance = new Vector2(3, -3);
            MakeLabel(panel, "Title", 0, 40, 800, 120, 96, s_pal.accent, TextAlignmentOptions.Center, true, s_cat.Ui.titleName);
            MakeLabel(panel, "Subtitle", 0, 160, 800, 60, 40, s_pal.text, TextAlignmentOptions.Center, true, s_cat.Ui.titleSub);

            // 버튼 두 개 — 남색 버튼 (경기 화면과 같은 모양)
            var start = MakeButton(root, "StartButton", Anchor.Center, 760, 600, 400, 90, s_cat.Ui.startButton, 34);
            start.gameObject.AddComponent<SceneButton>()._action = SceneButton.Action.StartMatch;
            var quit = MakeButton(root, "QuitButton", Anchor.Center, 760, 720, 400, 90, s_cat.Ui.quitButton, 34);
            quit.gameObject.AddComponent<SceneButton>()._action = SceneButton.Action.Quit;

            EditorSceneManager.SaveScene(scene, k_TitleScene);
            SetBuildScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[Title] 타이틀 화면 완료 — 빌드 목록: Title(0) · Match(1)");
        }

        /// <summary>빌드 목록 — 타이틀이 첫 칸, 그다음 경기. 다른 씬은 그대로 뒤에 둔다</summary>
        private static void SetBuildScenes()
        {
            var list = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(k_TitleScene, true),
                new EditorBuildSettingsScene(k_Scene, true),
            };
            list.AddRange(EditorBuildSettings.scenes.Where(s => s.path != k_TitleScene && s.path != k_Scene));
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
