using System.Text;
using BBGame.Core;
using BBGame.Presentation.Common;
using BBGame.Presentation.Data;
using BBGame.Presentation.Views;
using UnityEngine;

namespace BBGame.Presentation.Match
{
    public sealed partial class MatchPresenter
    {
        private readonly StringBuilder _sb = new StringBuilder(128);
        /// <summary>마지막으로 그린 목표치 보정 내역 — 리빌 중 타석이 끝났으면(상태가 다음 타자로 넘어갔으면) 이 값을 그대로 쓴다</summary>
        private string _lastTargetNotes = string.Empty;

        // ───────────────────────── 전체 ─────────────────────────

        /// <summary>엔진 상태 그대로 모든 칸을 그린다 (투구 대기 화면)</summary>
        private void RefreshAll()
        {
            _sel.Validate(this);
            RenderScoreboard(S.Balls, S.Strikes, S.Outs, CurrentBases());
            RenderPitchCount();
            RenderToday();
            RenderResources();
            var b = S.CurrentBatter;
            _lastTargetNotes = TargetNotesText(S.TargetNotes);
            RenderBatter(b, S.Gauge, S.Target, _lastTargetNotes);
            _v.field.SetBatterHand(b.Def.Hand);
            _v.zone.SetBatterSide(b.Def.Hand, Loc.Get(b.Def.Hand == Hand.Right ? Ui.batsRight : Ui.batsLeft));
            bool pitching = _engine.Phase == MatchPhase.Pitching;
            _v.hand.SetVisible(pitching);
            _v.throwButton.SetVisible(pitching);
            RefreshPlay();
        }

        /// <summary>존·손패·[던진다] — 선택이 바뀔 때마다</summary>
        private void RefreshPlay()
        {
            if (_engine.Phase != MatchPhase.Pitching) return;
            RenderZone();
            RenderHand();
            RenderThrow();
        }

        private BaseMask CurrentBases() => new BaseMask(S.GetBase(0).On, S.GetBase(1).On, S.GetBase(2).On);

        // ───────────────────────── 상단 줄 ─────────────────────────

        private void RenderScoreboard(int balls, int strikes, int outs, BaseMask bases)
        {
            var sb = _v.scoreboard;
            sb.SetTeams(Loc.Get(_cat.SchoolName(_setup.Opponent.Id)), Loc.Get(_cat.SchoolName(HomeSchoolId)));
            sb.SetLine(S, _engine.Phase == MatchPhase.Ended, _pal);
            sb.SetCount(balls, strikes, outs, bases, _pal);
        }

        /// <summary>투구 수 + 다음 휴식 구간 경계 (Q7, §22-4)</summary>
        private void RenderPitchCount()
        {
            var o = RestRule.Outlook(T.rest, T.bench.pitchLimit, S.Pitches);
            _args.Clear().Set("days", o.Days).Set("left", o.Left).Set("next", o.DaysAfterEdge).Set("limit", o.NextEdge);
            string line = !o.HasEdge ? _args.Format(Ui.restOnly) : o.EdgeIsLimit ? _args.Format(Ui.restLimit) : _args.Format(Ui.restNext);
            _v.pitchCount.Set(S.Pitches, line, o.Warn, _pal);
        }

        /// <summary>'오늘' 칩 — 판정 방식, 날씨, 컨디션 (보너스는 M3)</summary>
        private void RenderToday()
        {
            var today = _v.today;
            today.Begin();
            today.Add(Loc.Get(_setup.IsPracticeGame ? Ui.umpireHuman : Ui.umpireAbs), _pal.panelBorder, _pal.text);
            if (S.Weather != Weather.None) today.Add(Loc.Get(_cat.WeatherName(S.Weather)), _pal.panelBorder, _pal.warnText);
            int bits = (int)S.Conditions;
            for (int bit = 1; bit <= bits; bit <<= 1)
            {
                if ((bits & bit) == 0) continue;
                var c = (Condition)bit;
                bool good = c == Condition.Feel || c == Condition.Live || c == Condition.Clear;
                today.Add(Loc.Get(_cat.ConditionName(c)), _pal.panelBorder, good ? _pal.readText : _pal.intentText);
            }
            today.End();
        }

        private void RenderResources()
        {
            var tier = StaminaTier.Of(T.stamina, S.Stamina, S.StaminaMax, (S.Evolutions & Evolution.Iron) != 0);
            _v.stamina.Set(S.Stamina, S.StaminaMax, Loc.Get(_cat.StaminaTier((StaminaLevel)tier.Level)), _pal.stamina);
            if (_engine.Features.focusOn)
            {
                int regen = T.hand.focusRegen + T.hand.breathPerLv * S.BonusLevel(BonusId.Breath);
                _v.focus.Set(S.Focus, S.FocusMax, _args.Clear().Set("n", regen).Format(Ui.focusRegen), _pal.focus);
            }
        }

