using System;
using UnityEngine;

namespace BBGame.Core
{
    /// <summary>
    /// 튜닝 값 — 사양 [가설] 수치를 코드에 박지 않고 모은다 (CLAUDE.md §1, §19-4 CFG_DEFAULT + 프로토타입 코드 안 상수).
    /// 그룹·필드 이름은 가져오기 JSON 키와 같다. 값을 바꾸려면 이 에셋을 고치거나 프로토타입에서 다시 가져온다.
    /// </summary>
    [CreateAssetMenu(menuName = "BBGame/Data/Tuning Config")]
    public sealed class TuningConfig : ScriptableObject
    {
        public TargetTuning target = new TargetTuning();
        public ReadTuning read = new ReadTuning();
        public AddTuning add = new AddTuning();
        public StuffTuning stuff = new StuffTuning();
        public StaminaTuning stamina = new StaminaTuning();
        public ControlTuning control = new ControlTuning();
        public ReactionTuning reaction = new ReactionTuning();
        public InPlayTuning inPlay = new InPlayTuning();
        public RunningTuning running = new RunningTuning();
        public IntentTuning intent = new IntentTuning();
        public MentalTuning mental = new MentalTuning();
        public HandTuning hand = new HandTuning();
        public BenchTuning bench = new BenchTuning();
        public ScoutTuning scout = new ScoutTuning();
        public GameTuning game = new GameTuning();
        public RestTuning rest = new RestTuning();
        public OffenseTuning offense = new OffenseTuning();
        public PenTuning pen = new PenTuning();
    }

    /// <summary>목표치 (§11-1)</summary>
    [Serializable]
    public sealed class TargetTuning
    {
        public double tMul, contactT, risp, twoOutEmpty, afterHit, pressurePerLv, dust, heatLate;
        public int heatFromInning;
        public double aura;
        [Tooltip("선구안형 — 골라낸 볼마다 T +")] public int eyeTakeUp;
    }

    /// <summary>읽기 배수 (§8-4)와 볼 적립 (§8-1)</summary>
    [Serializable]
    public sealed class ReadTuning
    {
        public double readMul, counterMul, neutralMul, badMul, jamBadMul, aceReadMul, boldPerLv;
        public double declHit, declMiss, undeclCounter, stage0Counter, ballKeep;
    }

    /// <summary>가산 (§8-3)과 기본 족보 (§8-5)</summary>
    [Serializable]
    public sealed class AddTuning
    {
        public double countAdd;
        [Header("기본 족보")] public double tunnel, diagonal, speed, speedGap, highLow, inOut, comboPerLv, mixCard;
        [Header("2스트라이크·카드")] public double finisherPerLv, finishCard, riseCard, tempoCard, eephusCard;
        [Header("날씨·보너스")] public double coldInsideFastball, windLow, nightHighFastball, clutchPerLv;
    }

    /// <summary>구위·비용·구속 (§4-2, §4-6, §8-2)</summary>
    [Serializable]
    public sealed class StuffTuning
    {
        public int stuffN, stuffR, stuffS, costN, costR, costS, plusStuff, heavyPerLv, liveFastball, curvePenalty;
        public double veloR, veloS, veloNoise, veloTierDrop;
        public int heatCostAdd;
    }

    /// <summary>스태미나 구간 (§11-2)</summary>
    [Serializable]
    public sealed class StaminaTuning
    {
        public int staBase, simStaR;
        [Tooltip("구간 경계 — 남은 비율")] public double tier1, tier2, tier3;
        [Tooltip("구간별 구위 감소")] public double stuffDrop1, stuffDrop2, stuffDrop3;
        [Tooltip("구간별 흔들림 가산")] public double shake1, shake2, shake3;
        [Tooltip("철완 — 구간 페널티 배율")] public double ironHalf;
        public double condHeavyPct, pacePct, paceMaxPct;
    }

