using BBGame.Core;
using UnityEngine;

namespace BBGame.Presentation.Data
{
    /// <summary>
    /// 테스트 진입 설정 — 경기 전 화면(M8)이 생기기 전까지 Play를 누르면 이 값으로 경기를 연다 (match_m2_screen_plan 범위 8).
    /// 날씨·컨디션·보너스는 M3에서 굴린다 (지금은 없음)
    /// </summary>
    [CreateAssetMenu(menuName = "BBGame/Presentation/Test Match Setup")]
    public sealed class TestMatchSetup : ScriptableObject
    {
        [SerializeField] internal GameDatabase _database;
        [Tooltip("상대 학교 id (solmoe · hangang · cheongnam)")]
        [SerializeField] internal string _opponentId = "solmoe";
        [Tooltip("단계 0 / 1 / 2 — M2는 0단계만 화면이 맞다")]
        [SerializeField, Range(0, 2)] internal int _stage;
        [Tooltip("0이면 매 경기 무작위. 숫자를 넣으면 같은 경기를 다시 볼 수 있다 (같은 시드 · 같은 입력 = 같은 경기)")]
        [SerializeField] internal uint _seed;
        [Tooltip("육성 값 staR — 스태미나 최대 = 기본 + staR")]
        [SerializeField] internal int _staR = 10;

        public GameDatabase Database => _database;

        /// <summary>경기 설정을 만든다. 시드가 0이면 새로 뽑아 seedUsed로 알려 준다 (콘솔에 남겨 재현용)</summary>
        public MatchSetup Build(out uint seedUsed)
        {
            var school = _database.FindSchool(_opponentId);
            seedUsed = _seed != 0 ? _seed : (uint)Random.Range(1, int.MaxValue);
            var setup = new MatchSetup
            {
                Stage = _stage,
                Opponent = school,
                StaR = _staR,
                Seed = seedUsed,
                IsPracticeGame = school != null && school.IsPractice,
            };
            foreach (var card in _database.StarterDeck) setup.Deck.Add(new DeckEntry(card));
            return setup;
        }
    }
}
