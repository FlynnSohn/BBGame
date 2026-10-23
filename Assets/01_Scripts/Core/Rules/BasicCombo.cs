using System;

namespace BBGame.Core
{
    /// <summary>기본 족보 5종 (§8-5) — 0단계부터 공개</summary>
    public enum ComboKind : byte { None, Tunnel, Diagonal, Speed, HighLow, InOut }

    /// <summary>족보 판정에 쓰는 공 정보 — 직전 공(실제 칸)과 이번 공</summary>
    public readonly struct ComboPitch
    {
        public readonly Zone Zone;
        public readonly PitchType Type;
        /// <summary>직전 공은 화면 구속, 이번 공은 기대 구속 (±1.5 없음) — 미리보기 족보가 던진 뒤 사라지지 않게 (§4-6)</summary>
        public readonly double Velo;
        /// <summary>전력투구 직구 — 칸이 정해지지 않아 칸 족보를 만들지 않는다</summary>
        public readonly bool AnyZone;
        public ComboPitch(Zone zone, PitchType type, double velo, bool anyZone) { Zone = zone; Type = type; Velo = velo; AnyZone = anyZone; }
    }

    public static class BasicCombo
    {
        /// <summary>성립한 기본 족보 중 하나 (프로토타입 우선순위: 터널 → 대각 → 완급 → 높낮이 → 좌우)</summary>
        public static ComboKind Of(AddTuning t, in ComboPitch prev, in ComboPitch cur, out double value)
        {
            var p = prev.Zone;
            var c = cur.Zone;
            bool rowOpp = (p.Row == ZoneRow.Hi && c.Row == ZoneRow.Lo) || (p.Row == ZoneRow.Lo && c.Row == ZoneRow.Hi);
            bool colOpp = (p.Col == ZoneCol.In && c.Col == ZoneCol.Out) || (p.Col == ZoneCol.Out && c.Col == ZoneCol.In);
            bool speed = Math.Abs(prev.Velo - cur.Velo) >= t.speedGap;

            if (!cur.AnyZone && p.Col == c.Col && p.Row == c.Row && prev.Type != cur.Type) { value = t.tunnel; return ComboKind.Tunnel; }
            if (!cur.AnyZone && rowOpp && colOpp) { value = t.diagonal; return ComboKind.Diagonal; }
            if (speed) { value = t.speed; return ComboKind.Speed; }
            if (!cur.AnyZone && rowOpp) { value = t.highLow; return ComboKind.HighLow; }
            if (!cur.AnyZone && colOpp) { value = t.inOut; return ComboKind.InOut; }
            value = 0;
            return ComboKind.None;
        }
    }
}
