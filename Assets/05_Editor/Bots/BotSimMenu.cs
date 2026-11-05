using System.Diagnostics;
using System.Text;
using BBGame.Bots;
using BBGame.Core;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace BBGame.EditorTools.Bots
{
    /// <summary>
    /// 수치 검토 — 0단계 봇 3종 × 400경기를 돌려 콘솔에 표를 찍는다 (match_m9_bots_plan B5).
    /// 시드가 고정이라 규칙·튜닝을 바꾸기 전후를 같은 경기로 비교할 수 있다. 기준선은 사양 §19-3
    /// </summary>
    public static class BotSimMenu
    {
        private const string k_DatabasePath = "Assets/04_Data/Database.asset";
        private const uint SeedFrom = 9001;   // 비교 테스트·기준선 테스트와 같은 시드
        private const int Games = 400;

        [MenuItem("BBGame/Sim/0단계 봇 400경기", false, 1)]
        private static void Run()
        {
            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>(k_DatabasePath);
            if (db == null) { Debug.LogError("[Sim] Database가 없다 — 먼저 BBGame/Data/프로토타입 v12 데이터 가져오기"); return; }

            var bots = new Bot[] { new RandomBot(db), new BlindBot(db), new OracleBot(db) };
            var sb = new StringBuilder(1024);
            sb.Append("[Sim] 0단계 봇 ").Append(Games).Append("경기 · 시드 ").Append(SeedFrom).Append("~").Append(SeedFrom + Games - 1)
              .Append(" · 기준선(§19-3) 무작위 12.07 · 뒤집지 않음 3.63 · 정답 봇 1.25").AppendLine();
            var clock = Stopwatch.StartNew();
            try
            {
                for (int i = 0; i < bots.Length; i++)
                {
                    EditorUtility.DisplayProgressBar("봇 경기", bots[i].Name, (float)i / bots.Length);
                    var s = BotRunner.PlayMany(db, 0, SeedFrom, Games, bots[i]);
                    s.AppendLine(sb, bots[i].Name);
                }
            }
            finally { EditorUtility.ClearProgressBar(); }
            sb.Append("(").Append(clock.ElapsedMilliseconds).Append("ms)");
            Debug.Log(sb.ToString());
        }
    }
}
