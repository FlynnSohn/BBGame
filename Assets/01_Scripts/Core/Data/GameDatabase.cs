using System;
using UnityEngine;

namespace BBGame.Core
{
    /// <summary>엔진이 쓰는 데이터 묶음 — 가져오기 메뉴가 만든다 (Assets/04_Data/Database.asset)</summary>
    [CreateAssetMenu(menuName = "BBGame/Data/Game Database")]
    public sealed class GameDatabase : ScriptableObject
    {
        [SerializeField] private PitchTypeTable _pitchTypes;
        [SerializeField] private TuningConfig _tuning;
        [Tooltip("단계 0 / 1 / 2")]
        [SerializeField] private StageFeatures[] _stages = new StageFeatures[3];
        [SerializeField] private BatterTypeDef[] _batterTypes = Array.Empty<BatterTypeDef>();
        [SerializeField] private CardDef[] _cards = Array.Empty<CardDef>();
        [SerializeField] private SchoolDef[] _schools = Array.Empty<SchoolDef>();
        [Tooltip("시작 보유 카드 25장 (같은 카드 여러 번)")]
        [SerializeField] private CardDef[] _starterDeck = Array.Empty<CardDef>();
        [Tooltip("손패 고정 카드 — 가운데 보고 던지기")]
        [SerializeField] private CardDef _fixedCard;
        [Tooltip("컨디션 「손끝 감각이 좋다」로 들어오는 경기 한정 카드")]
        [SerializeField] private CardDef _todayCard;
        [Tooltip("이닝 보너스·진화 표, 우리 타자·불펜 이름")]
        [SerializeField] private BonusTable _bonusTable;

        public PitchTypeTable PitchTypes { get => _pitchTypes; internal set => _pitchTypes = value; }
        public TuningConfig Tuning { get => _tuning; internal set => _tuning = value; }
        public StageFeatures[] Stages { get => _stages; internal set => _stages = value; }
        public BatterTypeDef[] BatterTypes { get => _batterTypes; internal set => _batterTypes = value; }
        public CardDef[] Cards { get => _cards; internal set => _cards = value; }
        public SchoolDef[] Schools { get => _schools; internal set => _schools = value; }
        public CardDef[] StarterDeck { get => _starterDeck; internal set => _starterDeck = value; }
        public CardDef FixedCard { get => _fixedCard; internal set => _fixedCard = value; }
        public CardDef TodayCard { get => _todayCard; internal set => _todayCard = value; }
        public BonusTable BonusTable { get => _bonusTable; internal set => _bonusTable = value; }

        /// <summary>프로토타입 id로 카드 찾기 (없으면 null)</summary>
        public CardDef FindCard(string id)
        {
            foreach (var c in _cards) if (c != null && c.Id == id) return c;
            return null;
        }

        /// <summary>프로토타입 id로 학교 찾기 (없으면 null)</summary>
        public SchoolDef FindSchool(string id)
        {
            foreach (var s in _schools) if (s != null && s.Id == id) return s;
            return null;
        }
    }
}
