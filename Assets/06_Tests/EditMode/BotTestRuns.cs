using System.Collections.Generic;
using BBGame.Bots;

namespace BBGame.Tests
{
    /// <summary>봇 400경기 결과를 테스트끼리 나눠 쓴다 (같은 시드·같은 봇이면 결과가 같다 — 두 번 돌리지 않는다)</summary>
    internal static class BotTestRuns
    {
        private static readonly Dictionary<string, BotStats> s_cache = new Dictionary<string, BotStats>();

        public static Bot Make(string key)
        {
            var db = TestData.Db;
            switch (key)
            {
                case "random": return new RandomBot(db);
                case "blind": return new BlindBot(db);
                case "oracle": return new OracleBot(db);
            }
            throw new KeyNotFoundException("모르는 봇: " + key);
        }

        public static BotStats Get(string key, uint seedFrom, int games)
        {
            string k = key + "@" + seedFrom + "x" + games;
            if (!s_cache.TryGetValue(k, out var s))
            {
                s = BotRunner.PlayMany(TestData.Db, 0, seedFrom, games, Make(key));
                s_cache[k] = s;
            }
            return s;
        }
    }
}
