using System;

namespace BBGame.Core
{
    /// <summary>
    /// 경기 단위 변동과 이닝 보너스의 현재 값 — 규칙 효과의 입력 (§12, §13).
    /// 굴리기·고르기는 M3. 기본값은 날씨 없음·컨디션 없음·보너스 Lv 0.
    /// </summary>
    public sealed class MatchModifiers
    {
        public Weather Weather;
        public Condition Conditions;
        public Evolution Evolutions;
        private readonly int[] _bonus = new int[14];

        public int Level(BonusId id) => _bonus[(int)id];
        public void SetLevel(BonusId id, int level) => _bonus[(int)id] = level;
        public bool Has(Condition c) => (Conditions & c) != 0;
        public bool Has(Evolution e) => (Evolutions & e) != 0;
    }
}
