using System.IO;
using BBGame.Core;
using Newtonsoft.Json.Linq;

namespace BBGame.Tests
{
    /// <summary>테스트 공용 데이터 — 가져온 데이터 묶음과 프로토타입 기준값</summary>
    internal static class TestData
    {
        public const string DatabasePath = "Assets/04_Data/Database.asset";
        public const string FixturesPath = "Assets/04_Data/TestFixtures/proto_v12_fixtures.json";   // 비공개 데이터 서브모듈 안

        private static GameDatabase s_db;
        private static JObject s_fixtures;

        /// <summary>가져오기 메뉴로 만든 데이터 묶음 (직렬화 왕복까지 확인된다)</summary>
        public static GameDatabase Db
        {
            get
            {
#if UNITY_5_3_OR_NEWER
                if (s_db == null) s_db = UnityEditor.AssetDatabase.LoadAssetAtPath<GameDatabase>(DatabasePath);
#endif
                // Public 저장소만 받은 경우 — 데이터는 비공개 서브모듈(BBGame-Data)에 있다
                if (s_db == null) NUnit.Framework.Assert.Ignore("비공개 데이터 서브모듈(Assets/04_Data)이 없어 건너뛴다");
                return s_db;
            }
            internal set => s_db = value;
        }

        /// <summary>프로토타입 v12 기준값 (Docs/tools/proto_export.js 출력)</summary>
        public static JObject Fixtures
        {
            get
            {
                if (s_fixtures == null && !File.Exists(FixturesPath)) NUnit.Framework.Assert.Ignore("비공개 데이터 서브모듈에 기준값이 없어 건너뛴다");
                return s_fixtures ??= JObject.Parse(File.ReadAllText(FixturesPath));
            }
        }

        /// <summary>시작 덱으로 경기 설정</summary>
        public static MatchSetup Setup(int stage, string school, uint seed, int staR = 10)
        {
            var setup = new MatchSetup { Stage = stage, Opponent = Db.FindSchool(school), Seed = seed, StaR = staR };
            foreach (var c in Db.StarterDeck) setup.Deck.Add(new DeckEntry(c));
            return setup;
        }
    }
}
