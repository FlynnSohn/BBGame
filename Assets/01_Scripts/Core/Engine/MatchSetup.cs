using System.Collections.Generic;

namespace BBGame.Core
{
    /// <summary>경기 시작 입력 — 화면(경기 전)이나 테스트가 만든다</summary>
    public sealed class MatchSetup
    {
        /// <summary>단계 0 / 1 / 2</summary>
        public int Stage;
        public SchoolDef Opponent;
        /// <summary>경기에 가져갈 덱 (15~25장, 고정 카드 제외)</summary>
        public readonly List<DeckEntry> Deck = new List<DeckEntry>(25);
        /// <summary>육성 값 staR — 스태미나 최대 = staBase + staR</summary>
        public int StaR = 10;
        /// <summary>경기 시드 — 같은 시드·같은 입력이면 같은 경기</summary>
        public uint Seed = 1;
        /// <summary>날씨·컨디션·보너스 — 굴리기를 켜면 엔진이 굴려 덮어쓴다</summary>
        public readonly MatchModifiers Modifiers = new MatchModifiers();
        /// <summary>컨디션 굴리기 (§12-2) — 끄면 Modifiers.Conditions를 그대로 쓴다</summary>
        public bool RollConditions;
        /// <summary>날씨 굴리기 (§12-1) — 끄면 Modifiers.Weather를 그대로 쓴다</summary>
        public bool RollWeather;
        /// <summary>불펜 상태 굴리기 (§12-3) — 끄면 Bullpen을 그대로 쓴다</summary>
        public bool RollBullpen;
        /// <summary>오늘의 불펜 상태 (BonusTable.Bullpen 순서). 비어 있으면 강판 때 이름만 고르고 수비력은 pen.unknown</summary>
        public readonly List<BullpenState> Bullpen = new List<BullpenState>(3);
        /// <summary>연습 경기 — 주심 판정 (문장만 다르다, M4)</summary>
        public bool IsPracticeGame;
    }
}
