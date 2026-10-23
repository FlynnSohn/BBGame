using System;
using UnityEngine;

namespace BBGame.Core
{
    /// <summary>타자 유형 — 보스 블라인드 (§11-1) [가설 수치]</summary>
    [CreateAssetMenu(menuName = "BBGame/Data/Batter Type")]
    public sealed class BatterTypeDef : ScriptableObject
    {
        [SerializeField] private BatterKind _kind;
        [Tooltip("기본 목표치 T. 컨택형은 튜닝의 contactT가 덮어쓴다")]
        [SerializeField] private double _baseTarget;
        [Tooltip("대응력 0~1 — 노림 전환 성공·찔렀을 때 인플레이")]
        [SerializeField] private double _adjust;
        [Tooltip("파워 — 장타 확률 배율")]
        [SerializeField] private double _power;
        [Tooltip("유인구 추격 배율 (선구안형 0.55, 그 밖 1)")]
        [SerializeField] private double _chase = 1.0;
        [Tooltip("번트 타순 (작전형)")]
        [SerializeField] private bool _canBunt;
        [Tooltip("코스 노림형 특수 규칙 (같은 좌우 코스 연속이면 배수 ×1 상한)")]
        [SerializeField] private bool _isCourse;
        [Tooltip("노림 가중치 — 순서: 직구 · 변화구 · 몸쪽 · 바깥쪽 · 컨택 · 지켜보기")]
        [SerializeField] private double[] _intentWeights = new double[6];

        public BatterKind Kind { get => _kind; internal set => _kind = value; }
        public double BaseTarget { get => _baseTarget; internal set => _baseTarget = value; }
        public double Adjust { get => _adjust; internal set => _adjust = value; }
        public double Power { get => _power; internal set => _power = value; }
        public double Chase { get => _chase; internal set => _chase = value; }
        public bool CanBunt { get => _canBunt; internal set => _canBunt = value; }
        public bool IsCourse { get => _isCourse; internal set => _isCourse = value; }
        /// <summary>노림 가중치 (번트 제외 6종, Intent 순서)</summary>
        public double[] IntentWeights { get => _intentWeights; internal set => _intentWeights = value; }
    }
}
