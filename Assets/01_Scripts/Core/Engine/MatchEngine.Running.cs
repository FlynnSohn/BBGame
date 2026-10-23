namespace BBGame.Core
{
    public sealed partial class MatchEngine
    {
        /// <summary>득점 한 명 — 실책 진루 주자면 비자책으로 센다 (§23-3)</summary>
        private int Score(Runner runner)
        {
            if (runner.ByError) _unearned++;
            return 1;
        }

        /// <summary>타자를 뺀 주자 전원 n루 진루 (희생번트)</summary>
        private int RunOn(int n)
        {
            int runs = 0;
            for (int i = 2; i >= 0; i--)
            {
                if (!_s.Bases[i].On) continue;
                var r = _s.Bases[i];
                _s.Bases[i] = Runner.Empty;
                if (i + n >= 3) runs += Score(r); else _s.Bases[i + n] = r;
            }
            return runs;
        }

        /// <summary>안타 진루 (§10-3) — go: 1루 주자가 스타트를 끊었다 (M7)</summary>
        private int HitAdvance(int h, bool go)
        {
            var rn = _t.running;
            var r1 = _s.Bases[0];
            var r2 = _s.Bases[1];
            var r3 = _s.Bases[2];
            var bat = new Runner((byte)_s.AtBat.Batter.Slot);
            int runs = 0;

            if (h == 4)
            {
                if (r1.On) runs += Score(r1);
                if (r2.On) runs += Score(r2);
                if (r3.On) runs += Score(r3);
                runs += 1;
                ClearBases();
                return runs;
            }
            if (h == 3)
            {
                if (r1.On) runs += Score(r1);
                if (r2.On) runs += Score(r2);
                if (r3.On) runs += Score(r3);
                _s.Bases[0] = Runner.Empty; _s.Bases[1] = Runner.Empty; _s.Bases[2] = bat;
                return runs;
            }
            if (h == 2)
            {
                if (r3.On) runs += Score(r3);
                if (r2.On) runs += Score(r2);
                _s.Bases[0] = Runner.Empty; _s.Bases[1] = bat; _s.Bases[2] = Runner.Empty;
                if (r1.On)
                {
                    if (_rng.NextDouble() < (go ? rn.doubleFirstScoresGo : rn.doubleFirstScores)) runs += Score(r1);
                    else _s.Bases[2] = r1;
                }
                return runs;
            }
            // 단타
            if (r3.On) runs += Score(r3);
            _s.Bases[0] = bat; _s.Bases[1] = Runner.Empty; _s.Bases[2] = Runner.Empty;
            if (r2.On)
            {
                if (_rng.NextDouble() < (go ? rn.singleSecondScoresGo : rn.singleSecondScores)) runs += Score(r2);
                else _s.Bases[2] = r2;
            }
            if (r1.On)
            {
                if (!_s.Bases[2].On && _rng.NextDouble() < (go ? rn.singleFirstToThirdGo : rn.singleFirstToThird)) _s.Bases[2] = r1;
                else _s.Bases[1] = r1;
            }
            return runs;
        }

        /// <summary>볼넷·몸에 맞는 공 — 타자 1루, 밀려나는 주자만 (§10-3)</summary>
        private int WalkAdvance()
        {
            var bat = new Runner((byte)_s.AtBat.Batter.Slot);
            var b = _s.Bases;
            if (!b[0].On) { b[0] = bat; return 0; }
            if (!b[1].On) { b[1] = b[0]; b[0] = bat; return 0; }
            if (!b[2].On) { b[2] = b[1]; b[1] = b[0]; b[0] = bat; return 0; }
            var r = b[2];
            b[2] = b[1]; b[1] = b[0]; b[0] = bat;
            return Score(r);
        }
    }
}
