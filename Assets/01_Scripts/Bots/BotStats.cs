using System.Text;
using BBGame.Core;

namespace BBGame.Bots
{
    /// <summary>
    /// 봇 경기 집계 — 정수 합만 쌓고 비율은 읽을 때 계산한다 (프로토타입 addGame · renderSimOut).
    /// 9이닝 실점은 주인공 실점(강판 때 남긴 주자 실점 포함) ÷ 투구 이닝 × 9 (§19-3)
    /// </summary>
    public struct BotStats
    {
        public int Games, Wins, Ties;
        /// <summary>주인공이 잡은 아웃 (투구 이닝 × 3)</summary>
        public int Outs;
        /// <summary>주인공 실점 (남긴 주자 실점 포함)</summary>
        public int Runs;
        /// <summary>상대 총득점 (불펜 포함)</summary>
        public int RunsTotal;
        /// <summary>주인공이 상대한 타석</summary>
        public int PlateAppearances;
        public int Strikeouts, Hits;
        /// <summary>4사구 (볼넷 + 몸에 맞는 공)</summary>
        public int Walks;
        public int Pitches;
        public int ExitStamina, ExitLimit, ExitBench;

        /// <summary>끝난 경기 하나를 더한다</summary>
        public void Add(IMatchReadOnly s, int plateAppearances)
        {
            Games++;
            if (s.Result == MatchResult.Win) Wins++;
            if (s.Result == MatchResult.Tie) Ties++;
            var line = s.Line;
            Outs += line.OutsPitched;
            Runs += line.Runs;
            RunsTotal += s.ScoreOpp;
            PlateAppearances += plateAppearances;
            Strikeouts += s.Strikeouts;
            Hits += s.HitsOpp;
            Walks += s.Walks;
            Pitches += s.Pitches;
            if (s.ExitReason == ExitReason.Stamina) ExitStamina++;
            if (s.ExitReason == ExitReason.PitchLimit) ExitLimit++;
            if (s.ExitReason == ExitReason.Bench) ExitBench++;
        }

        public double RunsPer9 => Outs > 0 ? Runs / (Outs / 3.0) * 9 : 0;
        public double WinRate => Games > 0 ? (double)Wins / Games : 0;
        public double StrikeoutRate => PlateAppearances > 0 ? (double)Strikeouts / PlateAppearances : 0;
        public double HitRate => PlateAppearances > 0 ? (double)Hits / PlateAppearances : 0;
        public double WalkRate => PlateAppearances > 0 ? (double)Walks / PlateAppearances : 0;
        public double InningsPerGame => Games > 0 ? Outs / 3.0 / Games : 0;
        public double PitchesPerGame => Games > 0 ? (double)Pitches / Games : 0;
        public double PitchesPerPlateAppearance => PlateAppearances > 0 ? (double)Pitches / PlateAppearances : 0;
        public double RunsTotalPerGame => Games > 0 ? (double)RunsTotal / Games : 0;

        /// <summary>표 한 줄 (개발용 콘솔 출력)</summary>
        public void AppendLine(StringBuilder sb, string name)
        {
            sb.Append(name).Append(" — 9이닝 실점 ").Append(RunsPer9.ToString("0.00"))
              .Append(" · 승률 ").Append((WinRate * 100).ToString("0.0")).Append('%')
              .Append(" · 삼진 ").Append((StrikeoutRate * 100).ToString("0.0")).Append('%')
              .Append(" · 피안타 ").Append((HitRate * 100).ToString("0.0")).Append('%')
              .Append(" · 4사구 ").Append((WalkRate * 100).ToString("0.0")).Append('%')
              .Append(" · ").Append(InningsPerGame.ToString("0.00")).Append("이닝/경기")
              .Append(" · ").Append(PitchesPerGame.ToString("0.0")).Append("구/경기")
              .Append(" · 투구/타석 ").Append(PitchesPerPlateAppearance.ToString("0.00"))
              .Append(" · 총실점/경기 ").Append(RunsTotalPerGame.ToString("0.00"))
              .Append(" · 강판 스태미나 ").Append(ExitStamina).Append(" / 105구 ").Append(ExitLimit).Append(" / 벤치 ").Append(ExitBench)
              .Append(" (").Append(Games).Append("경기)").AppendLine();
        }
    }
}
