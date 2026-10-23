using System.Collections.Generic;

namespace BBGame.Core
{
    /// <summary>
    /// 경기 단위 변동 굴리기 (§12) — 프로토타입 rollConds · rollWeather · rollPen과 같은 순서·같은 난수.
    /// 엔진 생성자가 덱 셔플 전에 부른다 (프로토타입 newGame과 같은 자리). 경기 전 화면(M8)이 생기면 위치를 다시 정한다
    /// </summary>
    public static class Pregame
    {
        // 프로토타입 키 순서
        private static readonly Condition[] s_good = { Condition.Feel, Condition.Live, Condition.Clear };
        private static readonly Condition[] s_bad = { Condition.Curve, Condition.Heavy, Condition.Stiff };
        private static readonly Weather[] s_weather = { Weather.Cold, Weather.Heat, Weather.Wind, Weather.Wet, Weather.Night, Weather.Dust };
        // 보통이 둘 몫 (§12-3)
        private static readonly BullpenState[] s_pen = { BullpenState.Good, BullpenState.Mid, BullpenState.Mid, BullpenState.Bad };

        /// <summary>좋은 컨디션 하나 + 나쁜 컨디션 하나. 「머리가 맑다」는 집중력이 없는 단계에서 뽑지 않는다</summary>
        public static Condition RollConditions(StageFeatures f, ref SeededRandom rng)
        {
            int goodCount = f.focusOn ? s_good.Length : s_good.Length - 1;   // Clear가 마지막
            var good = s_good[rng.NextIndex(goodCount)];
            var bad = s_bad[rng.NextIndex(s_bad.Length)];
            return good | bad;
        }

        /// <summary>날씨 하나. 야간 조명·황사(단서 기능)는 wxClue가 꺼진 단계에서 뽑지 않는다</summary>
        public static Weather RollWeather(StageFeatures f, ref SeededRandom rng)
        {
            int count = f.wxClue ? s_weather.Length : s_weather.Length - 2;   // Night·Dust가 마지막 둘
            return s_weather[rng.NextIndex(count)];
        }

        /// <summary>불펜 투수마다 오늘 상태</summary>
        public static void RollBullpen(int pitchers, ref SeededRandom rng, List<BullpenState> output)
        {
            output.Clear();
            for (int i = 0; i < pitchers; i++) output.Add(s_pen[rng.NextIndex(s_pen.Length)]);
        }
    }
}
