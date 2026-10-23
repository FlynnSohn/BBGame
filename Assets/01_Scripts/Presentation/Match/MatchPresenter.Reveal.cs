using BBGame.Core;
using BBGame.Presentation.Common;
using BBGame.Presentation.Data;
using BBGame.Presentation.Views;
using UnityEngine;

namespace BBGame.Presentation.Match
{
    public sealed partial class MatchPresenter
    {
        /// <summary>
        /// 간이 리빌 (M2) — 손패 자리를 대신한다. 순서는 §20-2: 읽기 판정(큰 글씨) → 타자의 노림 → 제압치·게이지 → 결과 줄 → 노트.
        /// 리빌 동안 전광판·타자 패널은 결과의 "던진 뒤" 값으로 그린다 (D2) — 타석이 끝났어도 이 공의 타자를 보여 준다
        /// </summary>
        private void ShowReveal(PitchResult r)
        {
            _v.hand.SetVisible(false);
            _v.throwButton.SetVisible(false);

            // 전광판 — 타석이 끝났으면 카운트는 0-0
            var c = r.CountAfter;
            RenderScoreboard(r.AtBatEnded ? 0 : c.Balls, r.AtBatEnded ? 0 : c.Strikes, r.OutsAfter, r.BasesAfter);
            RenderPitchCount();
            RenderResources();
            // 타자 패널 게이지가 리빌과 같은 순간에 찬다 (§20-2-7). 다음 타자 정보는 [다음] 뒤에
            RenderBatter(r.Batter, r.GaugeAfter, r.TargetAfter, r.AtBatEnded ? _lastTargetNotes : TargetNotesText(S.TargetNotes));
            _v.field.SetBatterHand(r.Batter.Def.Hand);
            RenderZoneReveal(r);

            var t = new RevealTexts
            {
                Verdict = VerdictText(r, out var verdictColor),
                VerdictColor = verdictColor,
                IntentName = Loc.Get(_cat.IntentName(r.BatterIntent)),
                StabLine = _args.Clear().Set("stab", Loc.Get(_cat.StabName(r.Stab))).Set("desc", Loc.Get(_cat.StabDesc(r.Stab))).Format(Ui.stabLine),
                StabColor = StabColor(r.Stab, true),
                SwitchNote = r.SwitchFailed ? Loc.Get(Ui.switchFailed) : string.Empty,
                GainLine = GainText(r),
                GaugeLine = GaugeText(r),
                ResultLine = _args.Clear().Set("card", Loc.Get(_cat.CardName(r.Card.Def.Id))).Set("velo", r.Velocity)
                    .Set("zone", Loc.Get(_cat.Zone(r.Actual.Id))).Set("balls", (int)r.CountBefore.Balls).Set("strikes", (int)r.CountBefore.Strikes)
                    .Format(Ui.resultLine),
                Tag = TagText(r),
                TagColor = TagColor(r.Outcome),
                Notes = NotesText(r),
                NextLabel = Loc.Get(r.AtBatEnded ? Ui.nextBatter : Ui.nextPitch),
            };
            _v.reveal.Show(t);
        }

        /// <summary>리빌 존 — 실제로 들어간 칸(찌르기 색)과, 흔들렸으면 의도한 칸</summary>
        private void RenderZoneReveal(PitchResult r)
        {
            var batterHand = r.Batter.Def.Hand;
            var actualColor = StabColor(r.Stab, false);
            actualColor.a = 0.75f;
            for (int v = 0; v < Zone.Count; v++)
            {
                var visual = (ZoneId)v;
                var logical = PitcherView.ZoneOfVisual(visual, batterHand);
                var state = logical == r.Actual.Id ? ZoneCellState.Actual
                    : r.Shaken && logical == r.Intended.Id ? ZoneCellState.Intended : ZoneCellState.Idle;
                _v.zone.Cell(visual).Set(state, 0, actualColor, _pal);
            }
        }

        /// <summary>큰 글씨 — 0단계는 찌르기 결과, 1단계부터는 읽기 판정 (§8-4, 프로토타입 readLine)</summary>
        private string VerdictText(PitchResult r, out Color color)
        {
            if (r.ReadState == ReadState.Stage0)
            {
                color = StabColor(r.Stab, false);
                return Loc.Get(_cat.StabName(r.Stab));
            }
            ReadVerdict v;
            switch (r.ReadState)
            {
                case ReadState.Read: v = ReadVerdict.Read; break;
                case ReadState.ReadNotStabbed: v = r.Stab == Stab.Bad ? ReadVerdict.ReadButBad : ReadVerdict.ReadNotStabbed; break;
                case ReadState.Missed: v = r.Stab == Stab.Counter ? ReadVerdict.MissedButStabbed : ReadVerdict.Missed; break;
                default: v = r.Stab == Stab.Counter ? ReadVerdict.UndeclaredStabbed : ReadVerdict.UndeclaredThrown; break;
            }
            color = v == ReadVerdict.Read ? _pal.read : v == ReadVerdict.Missed ? _pal.intent : _pal.text;
            return Loc.Get(_cat.Read(v));
        }

