using System;
using UnityEngine;

namespace BBGame.Core
{
    /// <summary>시즌 기록 — 진짜 경향에서 시드 고정으로 뽑은 값. 판정에는 쓰지 않는다 (§11-1c 정보 단계 0)</summary>
    [Serializable]
    public struct SeasonRecord
    {
        public int atBats, hits, homeRuns, rbi;
        public double Average => atBats > 0 ? (double)hits / atBats : 0;
    }

    /// <summary>
    /// 상대 타자 (§11-1, §11-1c). 화면 이름·포지션은 Presentation 표시용 데이터가 연결한다 (D6).
    /// 진짜 경향은 엔진이 모든 단계에서 쓰고, 화면 공개는 2단계부터다.
    /// </summary>
    [CreateAssetMenu(menuName = "BBGame/Data/Batter")]
    public sealed class BatterDef : ScriptableObject
    {
        [Tooltip("이름 시드 문자열 = 한국어 이름. 노림 편차를 이 값으로 만든다. 바꾸면 그 타자의 숫자가 바뀐다")]
        [SerializeField] private string _seedKey;
        [SerializeField] private Hand _hand;
        [SerializeField] private BatterTypeDef _type;
        [Tooltip("학년 1~3. 0 = 미정")]
        [SerializeField, Range(0, 3)] private int _grade;

        [Header("진짜 경향 [가설]")]
        [Tooltip("타구 방향 — 당김 · 가운데 · 밀어")]
        [SerializeField] private double _pull, _center, _oppo;
        [SerializeField] private double _fastballBias = 1.0;
        [SerializeField] private double _breakingBias = 1.0;
        [SerializeField] private FirstPitchStyle _firstPitch;
        [Tooltip("2스트라이크 이후 컨택 전환 배율")]
        [SerializeField] private double _twoStrikeContact;
        [Tooltip("2스트라이크 이후 존 밖 스윙 가감")]
        [SerializeField] private double _twoStrikeChase;
        [SerializeField] private double _rispFactor = 1.0;
        [Tooltip("존별 안타 배율 (핫/콜드) — ZoneId 순서 17칸")]
        [SerializeField] private double[] _zoneHit = new double[Zone.Count];
        [Tooltip("볼 칸별 추격 배율 — ZoneId 순서 17칸, 존 안은 0")]
        [SerializeField] private double[] _zoneChase = new double[Zone.Count];
        [SerializeField] private SeasonRecord _season;

        public string SeedKey { get => _seedKey; internal set => _seedKey = value; }
        public Hand Hand { get => _hand; internal set => _hand = value; }
        public BatterTypeDef Type { get => _type; internal set => _type = value; }
        public int Grade { get => _grade; internal set => _grade = value; }
        public double Pull { get => _pull; internal set => _pull = value; }
        public double Center { get => _center; internal set => _center = value; }
        public double Oppo { get => _oppo; internal set => _oppo = value; }
        public double FastballBias { get => _fastballBias; internal set => _fastballBias = value; }
        public double BreakingBias { get => _breakingBias; internal set => _breakingBias = value; }
        public FirstPitchStyle FirstPitch { get => _firstPitch; internal set => _firstPitch = value; }
        public double TwoStrikeContact { get => _twoStrikeContact; internal set => _twoStrikeContact = value; }
        public double TwoStrikeChase { get => _twoStrikeChase; internal set => _twoStrikeChase = value; }
        public double RispFactor { get => _rispFactor; internal set => _rispFactor = value; }
        public double[] ZoneHit { get => _zoneHit; internal set => _zoneHit = value; }
        public double[] ZoneChase { get => _zoneChase; internal set => _zoneChase = value; }
        public SeasonRecord Season { get => _season; internal set => _season = value; }
    }
}
