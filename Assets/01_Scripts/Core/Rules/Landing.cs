namespace BBGame.Core
{
    /// <summary>착지 — 특수 카드 착지와 제구 미스 칸 (§11-3). 흔들린 공은 반드시 다른 칸으로 간다</summary>
    public static class Landing
    {
        // 한가운데 바로 옆 4칸 (프로토타입 MID_NEAR 순서)
        private static readonly ZoneId[] s_midNear = { ZoneId.InMid, ZoneId.OutMid, ZoneId.MidHi, ZoneId.MidLo };
        // 존 안 모서리 4칸 (프로토타입 powerLanding 순서)
        private static readonly ZoneId[] s_corners = { ZoneId.InHi, ZoneId.OutHi, ZoneId.InLo, ZoneId.OutLo };

        /// <summary>가운데 보고 던지기 — 한가운데 40% · 옆 4칸 60%</summary>
        public static Zone MidLanding(ControlTuning t, ref SeededRandom rng)
            => rng.NextDouble() < t.midLandingCenter ? new Zone(ZoneId.MidMid) : new Zone(s_midNear[rng.NextIndex(4)]);

        /// <summary>전력투구 직구 — 한가운데 20% · 옆 4칸 각 12.5% · 모서리 4칸 각 7.5%</summary>
        public static Zone PowerLanding(ControlTuning t, ref SeededRandom rng)
        {
            double x = rng.NextDouble();
            if (x < t.powerCenter) return new Zone(ZoneId.MidMid);
            if (x < t.powerNear) return new Zone(s_midNear[rng.NextIndex(4)]);
            return new Zone(s_corners[rng.NextIndex(4)]);
        }

        /// <summary>
        /// 제구 미스의 인접 칸 (v11) — 의도와 반드시 다르다.
        /// 존 밖: 같은 자리의 존 안 / 고리를 따라 옆 칸. 존 안: 같은 자리의 존 밖 / 존 안 옆 칸(끝이면 반대로 꺾는다).
        /// </summary>
        public static Zone Neighbor(ControlTuning t, ref SeededRandom rng, Zone z)
        {
            if (z.Outside)
            {
                if (rng.NextDouble() < t.outsideToInside) return Zone.From(z.Col, z.Row, false);
                int i = IndexInRing(z.Id);
                int step = rng.NextDouble() < 0.5 ? 1 : Zone.Ring.Length - 1;
                return new Zone(Zone.Ring[(i + step) % Zone.Ring.Length]);
            }
            if (!z.IsCenter && rng.NextDouble() < t.insideToOutside) return Zone.From(z.Col, z.Row, true);
            bool alongCol = rng.NextDouble() < 0.5;
            int idx = alongCol ? (int)z.Col : (int)z.Row;
            int d = rng.NextDouble() < 0.5 ? -1 : 1;
            if (idx + d < 0 || idx + d > 2) d = -d;
            return alongCol
                ? Zone.From((ZoneCol)(idx + d), z.Row, false)
                : Zone.From(z.Col, (ZoneRow)(idx + d), false);
        }

        /// <summary>존 안에서만 옆 칸 (가운데 보고 던지기의 제구 미스 — 존 밖으로 빠지지 않는다)</summary>
        public static Zone NeighborIn(ref SeededRandom rng, Zone z)
        {
            // 후보 순서는 프로토타입 neighborIn과 같다: 왼쪽 열, 오른쪽 열, 위 행, 아래 행
            System.Span<int> c = stackalloc int[4];
            int n = 0;
            int ci = (int)z.Col, ri = (int)z.Row;
            if (ci > 0) c[n++] = (int)Zone.From((ZoneCol)(ci - 1), z.Row, false).Id;
            if (ci < 2) c[n++] = (int)Zone.From((ZoneCol)(ci + 1), z.Row, false).Id;
            if (ri > 0) c[n++] = (int)Zone.From(z.Col, (ZoneRow)(ri - 1), false).Id;
            if (ri < 2) c[n++] = (int)Zone.From(z.Col, (ZoneRow)(ri + 1), false).Id;
            return new Zone((ZoneId)c[rng.NextIndex(n)]);
        }

        private static int IndexInRing(ZoneId id)
        {
            for (int i = 0; i < Zone.Ring.Length; i++) if (Zone.Ring[i] == id) return i;
            return 0;
        }
    }
}