        /// <summary>제압치 줄 — 기본 제압치 × 읽기 배수 (× 볼 적립) = +쌓인 값. 배수는 여기서 처음 공개한다 (§8-1, §20-4)</summary>
        private string GainText(PitchResult r)
        {
            _args.Clear().Set("base", r.BaseShown).Set("mul", Loc.Num(r.ReadMultiplier)).Set("gain", r.Gain);
            if (!r.BallKept) return _args.Format(Ui.gainLine);
            return _args.Set("keep", Loc.Num(T.read.ballKeep)).Format(Ui.gainLineBall);
        }

        private string GaugeText(PitchResult r)
        {
            if (r.Reaction == Reaction.Hbp) return Loc.Get(Ui.gaugeHbp);
            if (r.Reaction == Reaction.Ball && !r.BallKept) return Loc.Get(Ui.gaugeBall);
            return _args.Clear().Set("before", r.GaugeBefore).Set("after", r.GaugeAfter).Set("target", r.TargetBefore).Format(Ui.gaugeLine);
        }

        private string TagText(PitchResult r)
        {
            string tag = Loc.Get(_cat.Outcome(r.Outcome));
            return r.RunsScored > 0 ? _args.Clear().Set("tag", tag).Set("runs", r.RunsScored).Format(Ui.tagRuns) : tag;
        }

        /// <summary>노트 — 제구 미스, 특수 카드 착지, 코스형 규칙, 선구안형 목표치, 볼 적립, 커트, 투수 코치 방문 (§20-2-10·11)</summary>
        private string NotesText(PitchResult r)
        {
            _sb.Clear();
            if (r.Shaken)
                Line(_args.Clear().Set("intended", Loc.Get(_cat.Zone(r.Intended.Id))).Set("actual", Loc.Get(_cat.Zone(r.Actual.Id)))
                    .Set("intendedBase", r.BaseIfIntended).Set("actualBase", r.BaseShown).Format(Ui.noteShaken));
            if (r.Landing == LandingKind.PowerPitch) Line(_args.Clear().Set("zone", Loc.Get(_cat.Zone(r.Intended.Id))).Format(Ui.notePower));
            if (r.Landing == LandingKind.CenterAim) Line(_args.Clear().Set("zone", Loc.Get(_cat.Zone(r.Intended.Id))).Format(Ui.noteCenter));
            if (r.CourseCapped) Line(Loc.Get(Ui.noteCourseCap));
            if (r.TargetAfter > r.TargetBefore) Line(_args.Clear().Set("n", r.TargetAfter - r.TargetBefore).Format(Ui.noteEyeUp));
            if (r.Reaction == Reaction.Ball)
                Line(r.BallKept ? _args.Clear().Set("pct", Loc.Num(T.read.ballKeep * 100, 0)).Format(Ui.noteBallKept) : Loc.Get(Ui.noteBallLost));
            if (r.Outcome == PitchOutcome.Cut) Line(_args.Clear().Set("target", r.TargetBefore).Format(Ui.noteCut));
            if (r.BenchVisit) Line(_args.Clear().Set("pitcher", Loc.Get(Ui.pitcherName)).Format(Ui.noteBenchVisit));
            return _sb.ToString();
        }

        private void Line(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (_sb.Length > 0) _sb.Append('\n');
            _sb.Append(text);
        }

        /// <summary>찌르기 색 — 찔렀다 청록 · 비켜갔다 회색 · 걸렸다 레드 (§6-4)</summary>
        private Color StabColor(Stab s, bool small)
            => s == Stab.Counter ? (small ? _pal.readText : _pal.read)
             : s == Stab.Bad ? (small ? _pal.intentText : _pal.intent)
             : _pal.textSub;   // 비켜갔다 — 밝은 패널에서 읽히도록 보조 글자색

        /// <summary>결과 태그 색 — 출루·안타 레드, 아웃·삼진 청록, 압도 골드, 나머지 보조 (프로토타입 tagCls)</summary>
        private Color TagColor(PitchOutcome o)
        {
            switch (o)
            {
                case PitchOutcome.Single: case PitchOutcome.Double: case PitchOutcome.Triple: case PitchOutcome.HomeRun:
                case PitchOutcome.Walk: case PitchOutcome.Hbp:
                    return _pal.intentText;
                case PitchOutcome.Dominant:
                    return _pal.goldBorder;
                case PitchOutcome.Strikeout: case PitchOutcome.GroundOut: case PitchOutcome.FlyOut: case PitchOutcome.DoublePlay:
                case PitchOutcome.Sacrifice: case PitchOutcome.BuntStrikeout:
                    return _pal.readText;
                default:
                    return _pal.textSub;
            }
        }
    }
}
