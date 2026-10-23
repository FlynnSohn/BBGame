namespace BBGame.Core
{
    /// <summary>이닝 보너스 후보 한 칸 (§13)</summary>
    public readonly struct BonusOffer
    {
        public readonly OfferKind Kind;
        /// <summary>Kind가 Bonus일 때</summary>
        public readonly BonusId Bonus;
        /// <summary>Kind가 Evolution일 때 (플래그 하나)</summary>
        public readonly Evolution Evolution;

        public BonusOffer(BonusId bonus) { Kind = OfferKind.Bonus; Bonus = bonus; Evolution = Evolution.None; }
        public BonusOffer(Evolution evolution) { Kind = OfferKind.Evolution; Bonus = default; Evolution = evolution; }
    }

    /// <summary>결과만 계산한 반 이닝 종류</summary>
    public enum HalfKind : byte
    {
        /// <summary>우리 공격 (회말)</summary>
        UsOffense,
        /// <summary>강판한 이닝의 남은 아웃을 불펜이 맡았다 (회초)</summary>
        BullpenFinish,
        /// <summary>불펜이 던진 다음 이닝 (회초)</summary>
        BullpenInning
    }

    /// <summary>
    /// 결과만 계산한 반 이닝 한 줄 (§14). 문장은 고르지 않고 번호만 — Presentation이 문구 표에서 고른다.
    /// 이름: NameA = names[A], NameB = (A를 뺀 목록)[B] (프로토타입 offenseLine의 n · m)
    /// </summary>
    public readonly struct HalfInningRecord
    {
        public readonly HalfKind Kind;
        public readonly int Inning;
        public readonly int Runs;
        /// <summary>우리 팀 안타 — 지금은 늘 0 (사용자 결정 2026-10-24, 나중에 보완)</summary>
        public readonly int Hits;
        public readonly byte NameA, NameB;
        /// <summary>점수별 문구 번호 (4점 이상은 0)</summary>
        public readonly byte Line;
        /// <summary>불펜 이닝이면 올라간 투수 (BonusTable.Bullpen 번호, 목록이 없으면 이름 번호)</summary>
        public readonly byte Pitcher;

        public HalfInningRecord(HalfKind kind, int inning, int runs, int nameA, int nameB, int line, int pitcher)
        {
            Kind = kind; Inning = inning; Runs = runs; Hits = 0;
            NameA = (byte)nameA; NameB = (byte)nameB; Line = (byte)line; Pitcher = (byte)pitcher;
        }
    }

    /// <summary>실점 장부 한 줄 — 득점을 순서대로 (§23-2). 결정 기록 판정은 M8</summary>
    public readonly struct LedgerEntry
    {
        /// <summary>true = 우리 득점, false = 상대 득점</summary>
        public readonly bool Us;
        public readonly int Runs;
        /// <summary>상대 득점이 주인공 책임인가 (남긴 주자 포함)</summary>
        public readonly bool Charged;
        /// <summary>우리 득점일 때 — 그 회초를 마친 투수</summary>
        public readonly PitcherOfRecord PitcherOfRecord;

        public LedgerEntry(bool us, int runs, bool charged, PitcherOfRecord por) { Us = us; Runs = runs; Charged = charged; PitcherOfRecord = por; }
    }

    /// <summary>오늘 투수 기록 한 줄 (§15) — 이닝 · 투구 · 피안타 · 실점 · 자책 · 삼진 · 4사구</summary>
    public readonly struct PitcherLine
    {
        public readonly int OutsPitched;
        public readonly int Pitches;
        public readonly int Hits;
        public readonly int Runs;
        public readonly int EarnedRuns;
        public readonly int Strikeouts;
        public readonly int Walks;

        public PitcherLine(int outs, int pitches, int hits, int runs, int earned, int strikeouts, int walks)
        {
            OutsPitched = outs; Pitches = pitches; Hits = hits; Runs = runs; EarnedRuns = earned; Strikeouts = strikeouts; Walks = walks;
        }
    }
}
