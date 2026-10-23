namespace BBGame.Core
{
    /// <summary>가산 칩 종류 — 화면은 이 값으로 문구를 고른다 (§20-4 가산 내역을 칩으로)</summary>
    public enum AddChipKind : byte
    {
        Count, Combo, Finisher, FinishCard, RiseCard, TempoCard, EephusCard,
        ColdInsideFastball, WindLow, NightHighFastball, Clutch
    }

    /// <summary>가산 칩 한 개</summary>
    public readonly struct AddChip
    {
        public readonly AddChipKind Kind;
        public readonly double Value;
        /// <summary>Kind가 Combo일 때 족보 종류</summary>
        public readonly ComboKind Combo;
        public AddChip(AddChipKind kind, double value, ComboKind combo = ComboKind.None) { Kind = kind; Value = value; Combo = combo; }
    }

    /// <summary>
    /// 던지기 전 미리보기. 읽기 배수 필드가 아예 없다 — "던지기 전에는 읽기 배수를 공개하지 않는다"를 타입으로 지킨다 (§8-1, §20-4).
    /// </summary>
    public readonly struct PitchPreview
    {
        /// <summary>미리보기를 만들 수 있었는가 (단계·카드·칸이 맞지 않으면 false)</summary>
        public readonly bool Valid;
        /// <summary>구위 (보너스·컨디션·스태미나 구간 반영, 최소 1)</summary>
        public readonly int Stuff;
        /// <summary>스태미나 구간 구위 배율 (화면 칩용)</summary>
        public readonly double TierStuffMul;
        /// <summary>화면에 보여 줄 가산 합 (발견하지 않은 고급·기묘한 족보 제외 — M7)</summary>
        public readonly double AddShown;
        /// <summary>화면에 보여 줄 기본 제압치 = round(구위 × (1 + 보이는 가산))</summary>
        public readonly int BaseShown;
        /// <summary>실제 기본 제압치 (계산용 — 숨은 족보 포함)</summary>
        internal readonly int BaseActual;
        internal readonly double AddTotal;

        internal PitchPreview(int stuff, double tierMul, double addTotal, double addShown, int baseActual, int baseShown)
        {
            Valid = true; Stuff = stuff; TierStuffMul = tierMul; AddTotal = addTotal; AddShown = addShown; BaseActual = baseActual; BaseShown = baseShown;
        }
    }

    /// <summary>투구 명령. 선언은 1단계부터 쓰인다 (0단계는 null)</summary>
    public readonly struct ThrowCommand
    {
        public readonly int HandIndex;
        /// <summary>던질 칸. 전력투구 직구는 null (엔진이 착지를 정한다)</summary>
        public readonly ZoneId? Zone;
        /// <summary>노림 선언 (M6). 던질 때 확정된다</summary>
        public readonly Intent? Declared;

        public ThrowCommand(int handIndex, ZoneId? zone, Intent? declared = null) { HandIndex = handIndex; Zone = zone; Declared = declared; }
    }
}
