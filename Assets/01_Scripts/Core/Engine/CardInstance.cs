namespace BBGame.Core
{
    /// <summary>덱 안의 카드 한 장 — 같은 정의라도 강화 단계가 다를 수 있다. 경기 시작 때만 만든다</summary>
    public sealed class CardInstance
    {
        public readonly CardDef Def;
        /// <summary>강화 단계 0~2 (§15)</summary>
        public readonly int Plus;

        public CardInstance(CardDef def, int plus) { Def = def; Plus = plus; }
    }

    /// <summary>경기에 가져갈 덱 한 줄</summary>
    public readonly struct DeckEntry
    {
        public readonly CardDef Card;
        public readonly int Plus;
        public DeckEntry(CardDef card, int plus = 0) { Card = card; Plus = plus; }
    }
}
