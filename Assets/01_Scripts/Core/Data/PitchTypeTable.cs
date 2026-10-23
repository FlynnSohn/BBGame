using System;
using UnityEngine;

namespace BBGame.Core
{
    /// <summary>구종 한 줄 — 계열·기준 구속·반응 보정 (§4-5) [가설 수치]</summary>
    [Serializable]
    public sealed class PitchTypeRow
    {
        public PitchType type;
        public PitchFamily family;
        [Tooltip("족보 조건에서 읽는 이름 (§4-5 Z1)")]
        public PitchLike like;
        [Tooltip("기준 구속 km/h")]
        public double baseVelo;
        [Tooltip("해금 없이 카드로만 나오는 구종 (아리랑볼)")]
        public bool cardOnly;
        [Header("반응 보정 — 1 = 보정 없음, 0 = 가산 없음")]
        public double groundBonus;
        public double whiffMul = 1.0;
        public double whiffSameMul = 1.0;
        public double whiffOppMul = 1.0;
        public double hitOppMul = 1.0;
        public double jamOppMul = 1.0;
        public double whiffLowMul = 1.0;
        public double shakeAdd;

        /// <summary>노림 판정에서 직구로 보는가 — 직구 계열만 (§4-5)</summary>
        public bool IsFastball => family == PitchFamily.Fastball;
    }

    /// <summary>구종 표 — 11종. PitchType 순서로 찾는다</summary>
    [CreateAssetMenu(menuName = "BBGame/Data/Pitch Type Table")]
    public sealed class PitchTypeTable : ScriptableObject
    {
        [SerializeField] private PitchTypeRow[] _rows = Array.Empty<PitchTypeRow>();

        /// <summary>구종 한 줄. 표는 PitchType 순서로 정렬돼 있어야 한다 (가져오기가 맞춘다)</summary>
        public PitchTypeRow Get(PitchType type) => _rows[(int)type];

        public int Count => _rows.Length;

        internal PitchTypeRow[] Rows { get => _rows; set => _rows = value; }
    }
}
