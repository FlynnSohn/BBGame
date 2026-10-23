using System;
using System.Collections.Generic;

namespace BBGame.Core
{
    /// <summary>
    /// 경기 엔진 — 명령을 받아 상태를 바꾸고 결과를 돌려준다 (D1·D2). 동기·결정적, Unity 시간·난수를 쓰지 않는다.
    /// 처리 순서와 난수 호출 순서는 프로토타입 v12 엔진과 같다 (match_m1_core_plan 접근 4).
    /// 파일 나눔: 이 파일(생성·타석·노림) · Preview · Throw · Reaction · InPlay · Running · Inning(보너스·우리 공격·경기 끝) · Bullpen(강판 뒤)
    /// </summary>
    public sealed partial class MatchEngine
    {
        /// <summary>주인공 한이준 — 우완 (§11-2). 오버핸드는 설정만이다</summary>
        public const Hand PitcherHand = Hand.Right;

        private readonly GameDatabase _db;
        private readonly TuningConfig _t;
        private readonly StageFeatures _f;
        private readonly MatchState _s = new MatchState();
        private readonly PitchResult _result = new PitchResult();
        private readonly double[] _league = new double[6];
        private SeededRandom _rng;
        // 이번 공에서 실책 진루 주자가 득점한 수 (비자책) — 프로토타입 G._ue
        private int _unearned;

        /// <summary>현재 진행 단계 — 화면은 이 값으로 받을 수 있는 입력을 정한다</summary>
        public MatchPhase Phase => _s.Phase;

        /// <summary>읽기 전용 상태 (전광판·타자 패널·손패 표시용)</summary>
        public IMatchReadOnly State => _s;

        /// <summary>이 경기의 단계 기능 스위치</summary>
        public StageFeatures Features => _f;

        /// <summary>경기를 만든다. 데이터는 읽기만 하고 상태는 엔진이 소유한다. 덱 셔플까지 하고 Start를 기다린다</summary>
        public MatchEngine(GameDatabase db, MatchSetup setup)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            if (setup == null) throw new ArgumentNullException(nameof(setup));
            if (setup.Opponent == null) throw new ArgumentException("상대 학교가 없다", nameof(setup));
            _t = db.Tuning;
            _f = db.Stages[setup.Stage];
            _rng = new SeededRandom(setup.Seed);
            _s.Stage = setup.Stage;
            _s.Modifiers = setup.Modifiers;
            _s.Phase = MatchPhase.NotStarted;

            // 경기 단위 변동 굴리기 — 프로토타입 newGame과 같은 순서 (컨디션 → 날씨 → 불펜), 덱 셔플 전 (§12)
            if (setup.RollConditions) setup.Modifiers.Conditions = Pregame.RollConditions(_f, ref _rng);
            if (setup.RollWeather) setup.Modifiers.Weather = Pregame.RollWeather(_f, ref _rng);
            if (setup.RollBullpen) Pregame.RollBullpen(BullpenNames.Length, ref _rng, _s.BullpenList);
            else _s.BullpenList.AddRange(setup.Bullpen);

            int innings = _t.game.innings;
            _s.LineOpp = new int[innings];
            _s.LineUs = new int[innings];

            // 라인업 — 유형 목표치(컨택형은 contactT), 노림 기본 가중치(이름 시드)
            IntentWeights.League(db.BatterTypes, _league);
            _s.Lineup = new BatterState[9];
            for (int i = 0; i < 9; i++)
            {
                var def = setup.Opponent.Lineup[i];
                double baseT = def.Type.Kind == BatterKind.Contact && _t.target.contactT > 0 ? _t.target.contactT : def.Type.BaseTarget;
                var bs = new BatterState(def, i, baseT);
                IntentWeights.Base(_t.intent, def, _league, bs.BaseWeights);
                _s.Lineup[i] = bs;
            }

            // 스태미나·집중력·멘탈 (§11-2, §12-2)
            var mods = setup.Modifiers;
            int staMax = _t.stamina.staBase + setup.StaR;
            if (mods.Has(Condition.Heavy)) staMax = JsMath.RoundHalfUp(staMax * (1 - _t.stamina.condHeavyPct));
            _s.StaminaMax = staMax;
            _s.StaminaMaxAtStart = staMax;
            _s.FocusMax = _t.hand.focusMax + (mods.Has(Condition.Clear) ? _t.hand.clearFocus : 0);
            _s.Stamina = _s.StaminaMax;
            _s.Focus = _s.FocusMax;
            _s.Mental = _t.mental.start;

            // 덱 — 손끝 감각이 좋다면 오늘의 직구 추가, 셔플. 고정 카드는 덱 밖
            var deck = _s.Deck;
            foreach (var e in setup.Deck) deck.Draw.Add(new CardInstance(e.Card, e.Plus));
            if (mods.Has(Condition.Feel) && db.TodayCard != null) deck.Draw.Add(new CardInstance(db.TodayCard, 0));
            WeightedPick.Shuffle(ref _rng, deck.Draw);
            deck.Fixed = new CardInstance(db.FixedCard, 0);
        }

        /// <summary>1회초 첫 타자의 첫 공 직전까지 진행한다</summary>
        public void Start()
        {
            if (_s.Phase != MatchPhase.NotStarted) return;
            _s.Phase = MatchPhase.Pitching;
            StartAtBat();
        }

        // ───────────────────────── 타석 ─────────────────────────

