using UnityEngine;

namespace BBGame.Core
{
    /// <summary>
    /// 단계 기능 스위치 (§24 STAGE_FEATURES). 단계가 기본값을 정하고, 수치 검토에서 기능별로 덮어쓸 수 있다.
    /// 필드 이름은 프로토타입 키와 같다 (가져오기 JSON 키).
    /// </summary>
    [CreateAssetMenu(menuName = "BBGame/Data/Stage Features")]
    public sealed class StageFeatures : ScriptableObject
    {
        [Range(0, 2)] public int stage;
        [Tooltip("노림 선언")] public bool declare;
        [Tooltip("단서 카드")] public bool clues;
        [Tooltip("집중력")] public bool focusOn;
        [Tooltip("사인 교환")] public bool sign;
        [Tooltip("표현도·기질")] public bool persona;
        [Tooltip("버릇·연기")] public bool personaTell;
        [Tooltip("스카우팅 정보·단서")] public bool scoutInfo;
        [Tooltip("고급 족보")] public bool advCombo;
        [Tooltip("야간 조명·황사")] public bool wxClue;
        [Tooltip("연습 상대(솔뫼공고)")] public bool practiceOpp;
        [Tooltip("타자 단순화 — 노림 가중치 거듭제곱")] public double sitAmp = 1.0;
        [Tooltip("단계 목표치 배율")] public double tStageMul = 1.0;
        [Tooltip("중복 단서 (노림 묶음)")] public bool overlap;
        [Tooltip("기묘한 족보")] public bool oddCombo;
        [Tooltip("주자 작전·견제·퀵모션")] public bool runners;
        [Tooltip("몸에 맞는 공")] public bool hbp;
        [Tooltip("폭투")] public bool wp;
        [Tooltip("구속 반응")] public bool veloFx;
    }
}