    /// <summary>제구 — 흔들림과 착지 (§11-3)</summary>
    [Serializable]
    public sealed class ControlTuning
    {
        public double shakeBase, mentalCenter, mentalSlope, controlPerLv, sGradeBonus, coldBreaking, wetSGrade;
        public double powerShake, quickShake, edgeRelief, powerHang, cornerShake, shakeMin, shakeMax, fixedShakeMul;
        [Tooltip("흔들렸을 때 한가운데로 몰릴 확률")] public double centerPull;
        [Tooltip("존 안 공이 흔들려 같은 자리의 존 밖으로 빠질 확률")] public double insideToOutside;
        [Tooltip("존 밖 공이 흔들려 같은 자리의 존 안으로 들어올 확률")] public double outsideToInside;
        [Tooltip("가운데 보고 던지기 — 한가운데 확률")] public double midLandingCenter;
        [Tooltip("전력투구 — 한가운데 / 한가운데+옆 4칸 누적")] public double powerCenter, powerNear;
    }

    /// <summary>타자 반응 (§9) — 가중치는 순서가 결과를 바꾼다 (코드가 프로토타입 순서로 읽는다)</summary>
    [Serializable]
    public sealed class ReactionTuning
    {
        [Header("존 밖 추격")]
        public double chaseBase, lureChase, timingChase, awayChase, ctChase, takeChase;
        public double twoStrikeChaseAdd, hotChaseAdd, dropChaseAdd, hitRunChaseAdd;
        public double chasedWhiff, chasedFoul, chasedInPlay, dirtTwoStrikeWhiff;
        [Header("존 안 — 지켜보기 노림")]
        public double takeLook, takeFoul, takeInPlay;
        [Header("존 안 — 찔렀다")]
        public double counterWhiff, counterLook, counterFoul, counterInPlay, ipCounterBase, ipCounterAdj, ipAdjustPivot, ipMax;
        [Header("존 안 — 비켜갔다 · 걸렸다")]
        public double neutralFoul, neutralInPlay, neutralLook, neutralWhiff, badInPlay, badFoul;
        [Header("보정")]
        public double contactWhiffShift, firstPassLook, firstAggLook, twoStrikeLook, centerInPlay;
        public double highFastballWhiff, lowCurveWhiff, sameSliderWhiff, oppChangeupWhiff, backdoorLook;
        public double veloWhiff, veloClamp, runPlayLook, tier3Whiff;
        [Header("번트 노림 (존 안)")]
        public double buntCounterFoul, buntCounterWhiff, buntCounterInPlay, buntCounterLook, buntOtherInPlay, buntOtherFoul, buntOtherLook;
    }

    /// <summary>인플레이 (§10-2)</summary>
    [Serializable]
    public sealed class InPlayTuning
    {
        public double groundLow, groundMid, groundHigh, groundMin, groundMax;
        public double hitCounter, hitNeutral, hitBad, hitChase, hitDamp, bloop, bloopCounterMul;
        public double powerBadHit, tier3BadHit, centerHit, weakPerLv, zoneAmp, wetGround, veloHit, hitMax;
        public double xbhBad, xbhNeutral, xbhCounter, xbhDamp, windHighXbh, tier3Xbh, midXbh, eephusXbh, xbhMax;
        public double hrFastHigh, hrOther, hrPowerCap, triple, buntHitFull, buntHitBase, buntPop;
    }

    /// <summary>주루 진루 확률 (§10-3) — Go는 1루 주자가 스타트를 끊었을 때 (2단계)</summary>
    [Serializable]
    public sealed class RunningTuning
    {
        public double doublePlay, doublePlayGo, dpThirdScores, sacFly, groundThirdScores, groundSecondAdvance, groundSecondAdvanceGo;
        public double singleSecondScores, singleSecondScoresGo, singleFirstToThird, singleFirstToThirdGo, doubleFirstScores, doubleFirstScoresGo, flyDoublePlayGo;
    }

