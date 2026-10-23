namespace BBGame.Core
{
    /// <summary>주자 한 명. 프로토타입 주자 객체 { name, spd, err }에서 0단계가 쓰는 것만</summary>
    public readonly struct Runner
    {
        /// <summary>그 루에 주자가 있는가</summary>
        public readonly bool On;
        /// <summary>라인업 번호 (0~8)</summary>
        public readonly byte Slot;
        /// <summary>실책(견제 악송구)으로 진루했나 — 득점하면 비자책 (§23-3)</summary>
        public readonly bool ByError;

        public Runner(byte slot, bool byError = false) { On = true; Slot = slot; ByError = byError; }

        public static readonly Runner Empty = default;
    }

    /// <summary>베이스 상태 스냅샷 — 1루·2루·3루 점유 비트 (리빌 전후 표시용)</summary>
    public readonly struct BaseMask
    {
        public readonly byte Bits;
        public BaseMask(bool first, bool second, bool third) { Bits = (byte)((first ? 1 : 0) | (second ? 2 : 0) | (third ? 4 : 0)); }
        public bool First => (Bits & 1) != 0;
        public bool Second => (Bits & 2) != 0;
        public bool Third => (Bits & 4) != 0;
    }

    /// <summary>볼카운트 스냅샷</summary>
    public readonly struct Count
    {
        public readonly byte Balls, Strikes;
        public Count(int balls, int strikes) { Balls = (byte)balls; Strikes = (byte)strikes; }
    }
}
