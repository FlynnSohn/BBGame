using BBGame.Core;
using UnityEngine;

namespace BBGame.Presentation.Data
{
    /// <summary>
    /// 테스트 진입 설정 — 경기 전 화면(M8)이 생기기 전까지 Play를 누르면 이 값으로 경기를 연다 (match_m2_screen_plan 범위 8).
    /// 날씨·컨디션·불펜은 엔진이 굴린다 (끄면 없음)
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
        [Header("경기 단위 변동 (§12)")]
        [Tooltip("날씨·컨디션·불펜 상태를 굴린다 (경기 전 화면 M8이 생기기 전까지 여기서)")]
        [SerializeField] internal bool _rollModifiers = true;
        [Header("개발용 (에디터·개발 빌드에서만)")]
        [Tooltip("자동 진행 — 손패에서 카드를 골라 던지고, [다음]·보너스 첫 칸·안내를 스스로 누른다. 결과 창에서 멈춘다")]
        [SerializeField] internal bool _autoPlay;
        [Tooltip("자동 진행 한 단계 간격 (초)")]
        [SerializeField, Range(0.02f, 2f)] internal float _autoPlayDelay = 0.25f;

        public bool AutoPlay => _autoPlay;
        public float AutoPlayDelay => _autoPlayDelay;

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
                RollConditions = _rollModifiers,
                RollWeather = _rollModifiers,
                RollBullpen = _rollModifiers,
            };
            foreach (var card in _database.StarterDeck) setup.Deck.Add(new DeckEntry(card));
            return setup;
        }
    }
}
