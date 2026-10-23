using System;
using UnityEngine;

namespace BBGame.Core
{
    /// <summary>이닝 보너스 한 종 (§13)</summary>
    [Serializable]
    public struct BonusRow
    {
        public BonusId id;
        [Tooltip("최대 레벨")] public int maxLevel;
        [Tooltip("그 단계에 이 기능이 없으면 후보에서 뺀다")] public FeatureNeed need;
        [Tooltip("화면 문구용 — 레벨당 효과 수치 (규칙 효과는 TuningConfig가 정한다)")] public double perLevel;
    }

    /// <summary>진화 한 종 — 두 보너스의 레벨 조건 (§13)</summary>
    [Serializable]
    public struct EvolutionRow
    {
        public Evolution evolution;
        public BonusId first;
        public int firstLevel;
        public BonusId second;
        public int secondLevel;
        public FeatureNeed need;

        public bool Ready(MatchModifiers m) => m.Level(first) >= firstLevel && m.Level(second) >= secondLevel;
    }

    /// <summary>
    /// 이닝 보너스·진화 표와 우리 팀 이름(시드) — 프로토타입 v12 BONUS·BONUS_NEED·EVOS·OUR_BATS·OUR_PEN에서 가져온다.
    /// 화면 이름·효과 문구는 Presentation 표시용 데이터 (D6)
    /// </summary>
    [CreateAssetMenu(menuName = "BBGame/Data/Bonus Table")]
    public sealed class BonusTable : ScriptableObject
    {
        [Tooltip("BonusId 순서 14종 — 후보 뽑기는 이 순서로 가중치를 쌓는다 (프로토타입 키 순서)")]
        [SerializeField] private BonusRow[] _bonuses = Array.Empty<BonusRow>();
        [Tooltip("진화 4종 — 조건을 만족한 것 중 앞의 것이 첫 칸")]
        [SerializeField] private EvolutionRow[] _evolutions = Array.Empty<EvolutionRow>();
        [Tooltip("우리 타자 9명 시드 이름 — 우리 공격 중계 문구의 이름")]
        [SerializeField] private string[] _ourBatters = Array.Empty<string>();
        [Tooltip("불펜 3명 시드 이름 (§12-3)")]
        [SerializeField] private string[] _bullpen = Array.Empty<string>();

        public BonusRow[] Bonuses { get => _bonuses; internal set => _bonuses = value; }
        public EvolutionRow[] Evolutions { get => _evolutions; internal set => _evolutions = value; }
        public string[] OurBatters { get => _ourBatters; internal set => _ourBatters = value; }
        public string[] Bullpen { get => _bullpen; internal set => _bullpen = value; }

        public BonusRow Get(BonusId id)
        {
            for (int i = 0; i < _bonuses.Length; i++) if (_bonuses[i].id == id) return _bonuses[i];
            return default;
        }

        /// <summary>그 단계에 이 기능이 있는가</summary>
        public static bool Has(StageFeatures f, FeatureNeed need) => need switch
        {
            FeatureNeed.Sign => f.sign,
            FeatureNeed.Focus => f.focusOn,
            FeatureNeed.Clues => f.clues,
            FeatureNeed.Declare => f.declare,
            _ => true,
        };
    }
}