        // ───────────────────────── 타자 패널 ─────────────────────────

        private void RenderBatter(BatterState b, int gauge, int target, string targetNotes)
        {
            var def = b.Def;
            string name = Loc.Get(_cat.BatterName(def.SeedKey));
            string header = _args.Clear().Set("order", b.Slot + 1).Set("name", name).Format(Ui.batterHeader);
            string pa = b.Faced >= 1 && b.Faced <= 5
                ? Loc.Get(_cat.Plate((PlateOrdinal)(b.Faced - 1)))
                : _args.Clear().Set("n", b.Faced).Format(Ui.plateNth);
            string info = _args.Clear().Set("grade", def.Grade).Set("position", Loc.Get(_cat.Position(def.SeedKey)))
                .Set("bats", Loc.Get(def.Hand == Hand.Right ? Ui.batsRight : Ui.batsLeft)).Set("pa", pa)
                .Format(Ui.batterInfo);
            var season = def.Season;
            string seasonLine = _args.Clear().Set("avg", Loc.Average(season.hits, season.atBats)).Set("hr", season.homeRuns).Set("rbi", season.rbi)
                .Format(Ui.seasonLine);
            string feature = b.RuleKnown ? Loc.Get(_cat.Rule(def.Type.Kind)) : Loc.Get(Ui.featureUnknown);
            _v.batter.SetBatter(header, Loc.Get(_cat.Kind(def.Type.Kind)), _pal.intent, info, seasonLine,
                Loc.Get(Ui.unknown), feature, b.RuleKnown, _pal);

            var status = gauge >= 2 * target ? GaugeStatus.Overwhelm : gauge >= target ? GaugeStatus.Dominate : GaugeStatus.Below;
            string targetText = _args.Clear().Set("target", target).Set("dominate", 2 * target).Format(Ui.gaugeTarget);
            _v.batter.Gauge.Set(gauge, target, targetText, Loc.Get(_cat.Gauge(status)), targetNotes, _pal);
        }

        /// <summary>목표치 보정 내역 — 값은 튜닝에서 (프로토타입 computeT notes)</summary>
        private string TargetNotesText(TargetNote notes)
        {
            if (notes == TargetNote.None) return string.Empty;
            var t = T.target;
            string sep = Loc.Get(Ui.listSeparator);
            _sb.Clear();
            for (int bit = 1; bit <= (int)notes; bit <<= 1)
            {
                var n = (TargetNote)bit;
                if ((notes & n) == 0) continue;
                _args.Clear();
                switch (n)
                {
                    case TargetNote.Stage: _args.Set("stage", S.Stage).Set("mul", Loc.Num(_engine.Features.tStageMul)); break;
                    case TargetNote.Risp: _args.Set("mul", Loc.Num(t.risp)); break;
                    case TargetNote.TwoOutEmpty: _args.Set("mul", Loc.Num(t.twoOutEmpty)); break;
                    case TargetNote.AfterHit: _args.Set("mul", Loc.Num(t.afterHit)); break;
                    case TargetNote.Pressure: _args.Set("pct", Loc.Num(t.pressurePerLv * S.BonusLevel(BonusId.Pressure) * 100, 0)); break;
                    case TargetNote.Dust: _args.Set("pct", Loc.Num((1 - t.dust) * 100, 0)); break;
                    case TargetNote.HeatLate: _args.Set("inning", t.heatFromInning).Set("pct", Loc.Num((1 - t.heatLate) * 100, 0)); break;
                    case TargetNote.Aura: _args.Set("mul", Loc.Num(t.aura)); break;
                }
                if (_sb.Length > 0) _sb.Append(sep);
                _sb.Append(_args.Format(_cat.TargetNoteText(n)));
            }
            return _args.Clear().Set("notes", _sb.ToString()).Format(Ui.targetNotes);
        }

        // ───────────────────────── 존 ─────────────────────────

