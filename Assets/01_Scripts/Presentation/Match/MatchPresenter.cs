using System;
using System.Collections.Generic;
using System.Threading;
using BBGame.Core;
using BBGame.Presentation.Common;
using BBGame.Presentation.Data;
using Cysharp.Threading.Tasks;

namespace BBGame.Presentation.Match
{
    /// <summary>
    /// 경기 화면 Presenter (D4) — View 입력을 엔진 명령으로 바꾸고, 명령 뒤에 화면을 다시 그린다.
    /// 엔진 명령은 여기서만 부른다. 매 프레임 폴링하지 않는다 (갱신은 입력·명령 뒤에만).
    /// 파일 나눔: 이 파일(흐름·입력) · Render(전광판·타자·존·손패) · Reveal(간이 리빌)
    /// </summary>
    public sealed partial class MatchPresenter : IHandRules, IDisposable
    {
        private readonly MatchEngine _engine;
        private readonly GameDatabase _db;
        private readonly MatchSetup _setup;
        private readonly DisplayCatalog _cat;
        private readonly UiPalette _pal;
        private readonly MatchViews _v;
        private readonly MatchInputRouter _input;
        private readonly Action _restart;

        private readonly SelectionState _sel = new SelectionState();
        private readonly InputGate _gate = new InputGate();
        private readonly TextArgs _args = new TextArgs();
        private readonly List<AddChip> _chips = new List<AddChip>(8);

        private CancellationToken _ct;
        /// <summary>끌고 있는 손패 번호 (없으면 −1) — 끄는 동안 그 카드의 코스를 밝힌다</summary>
        private int _dragIndex = -1;

        private IMatchReadOnly S => _engine.State;
        private TuningConfig T => _db.Tuning;
        private UiTexts Ui => _cat.Ui;

        public MatchPresenter(MatchEngine engine, GameDatabase db, MatchSetup setup, DisplayCatalog catalog, UiPalette palette,
            MatchViews views, MatchInputRouter input, Action restart)
        {
            _engine = engine;
            _db = db;
            _setup = setup;
            _cat = catalog;
            _pal = palette;
            _v = views;
            _input = input;
            _restart = restart;
        }

        public void Start(CancellationToken ct)
        {
            _ct = ct;
            _v.hand.CardClicked += OnCardClicked;
            _v.hand.DragStarting += OnDragStarting;
            _v.hand.DragEnded += OnDragEnded;
            _v.zone.CellClicked += OnCellClicked;
            _v.zone.CardDropped += OnCardDropped;
            _v.throwButton.Clicked += OnThrowClicked;
            if (_input != null)
            {
                _input.CardKey += OnCardKey;
                _input.Confirm += OnConfirmKey;
                _input.Cancel += OnCancelKey;
            }

            // 단계 스위치로 칸을 켜고 끈다 (§6-3) — 0단계는 집중력·사인교환·성향·스카우팅이 없다
            var f = _engine.Features;
            _v.focus.gameObject.SetActive(f.focusOn);
            _v.actions.SetStage(f.sign);
            _v.batter.SetStageRows(f.persona, f.scoutInfo);

            _engine.Start();
            RefreshAll();
        }

        public void Dispose()
        {
            _v.hand.CardClicked -= OnCardClicked;
            _v.hand.DragStarting -= OnDragStarting;
            _v.hand.DragEnded -= OnDragEnded;
            _v.zone.CellClicked -= OnCellClicked;
            _v.zone.CardDropped -= OnCardDropped;
            _v.throwButton.Clicked -= OnThrowClicked;
            if (_input != null)
            {
                _input.CardKey -= OnCardKey;
                _input.Confirm -= OnConfirmKey;
                _input.Cancel -= OnCancelKey;
            }
        }

        // ───────────────────────── IHandRules (선택 규칙이 손패에 묻는 것) ─────────────────────────

        public int HandCount => S.Hand.Count;
        public bool IsPowerPitch(int handIndex) => S.Hand[handIndex].Def.IsPowerPitch;
        public bool CanTarget(int handIndex, ZoneId zone) => S.Hand[handIndex].Def.CanTarget(zone);
        public ZoneId FirstZone(int handIndex) => S.Hand[handIndex].Def.Zones[0];

        // ───────────────────────── 입력 ─────────────────────────

        private bool Accepting => !_gate.IsLocked && _engine.Phase == MatchPhase.Pitching;

        private void OnCardClicked(int handIndex)
        {
            if (!Accepting) return;
            _sel.ClickCard(handIndex, this);
            RefreshPlay();
        }

        private void OnCardKey(int handIndex) => OnCardClicked(handIndex);

        private void OnCellClicked(ZoneId visual)
        {
            if (!Accepting) return;
            _sel.ClickZone(PitcherView.ZoneOfVisual(visual, S.CurrentBatter.Def.Hand), this);
            RefreshPlay();
        }

        private void OnThrowClicked()
        {
            if (!Accepting || !_sel.HasCard) return;
            var cmd = _sel.ToCommand(this);
            if (_engine.CanThrow(cmd.HandIndex, cmd.Zone)) ThrowAsync(cmd).Forget();
        }

