using System;
using UnityEngine;

namespace BBGame.Core
{
    /// <summary>
    /// 구종 카드 정의 (§4-2). 이름·설명은 Presentation의 표시용 데이터가 Id로 연결한다 (D6).
    /// </summary>
    [CreateAssetMenu(menuName = "BBGame/Data/Card")]
    public sealed class CardDef : ScriptableObject
    {
        [Tooltip("프로토타입 카드 id (예: s_sl). 가져오기·표시용 데이터 연결 키")]
        [SerializeField] private string _id;
        [SerializeField] private PitchType _pitchType;
        [SerializeField] private Grade _grade;
        [Tooltip("던질 수 있는 칸. S등급은 2칸. 전력투구 직구는 비운다")]
        [SerializeField] private ZoneId[] _zones = Array.Empty<ZoneId>();
        [SerializeField] private CardSpecial _special;
        [Tooltip("전력투구 직구 — 코스를 고르지 않고 존 안 9칸에 가운데 가중으로 떨어진다 (§11-3)")]
        [SerializeField] private bool _isPowerPitch;
        [Tooltip("가운데 보고 던지기 — 손패에 고정, 덱 밖 (§4-1)")]
        [SerializeField] private bool _isFixed;
        [Tooltip("경기 한정 카드 (오늘의 직구)")]
        [SerializeField] private bool _isTemporary;
        [Header("카드별 보정 [가설]")]
        [SerializeField] private int _stuffAdjust;
        [SerializeField] private int _costAdjust;
        [SerializeField] private int _veloAdjust;

        public string Id { get => _id; internal set => _id = value; }
        public PitchType PitchType { get => _pitchType; internal set => _pitchType = value; }
        public Grade Grade { get => _grade; internal set => _grade = value; }
        /// <summary>던질 수 있는 칸 (읽기 전용으로 쓴다 — 배열을 고치지 않는다)</summary>
        public ZoneId[] Zones { get => _zones; internal set => _zones = value; }
        public CardSpecial Special { get => _special; internal set => _special = value; }
        public bool IsPowerPitch { get => _isPowerPitch; internal set => _isPowerPitch = value; }
        public bool IsFixed { get => _isFixed; internal set => _isFixed = value; }
        public bool IsTemporary { get => _isTemporary; internal set => _isTemporary = value; }
        public int StuffAdjust { get => _stuffAdjust; internal set => _stuffAdjust = value; }
        public int CostAdjust { get => _costAdjust; internal set => _costAdjust = value; }
        public int VeloAdjust { get => _veloAdjust; internal set => _veloAdjust = value; }

        /// <summary>이 카드로 그 칸에 던질 수 있는가 (전력투구 직구는 칸을 고르지 않는다)</summary>
        public bool CanTarget(ZoneId zone)
        {
            for (int i = 0; i < _zones.Length; i++)
                if (_zones[i] == zone) return true;
            return false;
        }
    }
}
