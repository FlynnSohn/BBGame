using System;
using System.Threading;
using BBGame.Core;
using BBGame.Presentation.Common;
using BBGame.Presentation.Views;
using Cysharp.Threading.Tasks;

namespace BBGame.Presentation.Match
{
    public sealed partial class MatchPresenter
    {
        private readonly BonusChoiceTexts[] _choiceTexts = new BonusChoiceTexts[BonusOffers.Count];

        // ───────────────────────── 이닝 보너스 (§13) ─────────────────────────

        /// <summary>보너스 후보를 3택1 창에 보여 주고 고른 번호를 받는다. 효과 수치는 고른 뒤 레벨 기준</summary>
        private UniTask<int> PickBonusAsync()
        {
            var offers = S.Offers;
            var table = _db.BonusTable;
            int n = Math.Min(offers.Count, _choiceTexts.Length);
            for (int i = 0; i < n; i++)
            {
                var o = offers[i];
                if (o.Kind == OfferKind.Evolution)
                {
                    _choiceTexts[i] = new BonusChoiceTexts
                    {
                        Name = Loc.Get(_cat.EvolutionName(o.Evolution)),
                        Level = Loc.Get(Ui.evolutionTag),
                        Effect = Loc.Get(_cat.EvolutionDesc(o.Evolution)),
                        Note = Loc.Get(_cat.EvolutionReq(o.Evolution)),
                        Evolution = true,
                    };
                    continue;
                }
                int lv = S.BonusLevel(o.Bonus);
                double per = table != null ? table.Get(o.Bonus).perLevel : 0;
                _args.Clear().Set("v", Loc.Num(per * (lv + 1))).Set("now", Loc.Num(T.stamina.pacePct * 100, 0));
                _choiceTexts[i] = new BonusChoiceTexts
                {
                    Name = Loc.Get(_cat.BonusName(o.Bonus)),
                    Effect = _args.Format(_cat.BonusDesc(o.Bonus)),
                    Level = _args.Clear().Set("from", lv).Set("to", lv + 1).Format(Ui.bonusLevel),
                    Note = string.Empty,
                    Evolution = false,
                };
            }
            return _v.bonus.ShowAsync(_choiceTexts, n, _pal, _ct);
        }

        // ───────────────────────── 반 이닝 한 줄 · 구원 · 기록 ─────────────────────────

        /// <summary>우리 공격·불펜 이닝 한 줄 — Core가 뽑은 이름·문구 번호로 문장을 고른다 (§14)</summary>
        private string OffenseText(in HalfInningRecord h)
        {
            string id = h.Runs >= 4 ? "r4" : OffenseIds[h.Runs][h.Line];
            string a, b;
            if (h.Kind == HalfKind.UsOffense)
            {
                var names = _db.BonusTable.OurBatters;
                a = Loc.Get(_cat.OurBatterName(names[h.NameA]));
                b = Loc.Get(_cat.OurBatterName(names[HalfInningSim.SecondNameIndex(h.NameA, h.NameB)]));
            }
            else
            {
                a = Loc.Get(_cat.BatterName(S.GetBatter(h.NameA).Def.SeedKey));
                b = Loc.Get(_cat.BatterName(S.GetBatter(HalfInningSim.SecondNameIndex(h.NameA, h.NameB)).Def.SeedKey));
            }
            return _args.Clear().Set("n", a).Set("m", b).Set("runs", h.Runs).Format(_cat.OffenseLine(id));
        }

        // 문구 id — 할당을 피하려 미리 만든다 (점수 0~3 × 문구 번호, HalfInningSim.LineCountByRuns와 같은 수)
        private static readonly string[][] OffenseIds = BuildOffenseIds();

        private static string[][] BuildOffenseIds()
        {
            var counts = HalfInningSim.LineCountByRuns;
            var ids = new string[counts.Length][];
            for (int r = 0; r < counts.Length; r++)
            {
                ids[r] = new string[counts[r]];
                for (int i = 0; i < counts[r]; i++) ids[r][i] = "r" + r + "." + i;
            }
            return ids;
        }

        /// <summary>강판 뒤 남은 아웃을 맡은 불펜 한 줄 — "구원 2학년 배도윤 (오늘 좋음) — 1점 허용"</summary>
        private string ReliefLine()
        {
            int idx = S.RelieverIndex;
            var names = _db.BonusTable != null ? _db.BonusTable.Bullpen : Array.Empty<string>();
            if (idx < 0 || idx >= names.Length) return string.Empty;
            var state = idx < S.Bullpen.Count ? S.Bullpen[idx] : BullpenState.Mid;   // 상태를 모르면 보통 (프로토타입)
            int runs = 0;
            foreach (var h in S.HalfLog)
                if (h.Kind == HalfKind.BullpenFinish) { runs = h.Runs; break; }
            _args.Clear().Set("name", Loc.Get(_cat.BullpenName(names[idx]))).Set("state", Loc.Get(_cat.BullpenStateName(state))).Set("runs", runs);
            return _args.Format(runs > 0 ? Ui.reliefRuns : Ui.reliefHold);
        }

        /// <summary>오늘 투수 기록 한 줄 (§15) — 이닝은 "5 2/3" 꼴</summary>
        private string PitcherLineText()
        {
            var l = S.Line;
            int whole = l.OutsPitched / 3, part = l.OutsPitched % 3;
            string ip = part == 0 ? whole.ToString() : whole + " " + part + "/3";
            return _args.Clear().Set("ip", ip).Set("pitches", l.Pitches).Set("h", l.Hits).Set("r", l.Runs).Set("er", l.EarnedRuns)
                .Set("k", l.Strikeouts).Set("bb", l.Walks).Format(Ui.pitcherLine);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // ───────────────────────── 자동 진행 (개발용, G8) ─────────────────────────

        /// <summary>
        /// 자동 진행 — 손패[(투구 수) % 장수]를 첫 코스로 던지고, [다음]·보너스 첫 칸·안내를 스스로 누른다. 결과 창에서 멈춘다.
        /// 화면 확인 시간을 줄이려는 도구다 (사용자 요청 2026-10-24). TestMatchSetup에서 켠다
        /// </summary>
        public async UniTaskVoid RunAutoPlayAsync(float delay, CancellationToken ct)
        {
            var wait = TimeSpan.FromSeconds(delay);
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    await UniTask.Delay(wait, cancellationToken: ct);
                    if (_v.result.IsShown) return;
                    if (_v.reveal.IsShown) { _v.reveal.Next(); continue; }
                    if (_v.bonus.IsShown) { _v.bonus.Choose(0); continue; }
                    if (_v.banner.IsShown) { _v.banner.Confirm(); continue; }
                    if (!Accepting || S.Hand.Count == 0) continue;
                    int i = S.Pitches % S.Hand.Count;
                    var def = S.Hand[i].Def;
                    ThrowAsync(new ThrowCommand(i, def.IsPowerPitch ? (ZoneId?)null : def.Zones[0])).Forget();
                }
            }
            catch (OperationCanceledException) { }
        }
#endif
    }
}
