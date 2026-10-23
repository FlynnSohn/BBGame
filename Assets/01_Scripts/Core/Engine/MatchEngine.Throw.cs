namespace BBGame.Core
{
    public sealed partial class MatchEngine
    {
        /// <summary>
        /// 한 공을 던진다 — 착지부터 손패 보충·다음 공 노림까지 한 번에 처리하고 결과를 돌려준다 (프로토타입 throwPitch).
        /// 결과 객체는 재사용된다 (D3). 던질 수 없는 명령이면 null.
        /// </summary>
        public PitchResult Throw(in ThrowCommand cmd)
        {
            if (!CanThrow(cmd.HandIndex, cmd.Zone)) return null;

            var r = _result;
            r.Reset();
            var ab = _s.AtBat;
            var b = ab.Batter;
            var deck = _s.Deck;
            var inst = deck.Hand[cmd.HandIndex];
            var def = inst.Def;
            var pt = _db.PitchTypes.Get(def.PitchType);
            var ctl = _t.control;
            var mods = _s.Modifiers;

            r.Card = inst; r.HandIndex = cmd.HandIndex; r.Batter = b; r.PitchNumber = ab.Pitches.Count + 1;
            r.Declared = _f.declare ? cmd.Declared : null;   // 던질 때 확정 (§7-5) — M6
            r.BatterIntent = ab.Intent; r.SwitchFailed = ab.SwitchFailed;
            r.CountBefore = new Count(ab.Balls, ab.Strikes); r.BasesBefore = Mask(); r.OutsBefore = _s.Outs;
            r.GaugeBefore = ab.Gauge; r.TargetBefore = ab.Target;
            _unearned = 0;

            // 1. 화면 구속 = 기대 구속 ± 흔들림 (§4-6) — 비용을 내기 전 구간으로
            var tierBefore = Tier();
            double veloExp = PitchPower.ExpectedVelo(_t.stuff, pt, def, tierBefore.Level, false, 0);
            double noise = _t.stuff.veloNoise;
            int velo = JsMath.RoundHalfUp(veloExp + (_rng.NextDouble() * (2 * noise) - noise));
            r.Velocity = velo;

            // 2. 스태미나·투구 수
            _s.Stamina -= PitchPower.Cost(_t.stuff, def, mods.Weather);
            _s.Pitches++;

            // 3. 특수 카드 착지 — 던지는 순간 칸이 정해진다 (흔들리기 전)
            Zone zone;
            if (def.IsPowerPitch) { zone = Landing.PowerLanding(ctl, ref _rng); r.Landing = LandingKind.PowerPitch; }
            else if (def.IsFixed)
            {
                zone = Landing.MidLanding(ctl, ref _rng);
                if (!zone.IsCenter) r.Landing = LandingKind.CenterAim;
            }
            else zone = new Zone(cmd.Zone.Value);
            r.Intended = zone;

            // 4. 흔들림 (§11-3) — 비용을 낸 뒤 구간으로
            var tier = Tier();
            double shake = ctl.shakeBase + (ctl.mentalCenter - _s.Mental) * ctl.mentalSlope + tier.ShakeAdd
                           - ctl.controlPerLv * Lv(BonusId.Control) + (def.Grade == Grade.S ? -ctl.sGradeBonus : 0);
            if (mods.Weather == Weather.Cold && !pt.IsFastball) shake += ctl.coldBreaking;
            if (mods.Weather == Weather.Wet && def.Grade == Grade.S) shake += ctl.wetSGrade;
            if (def.IsPowerPitch) shake += ctl.powerShake;
            // (M7) 퀵모션 흔들림
            shake += pt.shakeAdd;
            // (M7) 벼랑 끝 −, 풀파워 뒤 +
            bool backdoorOpp = def.Special == CardSpecial.Backdoor && PitcherHand != b.Def.Hand;
            if (zone.IsCorner && !def.IsPowerPitch && !def.IsFixed && !backdoorOpp) shake += ctl.cornerShake;
            shake = JsMath.Clamp(shake, ctl.shakeMin, ctl.shakeMax);
            if (def.IsFixed) shake *= ctl.fixedShakeMul;

            Zone actual = zone;
            if (_rng.NextDouble() < shake)
            {
                r.Shaken = true;
                if (def.IsFixed) actual = Landing.NeighborIn(ref _rng, zone);
                else actual = _rng.NextDouble() < ctl.centerPull && !zone.IsCenter ? new Zone(ZoneId.MidMid) : Landing.Neighbor(ctl, ref _rng, zone);
            }
            r.Actual = actual;

            // 5. 의도 칸 기준 값 (화면용) — 계산은 모두 실제 칸으로
            var pv = PreviewCore(inst, actual, false, null);
            r.BaseIfIntended = r.Shaken ? PreviewCore(inst, zone, false, null).BaseShown : pv.BaseShown;
            r.BaseShown = pv.BaseShown;
            r.AddShown = pv.AddShown;

            // 6. 판정·읽기 배수 (§6-2, §8-4)
            bool fast = pt.IsFastball;
            var stab = StabJudge.Judge(ab.Intent, actual, fast);
            var rm = ReadMultiplier.Compute(_t.read, _f.declare, r.Declared, ab.Intent, stab, def.Special, Lv(BonusId.Bold),
                b.Def.Type.IsCourse, ab.HasPrev ? ab.Prev.Actual.Col : (ZoneCol?)null, actual.Col);
            r.Stab = stab; r.ReadState = rm.State; r.ReadMultiplier = rm.Multiplier; r.CourseCapped = rm.CourseCapped;
            // (M7) 고급·기묘한 족보 사용·발견

            // 7. 반응 (§9)
            var reaction = React(def, pt, actual, stab, velo, out bool chased);
            // (M7) 몸에 맞는 공
            r.Reaction = reaction; r.Chased = chased;

            // 8. 게이지 — 볼은 찔렀다(유인)만 40% 적립 (§8-1)
            bool keep = reaction == Reaction.Ball && stab == Stab.Counter;
            int gain = reaction == Reaction.Ball || reaction == Reaction.Hbp
                ? (keep ? JsMath.RoundHalfUp(pv.BaseActual * rm.Multiplier * _t.read.ballKeep) : 0)
                : JsMath.RoundHalfUp(pv.BaseActual * rm.Multiplier);
            ab.Gauge += gain;
            if (!ab.Reached && ab.Gauge >= ab.Target) { ab.Reached = true; r.ReachedTarget = true; }
            r.Gain = gain; r.BallKept = keep && gain > 0;

            // 9. 결과 처리 (§10)
            bool abEnd = false;
            int runs = 0;
            PitchOutcome res = PitchOutcome.Ball;
            switch (reaction)
            {
                case Reaction.Ball:
                    ab.Balls++;
                    if (b.Def.Type.Kind == BatterKind.Eye && stab != Stab.Counter) ab.Target += _t.target.eyeTakeUp;   // 선구안형 — 골라낸 볼마다
                    res = PitchOutcome.Ball;
                    if (ab.Balls >= 4) { runs += WalkAdvance(); abEnd = true; res = PitchOutcome.Walk; }
                    break;
                case Reaction.Look:
                case Reaction.Whiff:
                    if (ab.Strikes == 2) StrikeOut(ref res, ref abEnd);
                    else { ab.Strikes++; res = reaction == Reaction.Look ? PitchOutcome.Look : PitchOutcome.Whiff; }
                    if (reaction == Reaction.Whiff && actual.Outside) ab.ChaseWhiffs++;
                    break;
                case Reaction.Foul:
                    if (ab.Intent == Intent.Bunt && ab.Strikes == 2) { _s.Outs++; abEnd = true; res = PitchOutcome.BuntStrikeout; }   // 스리번트
                    else { if (ab.Strikes < 2) ab.Strikes++; res = PitchOutcome.Foul; }
                    break;
                case Reaction.InPlay:
                    abEnd = true;
                    runs += ResolveInPlayOutcome(def, pt, actual, stab, chased, velo, ref res);
                    break;
            }

            // (M7) 폭투·주자 작전 결과
            if (_s.Outs > 3) _s.Outs = 3;
            if (rm.State == ReadState.Read) AddMental(_t.mental.read);
            AddOppRuns(runs);
            if (ab.Balls == 3 && ab.Strikes == 0) ab.Was30 = true;

            // 10. 투구 기록 — 실제 칸 (족보·직전 공·마운드 점, v11)
            ab.Pitches.Add(new PitchRecord
            {
                Number = ab.Pitches.Count + 1, Actual = actual, Intended = zone, Shaken = r.Shaken, Type = def.PitchType, Fastball = fast,
                Velo = velo, Stab = stab, Outcome = res, Power = def.IsPowerPitch, Fixed = def.IsFixed, Strike = IsStrike(res)
            });

            // 11. 손패·집중력 — 가운데 보고 던지기는 손패에 남는다
            if (!def.IsFixed)
            {
                deck.Hand.RemoveAt(cmd.HandIndex);
                deck.Discard.Add(inst);
            }
            _s.Focus = System.Math.Min(_s.FocusMax, _s.Focus + _t.hand.focusRegen + _t.hand.breathPerLv * Lv(BonusId.Breath));

            r.Outcome = res;
            r.GaugeAfter = ab.Gauge; r.TargetAfter = ab.Target;
            r.CountAfter = new Count(ab.Balls, ab.Strikes);
            r.RunsScored = runs;

            // 12. 타석 종료·반 이닝·강판·다음 공
            if (abEnd)
            {
                bool hit = IsHit(res);
                b.LastHit = hit;
                b.LastResult = hit ? LastResult.Hit : IsStrikeoutResult(res) ? LastResult.Strikeout
                    : res == PitchOutcome.Walk || res == PitchOutcome.Hbp ? LastResult.Walk : LastResult.Out;
                b.RuleKnown = true;
                _s.HitStreak = hit && Tier().Level == 3 ? _s.HitStreak + 1 : 0;   // 구간 3 연속 피안타 (벤치 판단)
                _s.WalkStreak = res == PitchOutcome.Walk || res == PitchOutcome.Hbp ? _s.WalkStreak + 1 : 0;
                // (M7) 상태형 기묘한 족보
                _s.BatterIndex = (_s.BatterIndex + 1) % 9;
            }
            r.AtBatEnded = abEnd;

            if (_s.Outs >= 3) EndHalfInning();
            else if (_s.Stamina <= 0) PitcherExit(ExitReason.Stamina);
            else if (abEnd)
            {
                if (_s.Pitches >= _t.bench.pitchLimit) PitcherExit(ExitReason.PitchLimit);   // 1일 투구 수 — 타석을 마치고
                else if (!BenchCheck()) StartAtBat();
            }
            else
            {
                if (!def.IsFixed) Draw(1);
                SetupPitch();
            }

            r.BasesAfter = Mask(); r.OutsAfter = _s.Outs;
            r.StaminaAfter = _s.Stamina; r.MentalAfter = _s.Mental;
            r.HalfInningEnded = _s.Phase == MatchPhase.HalfInningOver;
            r.PitcherExited = _s.Phase == MatchPhase.PitcherExited;
            r.ExitReason = _s.ExitReason;
            return r;
        }

        /// <summary>세 번째 스트라이크 — 목표치 이상이면 삼진(2T 이상 압도), 미만이면 커트 (§10-1)</summary>
        private void StrikeOut(ref PitchOutcome res, ref bool abEnd)
        {
            var ab = _s.AtBat;
            if (ab.Gauge >= ab.Target)
            {
                _s.Outs++;
                abEnd = true;
                res = ab.Gauge >= 2 * ab.Target ? PitchOutcome.Dominant : PitchOutcome.Strikeout;
                AddMental(res == PitchOutcome.Dominant ? _t.mental.dominant : _t.mental.strikeout);
            }
            else res = PitchOutcome.Cut;   // 파울 처리, 2스트라이크 유지
        }

        /// <summary>반 이닝 종료 — 주자를 비우고 대기 (M3: 3택1·우리 공격)</summary>
        private void EndHalfInning()
        {
            ClearBases();
            // (M7) 만루 탈출·경제적 이닝
            _s.Phase = MatchPhase.HalfInningOver;
        }

        /// <summary>강판 — M1은 신호만. 남은 이닝 불펜 처리는 M3</summary>
        private void PitcherExit(ExitReason reason)
        {
            _s.ExitReason = reason;
            _s.Phase = MatchPhase.PitcherExited;
        }

        /// <summary>
        /// 벤치 판단 (§22-2) — 이닝 실점 3 · 구간 3 연속 피안타 2 · 연속 사사구 2.
        /// 처음이면 투수 코치 방문(멘탈 +3, 연속 기록 초기화), 같은 이닝에 다시면 강판. 강판했으면 true.
        /// </summary>
        private bool BenchCheck()
        {
            var bt = _t.bench;
            bool trig = _s.InningRuns >= bt.inningRuns || _s.HitStreak >= bt.hitStreak || _s.WalkStreak >= bt.walkStreak;
            if (!trig) return false;
            if (_s.BenchVisitInning == _s.Inning)
            {
                if (_s.InningRuns > _s.BenchVisitRuns || _s.HitStreak >= bt.hitStreak || _s.WalkStreak >= bt.walkStreak)
                {
                    PitcherExit(ExitReason.Bench);
                    return true;
                }
                return false;
            }
            _s.BenchVisitInning = _s.Inning;
            _s.BenchVisitRuns = _s.InningRuns;
            _s.HitStreak = 0;
            _s.WalkStreak = 0;
            AddMental(_t.mental.benchVisit);
            _result.BenchVisit = true;
            return false;
        }

        /// <summary>상대 득점 기록 — 주인공 책임 실점·자책점 (§23-3)</summary>
        private void AddOppRuns(int n)
        {
            if (n <= 0) return;
            _s.ScoreOpp += n;
            _s.LineOpp[_s.Inning - 1] += n;
            _s.RunsAllowed += n;
            _s.EarnedRuns += n - System.Math.Min(n, _unearned);
            _s.InningRuns += n;
        }

        private static bool IsHit(PitchOutcome o) => o == PitchOutcome.Single || o == PitchOutcome.Double || o == PitchOutcome.Triple || o == PitchOutcome.HomeRun;
        private static bool IsStrikeoutResult(PitchOutcome o) => o == PitchOutcome.Strikeout || o == PitchOutcome.Dominant || o == PitchOutcome.BuntStrikeout;
        // 프로토타입 STRIKE_RES = look · whiff · foul · cut · k · dom
        private static bool IsStrike(PitchOutcome o) => o == PitchOutcome.Look || o == PitchOutcome.Whiff || o == PitchOutcome.Foul
                                                       || o == PitchOutcome.Cut || o == PitchOutcome.Strikeout || o == PitchOutcome.Dominant;
    }
}
