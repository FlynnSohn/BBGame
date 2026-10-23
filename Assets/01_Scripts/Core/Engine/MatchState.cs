using System.Collections.Generic;

namespace BBGame.Core
{
    /// <summary>직전 타석 결과 (등장 중계·목표치 보정용)</summary>
    public enum LastResult : byte { None, Hit, Strikeout, Walk, Out }

    /// <summary>라인업 한 자리의 경기 중 상태</summary>
    public sealed class BatterState
    {
        public readonly BatterDef Def;
        /// <summary>라인업 번호 0~8</summary>
        public readonly int Slot;
        /// <summary>유형 기본 목표치 (컨택형은 튜닝 contactT)</summary>
        public readonly double BaseTarget;
        /// <summary>노림 기본 가중치 — Intent 순서 6칸 (이름 시드, 경기 시작 때 한 번)</summary>
        public readonly double[] BaseWeights = new double[6];
        public int Faced;
        public bool LastHit;
        public LastResult LastResult;
        /// <summary>특수 규칙(특징) 공개 — 한 타석을 상대하면 열린다</summary>
        public bool RuleKnown;

        public BatterState(BatterDef def, int slot, double baseTarget) { Def = def; Slot = slot; BaseTarget = baseTarget; }
    }

    /// <summary>이번 타석에 던진 공 한 개 — 실제 칸 기준 (족보·직전 공·마운드 점, v11)</summary>
    public struct PitchRecord
    {
        public int Number;
        public Zone Actual;
        public Zone Intended;
        public bool Shaken;
        public PitchType Type;
        public bool Fastball;
        public int Velo;
        public Stab Stab;
        public PitchOutcome Outcome;
        public bool Power;
        public bool Fixed;
        public bool Strike;
    }

    /// <summary>현재 타석</summary>
    public sealed class AtBatState
    {
        public BatterState Batter;
        public int Balls, Strikes;
        public int Gauge;
        public int Target;
        public TargetNote TargetNotes;
        /// <summary>이번 공 노림 (히든 — 리빌 전에는 화면에 내보내지 않는다)</summary>
        public Intent Intent;
        public Intent? PrevIntent;
        public bool HasIntent;
        /// <summary>노림을 바꾸려다 몸이 따라오지 못했다 (§6-4)</summary>
        public bool SwitchFailed;
        /// <summary>목표치에 한 번이라도 도달했다</summary>
        public bool Reached;
        /// <summary>이번 타석 존 밖 헛스윙 수 (낚시꾼 족보, M7)</summary>
        public int ChaseWhiffs;
        public bool Was30;
        /// <summary>이번 타석 투구 기록 (미리 잡은 목록 재사용)</summary>
        public readonly List<PitchRecord> Pitches = new List<PitchRecord>(16);

        public bool HasPrev => Pitches.Count > 0;
        public PitchRecord Prev => Pitches[Pitches.Count - 1];

        internal void Reset(BatterState batter)
        {
            Batter = batter;
            Balls = Strikes = Gauge = Target = 0;
            TargetNotes = TargetNote.None;
            Intent = default; PrevIntent = null; HasIntent = false;
            SwitchFailed = Reached = Was30 = false;
            ChaseWhiffs = 0;
            Pitches.Clear();
        }
    }

    /// <summary>덱·버림 더미·손패</summary>
    public sealed class DeckState
    {
        public readonly List<CardInstance> Draw = new List<CardInstance>(32);
        public readonly List<CardInstance> Discard = new List<CardInstance>(32);
        public readonly List<CardInstance> Hand = new List<CardInstance>(9);
        /// <summary>손패 고정 카드 — 가운데 보고 던지기 (덱 밖)</summary>
        public CardInstance Fixed;
    }

    /// <summary>경기 상태 — 엔진만 고친다. 화면은 IMatchReadOnly로 읽는다</summary>
    public sealed class MatchState : IMatchReadOnly
    {
        public MatchPhase Phase { get; internal set; }
        public int Stage { get; internal set; }
        public int Inning { get; internal set; } = 1;
        public int Outs { get; internal set; }
        internal readonly Runner[] Bases = new Runner[3];
        public int ScoreOpp { get; internal set; }
        public int ScoreUs { get; internal set; }
        internal int[] LineOpp;
        internal int[] LineUs;
        public int HitsOpp { get; internal set; }
        public int HitsUs { get; internal set; }
        public int ErrorsUs { get; internal set; }
        internal BatterState[] Lineup;
        public int BatterIndex { get; internal set; }
        internal readonly DeckState Deck = new DeckState();
        public int Stamina { get; internal set; }
        public int StaminaMax { get; internal set; }
        /// <summary>경기 시작 최대치 (페이스 조절의 기준)</summary>
        public int StaminaMaxAtStart { get; internal set; }
        public int Focus { get; internal set; }
        public int FocusMax { get; internal set; }
        public int Mental { get; internal set; }
        public int Pitches { get; internal set; }
        /// <summary>이번 이닝 주인공 실점 (벤치 판단)</summary>
        public int InningRuns { get; internal set; }
        public int RunsAllowed { get; internal set; }
        public int EarnedRuns { get; internal set; }
        internal int HitStreak, WalkStreak;
        internal int BenchVisitInning = -1, BenchVisitRuns;
        public ExitReason ExitReason { get; internal set; }
        /// <summary>강판한 이닝·그때 아웃 수 (투구 이닝 계산)</summary>
        public int ExitInning { get; internal set; }
        public int ExitOuts { get; internal set; }
        /// <summary>강판 때 남긴 주자 중 들어온 수 (주인공 실점에 들어갔다)</summary>
        public int InheritedScored { get; internal set; }
        public int Strikeouts { get; internal set; }
        /// <summary>4사구 (볼넷 + 몸에 맞는 공)</summary>
        public int Walks { get; internal set; }
        public MatchResult Result { get; internal set; }
        /// <summary>강판 뒤 올라간 불펜 (BonusTable.Bullpen 번호, 없으면 −1)</summary>
        public int RelieverIndex { get; internal set; } = -1;
        internal readonly AtBatState AtBat = new AtBatState();
        internal readonly List<BonusOffer> OffersList = new List<BonusOffer>(3);
        internal readonly List<HalfInningRecord> HalfLogList = new List<HalfInningRecord>(20);
        internal readonly List<LedgerEntry> LedgerList = new List<LedgerEntry>(24);
        internal readonly List<BullpenState> BullpenList = new List<BullpenState>(3);
        internal MatchModifiers Modifiers;