        /// <summary>
        /// 투구 대기 중의 존 — 카드를 골랐으면(또는 끄는 중이면) 그 카드의 코스를 밝히고, 아니면 칸마다 던질 수 있는 손패 장수
        /// </summary>
        private void RenderZone()
        {
            var hand = S.Hand;
            var batterHand = S.CurrentBatter.Def.Hand;
            int card = _dragIndex >= 0 ? _dragIndex : _sel.Card;
            CardDef def = card >= 0 && card < hand.Count ? hand[card].Def : null;
            for (int v = 0; v < Zone.Count; v++)
            {
                var visual = (ZoneId)v;
                var logical = PitcherView.ZoneOfVisual(visual, batterHand);
                var state = ZoneCellState.Idle;
                int count = 0;
                if (def != null)
                {
                    bool lit = def.IsPowerPitch ? !new Zone(logical).Outside : def.CanTarget(logical);
                    bool selected = _dragIndex < 0 && !def.IsPowerPitch && _sel.Zone.HasValue && _sel.Zone.Value == logical;
                    state = selected ? ZoneCellState.Selected : lit ? ZoneCellState.Lit : ZoneCellState.Idle;
                }
                else
                {
                    if (_sel.Zone.HasValue && _sel.Zone.Value == logical) state = ZoneCellState.Selected;
                    for (int i = 0; i < hand.Count; i++)
                        if (!hand[i].Def.IsPowerPitch && hand[i].Def.CanTarget(logical)) count++;
                }
                _v.zone.Cell(visual).Set(state, count, default, _pal);
            }
        }

        // ───────────────────────── 손패 ─────────────────────────

        private void RenderHand()
        {
            var hand = S.Hand;
            int n = Mathf.Min(hand.Count, _v.hand.Capacity);
            for (int i = 0; i < n; i++)
            {
                var inst = hand[i];
                var def = inst.Def;
                string typeName = Loc.Get(_cat.PitchTypeName(def.PitchType));
                string type = def.IsFixed ? _args.Clear().Set("type", typeName).Format(Ui.cardFixedType) : typeName;
                string stats = _args.Clear().Set("stuff", PitchPower.CardStuff(T.stuff, def, inst.Plus))
                    .Set("cost", PitchPower.Cost(T.stuff, def, S.Weather)).Format(Ui.cardStats);
                var view = _v.hand.Card(i);
                view.Show(def.Grade, type, Loc.Get(_cat.CardName(def.Id)), CourseText(def),
                    def.Special == CardSpecial.None ? string.Empty : Loc.Get(_cat.Special(def.Special)), stats, _pal);
                bool dim = _sel.ZoneFirst && !_sel.HasCard && !_sel.Fits(i, this);
                view.SetState(_sel.Card == i, dim, _pal);
            }
            _v.hand.SetCount(n);
        }

        /// <summary>카드 코스 줄 — 전력투구 "존 어딘가", 가운데 보고 던지기 "한가운데 n% · 옆 칸 n%", 그 밖은 칸 이름</summary>
        private string CourseText(CardDef def)
        {
            if (def.IsPowerPitch) return Loc.Get(Ui.coursePower);
            if (def.IsFixed)
            {
                double center = T.control.midLandingCenter * 100;
                return _args.Clear().Set("center", Loc.Num(center, 0)).Set("side", Loc.Num(100 - center, 0)).Format(Ui.courseFixed);
            }
            var zones = def.Zones;
            if (zones.Length == 1) return Loc.Get(_cat.Zone(zones[0]));
            string sep = Loc.Get(Ui.courseSeparator);
            _sb.Clear();
            for (int i = 0; i < zones.Length; i++)
            {
                if (i > 0) _sb.Append(sep);
                _sb.Append(Loc.Get(_cat.Zone(zones[i])));
            }
            return _sb.ToString();
        }

        // ───────────────────────── [던진다] ─────────────────────────

        /// <summary>미리보기 — 구위 × (1 + 가산) = 기본 제압치 × ? (§20-4). 읽기 배수는 보여 주지 않는다</summary>
        private void RenderThrow()
        {
            if (!_sel.HasCard)
            {
                _v.throwButton.Set(false, Loc.Get(Ui.throwHint), string.Empty);
                return;
            }
            var cmd = _sel.ToCommand(this);
            bool can = _engine.CanThrow(cmd.HandIndex, cmd.Zone);
            var pv = _engine.Preview(cmd.HandIndex, cmd.Zone, _chips);
            if (!pv.Valid)
            {
                _v.throwButton.Set(false, Loc.Get(Ui.throwHint), string.Empty);
                return;
            }
            var inst = S.Hand[cmd.HandIndex];
            string preview = _args.Clear().Set("stuff", pv.Stuff).Set("add", Loc.Num(1 + pv.AddShown, 2)).Set("base", pv.BaseShown)
                .Format(Ui.throwPreview);
            string cost = _args.Clear().Set("cost", PitchPower.Cost(T.stuff, inst.Def, S.Weather)).Format(Ui.throwCost);
            _v.throwButton.Set(can, preview, cost);
        }
    }
}
