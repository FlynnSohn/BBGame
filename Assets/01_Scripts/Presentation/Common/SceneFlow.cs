using UnityEngine;
using UnityEngine.SceneManagement;

namespace BBGame.Presentation.Common
{
    /// <summary>
    /// 씬 이동 (1차 빌드, build1_plan) — 타이틀 ↔ 경기, 게임 끝내기.
    /// 씬 이름은 빌드 목록(EditorBuildSettings)과 같아야 한다 — 타이틀 빌더 메뉴가 목록을 맞춘다
    /// </summary>
    public static class SceneFlow
    {
        public const string TitleScene = "Title";
        public const string MatchScene = "Match";

        /// <summary>[게임 시작] — 새 경기 (시드는 TestMatchSetup이 정한다)</summary>
        public static void StartMatch() => SceneManager.LoadScene(MatchScene);

        /// <summary>[타이틀로] — 경기 도중이든 결과 창이든 타이틀로 돌아간다</summary>
        public static void ToTitle() => SceneManager.LoadScene(TitleScene);

        /// <summary>[종료] — 실행 파일을 닫는다. 에디터에서는 Play를 끈다</summary>
        public static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