        /// <summary>Space·Enter — 리빌·안내 중이면 [다음], 아니면 던진다</summary>
        private void OnConfirmKey()
        {
            if (_v.reveal.IsShown) { _v.reveal.Next(); return; }
            if (_v.banner.IsShown) { _v.banner.Confirm(); return; }
            OnThrowClicked();
        }

        private void OnCancelKey()
        {
            if (!Accepting) return;
            _sel.Clear();
            RefreshPlay();
        }

        private bool OnDragStarting(int handIndex)
        {
            if (!Accepting || handIndex >= S.Hand.Count) return false;
            _dragIndex = handIndex;
            RenderZone();
            return true;
        }

        private void OnDragEnded(int handIndex)
        {
            _dragIndex = -1;
            if (!_gate.IsLocked) RefreshPlay();
        }

        /// <summary>
        /// 카드를 존 칸에 놓았다 (D11). 그 카드로 던질 수 있는 칸이면 바로 던지고, 아니면 아무 일도 없다(카드는 손패에 남는다).
        /// 전력투구 직구·가운데 보고 던지기는 존 안 아무 칸에 놓으면 던진다 — 착지는 엔진이 정한다
        /// </summary>
        private void OnCardDropped(ZoneId visual, int handIndex)
        {
            if (!Accepting || handIndex < 0 || handIndex >= S.Hand.Count) return;
            var zone = PitcherView.ZoneOfVisual(visual, S.CurrentBatter.Def.Hand);
            var def = S.Hand[handIndex].Def;
            ZoneId? target;
            if (def.IsPowerPitch || def.IsFixed)
            {
                if (new Zone(zone).Outside) return;
                target = def.IsPowerPitch ? (ZoneId?)null : def.Zones[0];
            }
            else target = zone;
            if (!_engine.CanThrow(handIndex, target)) return;
            ThrowAsync(new ThrowCommand(handIndex, target)).Forget();
        }

        // ───────────────────────── 던지기 → 리빌 → 다음 ─────────────────────────

        /// <summary>던지기 → 간이 리빌 → [다음] → 반 이닝·강판·경기 끝 안내 → 다시 그림. 리빌 동안 입력을 잠근다</summary>
        private async UniTaskVoid ThrowAsync(ThrowCommand cmd)
        {
            if (_gate.IsLocked) return;
            var r = _engine.Throw(cmd);   // Core가 상태를 모두 바꾼다. 결과 객체는 다음 Throw 전까지 유효하다
            if (r == null) return;
            _gate.Lock();
            _sel.Clear();
            _dragIndex = -1;
            _v.hand.CancelDrag();
            try
            {
                ShowReveal(r);
                await _v.reveal.WaitNextAsync(_ct);
                _v.reveal.Hide();
                await HandlePhaseAsync();
            }
            catch (OperationCanceledException)
            {
                return;   // 씬을 떠났다
            }
            _gate.Unlock();
            RefreshAll();
        }

        /// <summary>반 이닝 종료 → [다음 이닝] (M1 임시 명령) · 강판·경기 끝 → [처음부터]</summary>
        private async UniTask HandlePhaseAsync()
        {
            switch (_engine.Phase)
            {
                case MatchPhase.HalfInningOver:
                    {
                        string title = _args.Clear().Set("inning", S.Inning).Format(Ui.halfOverTitle);
                        await _v.banner.ShowAsync(title, Loc.Get(Ui.halfOverBody), Loc.Get(Ui.nextInning), _ct);
                        _engine.AdvanceHalfInning();
                        if (_engine.Phase == MatchPhase.Ended) await ShowEndAsync();
                        break;
                    }
                case MatchPhase.PitcherExited:
                    {
                        string body = _args.Clear().Set("pitcher", Loc.Get(Ui.pitcherName)).Set("pitches", S.Pitches).Format(_cat.Exit(S.ExitReason));
                        RefreshAll();
                        await _v.banner.ShowAsync(Loc.Get(Ui.exitTitle), body, Loc.Get(Ui.restart), _ct);
                        _restart?.Invoke();
                        break;
                    }
                case MatchPhase.Ended:
                    await ShowEndAsync();
                    break;
            }
        }

        private async UniTask ShowEndAsync()
        {
            RefreshAll();
            string body = _args.Clear()
                .Set("away", Loc.Get(_cat.SchoolName(_setup.Opponent.Id))).Set("awayRuns", S.ScoreOpp)
                .Set("home", Loc.Get(_cat.SchoolName(HomeSchoolId))).Set("homeRuns", S.ScoreUs)
                .Format(Ui.endBody);
            await _v.banner.ShowAsync(Loc.Get(Ui.endTitle), body, Loc.Get(Ui.restart), _ct);
            _restart?.Invoke();
        }

        /// <summary>주인공 학교 — 표시용 데이터 id</summary>
        private const string HomeSchoolId = "haeshin";
    }
}
