using System;
using System.Collections.Generic;
using BBGame.Core;

namespace BBGame.Bots
{
    /// <summary>한 경기에서 봇이 고른 것 (비교 테스트용 — 기록할 때만 할당한다)</summary>
    public sealed class BotTrace
    {
        public readonly List<ThrowCommand> Throws = new List<ThrowCommand>(80);
        public readonly List<int> Bonus = new List<int>(9);
    }

    /// <summary>
    /// 봇 경기 돌리기 — 프로토타입 simGame. 보너스는 경기 난수로 무작위, 투구는 봇.
    /// 0단계만 (연습 상대 고정). 1·2단계는 상대 학교도 무작위라 M9 2차에서 연다
    /// </summary>
    public static class BotRunner
    {
        /// <summary>0단계 연습 상대 — 프로토타입 PRACTICE_ID</summary>
        public const string PracticeSchoolId = "solmoe";
        /// <summary>한 경기 최대 단계 (프로토타입 guard 6000) — 무한 반복 안전장치</summary>
        public const int MaxSteps = 6000;

        /// <summary>봇 경기 입력 — 시작 덱 25장 · staR = 튜닝 simStaR · 날씨·컨디션·불펜 굴리기</summary>
        public static MatchSetup Setup(GameDatabase db, int stage, uint seed)
        {
            var f = db.Stages[stage];
            if (!f.practiceOpp) throw new NotSupportedException("봇 경기는 아직 0단계(연습 상대)만 — M9 2차");
            var setup = new MatchSetup
            {
                Stage = stage,
                Opponent = db.FindSchool(PracticeSchoolId),
                Seed = seed,
                StaR = db.Tuning.stamina.simStaR,
                RollConditions = true,
                RollWeather = true,
                RollBullpen = true,
            };
            foreach (var c in db.StarterDeck) setup.Deck.Add(new DeckEntry(c));
            return setup;
        }

        /// <summary>한 경기를 끝까지 돌리고 집계에 더한다. 끝난 엔진을 돌려준다</summary>
        public static MatchEngine Play(GameDatabase db, int stage, uint seed, Bot bot, ref BotStats stats, BotTrace trace = null)
        {
            var e = new MatchEngine(db, Setup(db, stage, seed));
            e.Start();
            int steps = 0, pa = 0;
            while (e.Phase != MatchPhase.Ended && steps++ < MaxSteps)
            {
                if (e.Phase == MatchPhase.BonusPick)
                {
                    int n = e.State.Offers.Count;
                    int k = e.BotRandomIndex(n);   // 프로토타입 pick(offers) — 후보가 없어도 난수 하나를 쓴다
                    trace?.Bonus.Add(k);
                    if (n == 0) throw new InvalidOperationException($"보너스 후보가 없다 (시드 {seed})");
                    e.ChooseBonus(k);
                    continue;
                }
                var cmd = bot.Choose(e);
                trace?.Throws.Add(cmd);
                var r = e.Throw(cmd);
                if (r == null) throw new InvalidOperationException($"봇이 던질 수 없는 공을 골랐다 (시드 {seed}, 손패 {cmd.HandIndex}, 칸 {cmd.Zone})");
                if (r.AtBatEnded) pa++;
            }
            if (e.Phase != MatchPhase.Ended) throw new InvalidOperationException($"봇 경기가 끝나지 않았다 (시드 {seed})");
            stats.Add(e.State, pa);
            return e;
        }

        /// <summary>시드 seedFrom부터 games경기를 돌려 집계한다</summary>
        public static BotStats PlayMany(GameDatabase db, int stage, uint seedFrom, int games, Bot bot)
        {
            var stats = new BotStats();
            for (int g = 0; g < games; g++) Play(db, stage, seedFrom + (uint)g, bot, ref stats);
            return stats;
        }
    }
}
