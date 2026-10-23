using System;
using UnityEngine;

namespace BBGame.Core
{
    /// <summary>상대 학교 — 라인업 9명 (§11-1). 학교 이름은 Presentation 표시용 데이터</summary>
    [CreateAssetMenu(menuName = "BBGame/Data/School")]
    public sealed class SchoolDef : ScriptableObject
    {
        [Tooltip("프로토타입 학교 id (hangang · cheongnam · solmoe)")]
        [SerializeField] private string _id;
        [Tooltip("0단계 연습 상대 (솔뫼공고)")]
        [SerializeField] private bool _isPractice;
        [Tooltip("1번부터 9번 타자")]
        [SerializeField] private BatterDef[] _lineup = new BatterDef[9];

        public string Id { get => _id; internal set => _id = value; }
        public bool IsPractice { get => _isPractice; internal set => _isPractice = value; }
        public BatterDef[] Lineup { get => _lineup; internal set => _lineup = value; }
    }
}