        /// <summary>타석 시작 — 목표치, 손패 정리(고정 카드 + 새로 뽑기), 첫 공 노림 (프로토타입 startAB)</summary>
        private void StartAtBat()
        {
            var b = _s.Lineup[_s.BatterIndex];
            var ab = _s.AtBat;
            ab.Reset(b);
            ab.Target = TargetCalculator.Compute(_t.target, b.BaseTarget, _f.tStageMul, IsRisp(), BasesEmpty(), _s.Outs, b.LastHit,
                Lv(BonusId.Pressure), _s.Modifiers.Weather, _s.Inning, _s.Modifiers.Has(Evolution.Aura), out var notes);
            ab.TargetNotes = notes;

            // 손패 — 고정 카드를 뺀 남은 손패는 버림 더미로, 새로 (손패 수 − 1)장 (§4-1)
            var deck = _s.Deck;
            for (int i = 0; i < deck.Hand.Count; i++)
                if (!deck.Hand[i].Def.IsFixed) deck.Discard.Add(deck.Hand[i]);
            deck.Hand.Clear();
            deck.Hand.Add(deck.Fixed);
            Draw(HandSize() - 1);

            b.Faced++;
            SetupPitch();
        }

        /// <summary>손패 수 — 기본 + 「넓은 손패」 − 「몸이 덜 풀렸다」(1~2회)</summary>
        private int HandSize()
        {
            int n = _t.hand.handSize + Lv(BonusId.Grip);
            if (_s.Modifiers.Has(Condition.Stiff) && _s.Inning <= _t.hand.stiffUntilInning) n -= _t.hand.stiffMinus;
            return n;
        }

        /// <summary>덱에서 n장 — 덱이 비면 버림 더미를 섞어 덱으로 (프로토타입 drawN)</summary>
        private void Draw(int n)
        {
            var deck = _s.Deck;
            for (int i = 0; i < n; i++)
            {
                if (deck.Draw.Count == 0)
                {
                    deck.Draw.AddRange(deck.Discard);
                    deck.Discard.Clear();
                    WeightedPick.Shuffle(ref _rng, deck.Draw);
                }
                if (deck.Draw.Count == 0) break;
                int last = deck.Draw.Count - 1;
                deck.Hand.Add(deck.Draw[last]);
                deck.Draw.RemoveAt(last);
            }
        }

        /// <summary>공마다 — 타자가 노림을 정한다 (히든). 주자 계획(M7)·단서 배분(M6) 자리</summary>
        private void SetupPitch()
        {
            var ab = _s.AtBat;
            ab.PrevIntent = ab.HasIntent ? ab.Intent : (Intent?)null;
            ab.Intent = DecideIntent();
            ab.HasIntent = true;
        }

        /// <summary>노림 결정 — 상황 가중치 추첨 → 전환 비용으로 실패하면 직전 노림 유지 (§6-3, §6-4)</summary>
        private Intent DecideIntent()
        {
            var ab = _s.AtBat;
            var b = ab.Batter;
            Span<double> w = stackalloc double[IntentWeights.Count];
            var sit = new IntentWeights.Situation(ab.Balls, ab.Strikes, _s.Outs, _s.ScoreUs - _s.ScoreOpp,
                _s.Bases[0].On, _s.Bases[1].On, _s.Bases[2].On);
            IntentWeights.Situational(_t.intent, _t.scout, b.Def, b.BaseWeights, sit, _f.sitAmp, w);
            Intent wanted = IntentWeights.Order[WeightedPick.Pick(ref _rng, w)];

            double cost = SwitchCost.Cost(_t.intent, ab.PrevIntent, wanted);
            double p = SwitchCost.SuccessChance(_t.intent, cost, b.Def.Type.Adjust);
            // 2스트라이크에서 직전이 지켜보기면 예외 — 전환 성공 (지켜볼 수 없으므로)
            if (cost > 0 && _rng.NextDouble() > p && !(ab.PrevIntent == Intent.Take && ab.Strikes >= 2))
            {
                ab.SwitchFailed = true;
                return ab.PrevIntent.Value;
            }
            ab.SwitchFailed = false;
            return wanted;
        }

        // ───────────────────────── 도우미 ─────────────────────────

        private int Lv(BonusId id) => _s.Modifiers.Level(id);
        /// <summary>불펜 이름 표 (BonusTable이 없으면 3명 자리만)</summary>
        private string[] BullpenNames => _db.BonusTable != null && _db.BonusTable.Bullpen.Length > 0 ? _db.BonusTable.Bullpen : s_defaultPen;
        private static readonly string[] s_defaultPen = { "", "", "" };
        private bool IsRisp() => _s.Bases[1].On || _s.Bases[2].On;
        private bool BasesEmpty() => !_s.Bases[0].On && !_s.Bases[1].On && !_s.Bases[2].On;
        private BaseMask Mask() => new BaseMask(_s.Bases[0].On, _s.Bases[1].On, _s.Bases[2].On);
        private void ClearBases() { _s.Bases[0] = Runner.Empty; _s.Bases[1] = Runner.Empty; _s.Bases[2] = Runner.Empty; }
        private StaminaTierInfo Tier() => StaminaTier.Of(_t.stamina, _s.Stamina, _s.StaminaMax, _s.Modifiers.Has(Evolution.Iron));
        private void AddMental(int delta) => _s.Mental = JsMath.Clamp(_s.Mental + delta, _t.mental.min, _t.mental.max);
    }
}
