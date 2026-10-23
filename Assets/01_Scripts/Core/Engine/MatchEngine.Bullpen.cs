namespace BBGame.Core
{
    public sealed partial class MatchEngine
    {
        /// <summary>
        /// 강판 (§22-3, 프로토타입 pitcherExit) — 상태가 가장 좋은 불펜이 남은 아웃을 맡고, 남은 이닝은 결과만 계산해 경기를 끝낸다.
        /// · 남은 아웃: 반 이닝 공식에 남은 아웃 비율을 곱한다. 불펜 실점 중 남긴 주자 수까지는 주인공 실점 (§14 v12 Z12)
        /// · 다음 이닝부터 불펜 수비력이 이닝마다 −3
        /// 화면 문장은 벤치가 결정한 것처럼 쓴다 (§20-5) — 문장은 Presentation
        /// </summary>
        private void PitcherExit(ExitReason reason)
        {
            _s.ExitReason = reason;
            _s.ExitInning = _s.Inning;
            _s.ExitOuts = _s.Outs;
            var pt = _t.pen;
            var o = _t.offense;

            // 올라갈 투수 — 목록이 있으면 상태가 가장 좋은 투수(같으면 앞 사람, 난수 없음), 없으면 이름만 고른다
            double def;
            int reliever;
            if (_s.BullpenList.Count > 0)
            {
                reliever = 0;
                for (int i = 1; i < _s.BullpenList.Count; i++)
                    if (_s.BullpenList[i] < _s.BullpenList[reliever]) reliever = i;
                def = PenValue(_s.BullpenList[reliever]);
            }
            else
            {
                reliever = _rng.NextIndex(BullpenNames.Length);
                def = pt.unknown;
            }
            _s.RelieverIndex = reliever;

            // 남은 아웃 — 남긴 주자가 들어오면 주인공 실점 (실책 진루 주자는 비자책)
            int runs = HalfInningSim.Runs(o, ref _rng, o.oppTeam, def, (3 - _s.Outs) / 3.0);
            int onBase = 0, byError = 0;
            for (int b = 0; b < 3; b++)
            {
                if (!_s.Bases[b].On) continue;
                onBase++;
                if (_s.Bases[b].ByError) byError++;
            }
            int mine = System.Math.Min(runs, onBase);
            AddOppRuns(mine, true, System.Math.Min(mine, byError));
            AddOppRuns(runs - mine, false, 0);
            _s.InheritedScored = mine;
            _s.HalfLogList.Add(new HalfInningRecord(HalfKind.BullpenFinish, _s.Inning, runs, 0, 0, 0, reliever));

            // 남은 이닝 — 회말 우리 공격(그 회초를 마친 투수는 불펜) → 다음 회초 불펜
            int innings = _t.game.innings;
            while (true)
            {
                int i = _s.Inning;
                if (i >= innings && _s.ScoreUs > _s.ScoreOpp)
                {
                    _s.LineUs[i - 1] = -1;
                    _s.LineUsPlayed = i;
                    break;
                }
                PlayUsHalf(PitcherOfRecord.Bullpen);
                if (i >= innings) break;
                _s.Inning++;
                def -= pt.decayPerInning;
                _s.LineOpp[_s.Inning - 1] = 0;
                int opp = HalfInningSim.Runs(o, ref _rng, o.oppTeam, def, 1);
                AddOppRuns(opp, false, 0);
                HalfInningSim.PickLine(ref _rng, opp, 9, out int a, out int bName, out int line);   // 상대 라인업 9명
                _s.HalfLogList.Add(new HalfInningRecord(HalfKind.BullpenInning, _s.Inning, opp, a, bName, line, reliever));
            }
            EndGame();
        }

        private double PenValue(BullpenState s) => s == BullpenState.Good ? _t.pen.good : s == BullpenState.Bad ? _t.pen.bad : _t.pen.mid;
    }
}
