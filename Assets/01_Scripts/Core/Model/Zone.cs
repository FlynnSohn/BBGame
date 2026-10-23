using System;

namespace BBGame.Core
{
    /// <summary>타자 기준 좌우 열 — 몸쪽 / 가운데 / 바깥쪽 (§5-1)</summary>
    public enum ZoneCol : byte { In, Mid, Out }

    /// <summary>높낮이. 노림의 축이 아니다 — 타구 질과 일부 헛스윙 보정만 정한다 (§5-1)</summary>
    public enum ZoneRow : byte { Hi, Mid, Lo }

    /// <summary>
    /// 17분할 존의 칸 번호. 순서는 프로토타입 ZONES17과 같다 (데이터 배열 인덱스로 쓴다).
    /// O 접두 = 존 밖. 존 밖 한가운데 칸은 없다.
    /// </summary>
    public enum ZoneId : byte
    {
        InHi, MidHi, OutHi, InMid, MidMid, OutMid, InLo, MidLo, OutLo,
        OInHi, OMidHi, OOutHi, OInMid, OOutMid, OInLo, OMidLo, OOutLo
    }

    /// <summary>17분할 존의 한 칸 (§5). 값 타입, 할당 없음</summary>
    public readonly struct Zone : IEquatable<Zone>
    {
        /// <summary>칸 수 (존 안 9 + 존 밖 8)</summary>
        public const int Count = 17;

        public readonly ZoneId Id;

        public Zone(ZoneId id) { Id = id; }

        public ZoneCol Col => s_col[(int)Id];
        public ZoneRow Row => s_row[(int)Id];
        /// <summary>존 밖이면 true</summary>
        public bool Outside => Id >= ZoneId.OInHi;
        /// <summary>모서리 칸 — 존 안이고 좌우·높낮이가 모두 가운데가 아님. 흔들림 가산 대상 (§11-3)</summary>
        public bool IsCorner => !Outside && Col != ZoneCol.Mid && Row != ZoneRow.Mid;
        /// <summary>한가운데 (mid-mid)</summary>
        public bool IsCenter => Id == ZoneId.MidMid;

        /// <summary>열·행·존 밖 여부로 칸을 만든다. 존 밖 한가운데는 없으므로 예외</summary>
        public static Zone From(ZoneCol col, ZoneRow row, bool outside)
        {
            if (!outside) return new Zone((ZoneId)((int)row * 3 + (int)col));
            for (int i = 9; i < Count; i++)
                if (s_col[i] == col && s_row[i] == row) return new Zone((ZoneId)i);
            throw new ArgumentException("존 밖 한가운데 칸은 없다");
        }

        // 칸 번호 → 열·행 (ZoneId 순서)
        private static readonly ZoneCol[] s_col =
        {
            ZoneCol.In, ZoneCol.Mid, ZoneCol.Out, ZoneCol.In, ZoneCol.Mid, ZoneCol.Out, ZoneCol.In, ZoneCol.Mid, ZoneCol.Out,
            ZoneCol.In, ZoneCol.Mid, ZoneCol.Out, ZoneCol.In, ZoneCol.Out, ZoneCol.In, ZoneCol.Mid, ZoneCol.Out
        };
        private static readonly ZoneRow[] s_row =
        {
            ZoneRow.Hi, ZoneRow.Hi, ZoneRow.Hi, ZoneRow.Mid, ZoneRow.Mid, ZoneRow.Mid, ZoneRow.Lo, ZoneRow.Lo, ZoneRow.Lo,
            ZoneRow.Hi, ZoneRow.Hi, ZoneRow.Hi, ZoneRow.Mid, ZoneRow.Mid, ZoneRow.Lo, ZoneRow.Lo, ZoneRow.Lo
        };

        /// <summary>
        /// 존 밖 고리 — 몸쪽 높게 → 가운데 높게 → 바깥쪽 높게 → 바깥쪽 → 바깥쪽 낮게 → 가운데 낮게 → 몸쪽 낮게 → 몸쪽 (§5-1, 프로토타입 RING)
        /// </summary>
        public static readonly ZoneId[] Ring =
        {
            ZoneId.OInHi, ZoneId.OMidHi, ZoneId.OOutHi, ZoneId.OOutMid, ZoneId.OOutLo, ZoneId.OMidLo, ZoneId.OInLo, ZoneId.OInMid
        };

        /// <summary>프로토타입 칸 이름("in-hi", "R:out-lo")을 읽는다 — 데이터 가져오기·테스트용</summary>
        public static bool TryParsePrototype(string s, out Zone zone)
        {
            zone = default;
            if (string.IsNullOrEmpty(s)) return false;
            bool outside = s.StartsWith("R:", StringComparison.Ordinal);
            string body = outside ? s.Substring(2) : s;
            int dash = body.IndexOf('-');
            if (dash < 0) return false;
            if (!TryCol(body.Substring(0, dash), out var col) || !TryRow(body.Substring(dash + 1), out var row)) return false;
            if (outside && col == ZoneCol.Mid && row == ZoneRow.Mid) return false;
            zone = From(col, row, outside);
            return true;
        }

        /// <summary>프로토타입 칸 이름으로 — 테스트·로그용 (할당 있음)</summary>
        public string ToPrototypeId()
        {
            string c = Col == ZoneCol.In ? "in" : Col == ZoneCol.Mid ? "mid" : "out";
            string r = Row == ZoneRow.Hi ? "hi" : Row == ZoneRow.Mid ? "mid" : "lo";
            return (Outside ? "R:" : "") + c + "-" + r;
        }

        private static bool TryCol(string s, out ZoneCol c)
        {
            c = s == "in" ? ZoneCol.In : s == "mid" ? ZoneCol.Mid : ZoneCol.Out;
            return s == "in" || s == "mid" || s == "out";
        }

        private static bool TryRow(string s, out ZoneRow r)
        {
            r = s == "hi" ? ZoneRow.Hi : s == "mid" ? ZoneRow.Mid : ZoneRow.Lo;
            return s == "hi" || s == "mid" || s == "lo";
        }

        public bool Equals(Zone other) => Id == other.Id;
        public override bool Equals(object obj) => obj is Zone z && Equals(z);
        public override int GetHashCode() => (int)Id;
        public static bool operator ==(Zone a, Zone b) => a.Id == b.Id;
        public static bool operator !=(Zone a, Zone b) => a.Id != b.Id;
        public override string ToString() => ToPrototypeId();
    }
}
