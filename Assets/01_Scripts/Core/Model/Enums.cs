using System;

namespace BBGame.Core
{
    /// <summary>타석·투구 손</summary>
    public enum Hand : byte { Left, Right }

    /// <summary>
    /// 타자의 노림 7종 (§6-1). 프로토타입 키: FB · BR · IN · OUT · CT · TAKE · BUNT (같은 순서)
    /// "직구"는 직구 계열(포심·투심)만이다
    /// </summary>
    public enum Intent : byte { Fastball, Breaking, Inside, Outside, Contact, Take, Bunt }

    /// <summary>
    /// 찌르기 판정. 내부 용어 역(+1) / 무관(0) / 걸림(−1).
    /// 화면 용어는 찔렀다 / 비켜갔다 / 걸렸다 — String Table에서만 쓴다 (§6-2, §20-5)
    /// </summary>
    public enum Stab : sbyte { Bad = -1, Neutral = 0, Counter = 1 }

    /// <summary>구종 11종 (§4-5). 순서는 프로토타입 PT 키 순서 (FF FT SL ST FC CU EP CH SP FO SI)</summary>
    public enum PitchType : byte { FourSeam, TwoSeam, Slider, Sweeper, Cutter, Curve, Eephus, Changeup, Splitter, Forkball, Sinker }

    /// <summary>구종 계열 3개 (§4-5) — 노림 판정은 Fastball만 직구로 본다</summary>
    public enum PitchFamily : byte { Fastball, Breaking, Changeup }

    /// <summary>족보 조건에서 읽는 이름 (§4-5 v12 Z1) — 직구 / 슬라이더형 / 커브형 / 체인지업 계열</summary>
    public enum PitchLike : byte { Fastball, Slider, Curve, Changeup }

    /// <summary>카드 등급</summary>
    public enum Grade : byte { N, R, S }

    /// <summary>카드 특수 효과 (§4-3)</summary>
    public enum CardSpecial : byte { None, Rise, Mix, Drop, Tempo, Jam, Ace, Finish, Bite, Eephus, Dirt, Backdoor }

    /// <summary>타자 유형 — 보스 블라인드 (§11-1)</summary>
    public enum BatterKind : byte { Contact, Power, Course, Eye, Small }

    /// <summary>초구 성향 — 진짜 경향 (§11-1c)</summary>
    public enum FirstPitchStyle : byte { Aggressive, Normal, Passive }

    /// <summary>타자 반응 (§9)</summary>
    public enum Reaction : byte { Ball, Look, Whiff, Foul, InPlay, Hbp }

    /// <summary>한 공의 결과 태그 (프로토타입 ev.res)</summary>
    public enum PitchOutcome : byte
    {
        Ball, Walk, Hbp, Look, Whiff, Foul, Cut, Strikeout, Dominant, BuntStrikeout,
        Single, Double, Triple, HomeRun, Sacrifice, GroundOut, FlyOut, DoublePlay
    }

    /// <summary>날씨 — 이득 하나·손해 하나 (§12-1). None = 효과 없음</summary>
    public enum Weather : byte { None, Cold, Heat, Wind, Wet, Night, Dust }

    /// <summary>컨디션 — 불펜 투구에서 공개 (§12-2). 둘을 함께 갖는다</summary>
    [Flags]
    public enum Condition : byte
    {
        None = 0,
        Feel = 1 << 0,   // 손끝 감각이 좋다 — 오늘의 직구 1장
        Live = 1 << 1,   // 직구가 살아 있다 — 직구 계열 구위 +3
        Clear = 1 << 2,  // 머리가 맑다 — 집중력 최대 +10
        Curve = 1 << 3,  // 커브 각이 안 잡힌다 — 커브형 구위 −5
        Heavy = 1 << 4,  // 어깨가 무겁다 — 스태미나 최대 −10%
        Stiff = 1 << 5,  // 몸이 덜 풀렸다 — 1~2회 손패 −1
    }

    /// <summary>이닝 보너스 14종 (§13). 순서는 프로토타입 BONUS 키 순서</summary>
    public enum BonusId : byte { Heavy, Finisher, Trust, Breath, Eyes, Sharp, Pressure, Clutch, Control, Grip, Combo, Pace, Weak, Bold }

    /// <summary>진화 4종 (§13)</summary>
    [Flags]
    public enum Evolution : byte
    {
        None = 0,
        Rising = 1 << 0,  // 라이징 패스트볼
        Mind = 1 << 1,    // 독심술
        Aura = 1 << 2,    // 에이스의 기백
        Iron = 1 << 3,    // 철완
    }

    /// <summary>경기 진행 단계 (프로토타입 phase)</summary>
    public enum MatchPhase : byte
    {
        /// <summary>시작 전</summary>
        NotStarted,
        /// <summary>투구 대기</summary>
        Pitching,
        /// <summary>아웃 3개 — 반 이닝 종료 (M1: AdvanceHalfInning 대기, M3: 3택1·우리 공격)</summary>
        HalfInningOver,
        /// <summary>강판 — 스태미나 0 · 투구 수 한도 · 벤치 판단 (M3에서 불펜 처리)</summary>
        PitcherExited,
        /// <summary>경기 끝</summary>
        Ended
    }

    /// <summary>강판 이유</summary>
    public enum ExitReason : byte { None, Stamina, PitchLimit, Bench, Request }
}
