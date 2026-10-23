using System;
using BBGame.Core;
using BBGame.Presentation.Common;
using BBGame.Presentation.Data;
using BBGame.Presentation.Views;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;

namespace BBGame.Presentation.Match
{
    /// <summary>경기 화면 View 참조 묶음 — 에디터 빌더가 채운다</summary>
    [Serializable]
    public sealed class MatchViews
    {
        public FieldView field;
        public ScoreboardView scoreboard;
        public PitchCountView pitchCount;
        public TodayChipsView today;
        public ResourceBarView stamina;
        public ResourceBarView focus;
        public BatterPanelView batter;
        public StrikeZoneView zone;
        public HandView hand;
        public ActionBarView actions;
        public ThrowButtonView throwButton;
        public SimpleRevealView reveal;
        public MatchBannerView banner;
        public BonusPickView bonus;
        public MatchResultView result;
    }

    /// <summary>
    /// 경기 시작 — Localization 초기화를 기다린 뒤(테이블 Preload, decisions 2026-10-13) 엔진을 만들고 Presenter를 연결한다.
    /// 경기 전 화면(M8)이 생기기 전까지는 테스트 진입 설정(TestMatchSetup)으로 연다
    /// </summary>
    public sealed class MatchBootstrap : MonoBehaviour
    {
        [SerializeField] internal TestMatchSetup _setup;
        [SerializeField] internal DisplayCatalog _catalog;
        [SerializeField] internal UiPalette _palette;
        [SerializeField] internal MatchInputRouter _input;
        [SerializeField] internal MatchViews _views = new MatchViews();

        private MatchPresenter _presenter;

        private void Awake()
        {
            // 첫 화면이 그려지기 전에 리빌·안내·고스트를 닫아 둔다
            _views.reveal.Hide();
            _views.banner.gameObject.SetActive(false);
            if (_views.bonus != null) _views.bonus.gameObject.SetActive(false);
            if (_views.result != null) _views.result.gameObject.SetActive(false);
        }

        private async UniTaskVoid Start()
        {
            var ct = destroyCancellationToken;
            var init = LocalizationSettings.InitializationOperation;
            while (!init.IsDone) await UniTask.Yield(ct);
            KoreanJosaFormatter.Register();   // 중계 문구의 조사 {n:josa(이|가)}

            var db = _setup.Database;
            var setup = _setup.Build(out uint seed);
            Debug.Log($"[Match] 시드 {seed} · {setup.Opponent.Id} · {setup.Stage}단계 — 같은 경기를 다시 보려면 TestMatchSetup의 시드에 넣는다");

            var engine = new MatchEngine(db, setup);
            _presenter = new MatchPresenter(engine, db, setup, _catalog, _palette, _views, _input, Restart);
            _presenter.Start(ct);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_setup.AutoPlay) _presenter.RunAutoPlayAsync(_setup.AutoPlayDelay, ct).Forget();
#endif
        }

        private void OnDestroy() => _presenter?.Dispose();

        /// <summary>[처음부터] — 씬을 다시 연다 (M2 임시)</summary>
        private static void Restart() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