        // ── IMatchReadOnly ──
        public Runner GetBase(int index) => Bases[index];
        public int GetLineOpp(int inningIndex) => LineOpp[inningIndex];
        public int GetLineUs(int inningIndex) => LineUs[inningIndex];
        public int LineInnings => LineOpp.Length;
        public BatterState GetBatter(int slot) => Lineup[slot];
        public BatterState CurrentBatter => AtBat.Batter;
        public int Balls => AtBat.Balls;
        public int Strikes => AtBat.Strikes;
        public int Gauge => AtBat.Gauge;
        public int Target => AtBat.Target;
        public TargetNote TargetNotes => AtBat.TargetNotes;
        public IReadOnlyList<PitchRecord> AtBatPitches => AtBat.Pitches;
        public IReadOnlyList<CardInstance> Hand => Deck.Hand;
        public int DrawCount => Deck.Draw.Count;
        public int DiscardCount => Deck.Discard.Count;
        public Weather Weather => Modifiers.Weather;
        public Condition Conditions => Modifiers.Conditions;
        public int BonusLevel(BonusId id) => Modifiers.Level(id);
        public Evolution Evolutions => Modifiers.Evolutions;
        public IReadOnlyList<BonusOffer> Offers => OffersList;
        public IReadOnlyList<HalfInningRecord> HalfLog => HalfLogList;
        public IReadOnlyList<LedgerEntry> Ledger => LedgerList;
        public IReadOnlyList<BullpenState> Bullpen => BullpenList;
        /// <summary>우리 팀 라인스코어 칸이 X(9회말 공격 없음)인가</summary>
        public bool IsLineUsX(int inningIndex) => LineUs[inningIndex] < 0;
        /// <summary>그 이닝 말 공격을 했는가 (화면 — 아직 안 한 칸은 비운다)</summary>
        public bool HasLineUs(int inningIndex) => inningIndex < LineUsPlayed;
        internal int LineUsPlayed;

        public PitcherLine Line
        {
            get
            {
                int outs = ExitReason != ExitReason.None ? (ExitInning - 1) * 3 + ExitOuts
                    : Phase == MatchPhase.Ended ? LineOpp.Length * 3 : (Inning - 1) * 3 + Outs;
                return new PitcherLine(outs, Pitches, HitsOpp, RunsAllowed, EarnedRuns, Strikeouts, Walks);
            }
        }
    }

    /// <summary>화면이 읽는 경기 상태 — 고칠 수 없다. 타자의 이번 공 노림은 리빌 전에는 노출하지 않는다 (§20-4)</summary>
    public interface IMatchReadOnly
    {
        MatchPhase Phase { get; }
        int Stage { get; }
        int Inning { get; }
        int Outs { get; }
        Runner GetBase(int index);
        int ScoreOpp { get; }
        int ScoreUs { get; }
        int GetLineOpp(int inningIndex);
        int GetLineUs(int inningIndex);
        int LineInnings { get; }
        int HitsOpp { get; }
        int HitsUs { get; }
        int ErrorsUs { get; }
        BatterState GetBatter(int slot);
        int BatterIndex { get; }
        BatterState CurrentBatter { get; }
        int Balls { get; }
        int Strikes { get; }
        int Gauge { get; }
        int Target { get; }
        TargetNote TargetNotes { get; }
        IReadOnlyList<PitchRecord> AtBatPitches { get; }
        IReadOnlyList<CardInstance> Hand { get; }
        int DrawCount { get; }
        int DiscardCount { get; }
        int Stamina { get; }
        int StaminaMax { get; }
        int Focus { get; }
        int FocusMax { get; }
        int Mental { get; }
        int Pitches { get; }
        ExitReason ExitReason { get; }
        Weather Weather { get; }
        Condition Conditions { get; }
        int BonusLevel(BonusId id);
        Evolution Evolutions { get; }
        /// <summary>이닝 보너스 후보 (BonusPick 단계에서만 채워진다)</summary>
        IReadOnlyList<BonusOffer> Offers { get; }
        /// <summary>결과만 계산한 반 이닝 (우리 공격·불펜) — 순서대로</summary>
        IReadOnlyList<HalfInningRecord> HalfLog { get; }
        /// <summary>실점 장부 (§23-2)</summary>
        IReadOnlyList<LedgerEntry> Ledger { get; }
        /// <summary>오늘의 불펜 상태 (비어 있을 수 있다)</summary>
        IReadOnlyList<BullpenState> Bullpen { get; }
        int RelieverIndex { get; }
        int InheritedScored { get; }
        int ExitInning { get; }
        int ExitOuts { get; }
        int Strikeouts { get; }
        int Walks { get; }
        bool IsLineUsX(int inningIndex);
        bool HasLineUs(int inningIndex);
        MatchResult Result { get; }
        /// <summary>오늘 투수 기록 한 줄</summary>
        PitcherLine Line { get; }
    }
}