    /// <summary>노림 가중치 (§6-3)와 전환 비용 (§6-4)</summary>
    [Serializable]
    public sealed class IntentTuning
    {
        public double typeK, typeJitter, buntW;
        public int closeDiff;
        public double rispThirdContact, rispThirdBreaking, twoOutRispFastball, twoOutRispContact, twoOutEmptyContact;
        public double twoStrikeContact, twoStrikeContactCourse, twoStrikeOther, aheadFastball, aheadContact, aheadTake;
        public double threeBallTake0, threeBallTake1, firstAggFastball, firstAggTake, firstPassTake, sprayFactor, sprayMin, sprayMax;
        [Header("전환 비용")]
        public double costTake, costBunt, costToContact, costFromContact, costTimingFlip, costCourseFlip, costOther, switchFactor, switchMin;
    }

    /// <summary>멘탈 (§11-2)</summary>
    [Serializable]
    public sealed class MentalTuning
    {
        public int start, min, max, dominant, strikeout, homeRun, extraBase, hit, read, benchVisit;
    }

    /// <summary>손패·집중력 (§4-1, §11-2)</summary>
    [Serializable]
    public sealed class HandTuning
    {
        public int handSize, stiffMinus, stiffUntilInning, focusMax, focusRegen, breathPerLv, clearFocus;
    }

    /// <summary>벤치 판단·투구 수 (§22)</summary>
    [Serializable]
    public sealed class BenchTuning
    {
        public int inningRuns, hitStreak, walkStreak, pitchLimit;
    }

    /// <summary>진짜 경향 스위치 (§11-1c) — 엔진은 모든 단계에서 쓴다</summary>
    [Serializable]
    public sealed class ScoutTuning
    {
        public bool scout, zone, spray, pitch, count, risp;
    }

    /// <summary>경기 규칙</summary>
    [Serializable]
    public sealed class GameTuning
    {
        public int innings;
        [Tooltip("스태미나 구간 3 피로 강화")] public bool fatigueHard;
    }

    /// <summary>투구 수 → 의무 휴식일 (§22-4 [가설: 고교야구 투구 수 규정]). 프로토타입 REST·restWarn</summary>
    [Serializable]
    public sealed class RestTuning
    {
        [Tooltip("휴식 구간 상한 — i번째 값 이하로 던지면 휴식 i일, 마지막 값을 넘으면 (개수)일")]
        public int[] maxPitches = { 30, 45, 60, 75 };
        [Tooltip("휴식 구간 경계·투구 수 한도 몇 구 전부터 경고하나")]
        public int warnBefore = 5;
    }

    /// <summary>
    /// 반 이닝 결과 시뮬레이션 — 우리 공격·불펜 이닝 (§14 [임시 수치]).
    /// 무득점 확률 = clamp(zeroBase − (공격력 − 수비력) × zeroSlope, zeroMin, zeroMax), 득점이면 점수 분포
    /// </summary>
    [Serializable]
    public sealed class OffenseTuning
    {
        [Tooltip("우리 공격력 (육성으로 보강 예정)")] public double teamOff = 50;
        [Tooltip("상대 투수 수비력 · 상대 공격력 (프로토타입 코드 안 상수 50)")] public double oppTeam = 50;
        public double zeroBase = 0.70, zeroSlope = 0.006, zeroMin = 0.45, zeroMax = 0.88;
        [Tooltip("득점 분포 누적 경계 — r < [0] 1점 · < [1] 2점 · < [2] 3점 · < [3] 4점 · 그 밖 5점 (55·25·12·5·3%)")]
        public double[] runCumulative = { 0.55, 0.8, 0.92, 0.97 };
    }

    /// <summary>오늘의 불펜 (§12-3, §14)</summary>
    [Serializable]
    public sealed class PenTuning
    {
        [Tooltip("상태별 수비력 — 좋음 · 보통 · 나쁨")] public double good = 55, mid = 47, bad = 40;
        [Tooltip("불펜 목록이 없을 때 (테스트·M1 기록)")] public double unknown = 45;
        [Tooltip("이닝마다 수비력 감소")] public double decayPerInning = 3;
    }
}
