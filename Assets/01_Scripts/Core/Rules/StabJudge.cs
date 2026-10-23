namespace BBGame.Core
{
    /// <summary>
    /// 찌르기 판정 (§6-2) — 실제로 들어간 칸으로 한다 (§11-3).
    /// 존 안: 노림 대응표. 존 밖: 그 노림 때문에 배트가 나올 공이면 찔렀다(유인), 아니면 비켜갔다. 존 밖 걸렸다는 지켜보기뿐.
    /// </summary>
    public static class StabJudge
    {
        /// <param name="fastball">직구 계열(포심·투심)만 true (§4-5)</param>
        public static Stab Judge(Intent intent, Zone z, bool fastball)
        {
            if (!z.Outside)
            {
                switch (intent)
                {
                    case Intent.Fastball: return fastball ? Stab.Bad : Stab.Counter;
                    case Intent.Breaking: return fastball ? Stab.Counter : Stab.Bad;
                    case Intent.Inside: return z.Col == ZoneCol.Out ? Stab.Counter : z.Col == ZoneCol.In ? Stab.Bad : Stab.Neutral;
                    case Intent.Outside: return z.Col == ZoneCol.In ? Stab.Counter : z.Col == ZoneCol.Out ? Stab.Bad : Stab.Neutral;
                    case Intent.Contact: return z.IsCenter ? Stab.Bad : Stab.Neutral;
                    case Intent.Take: return Stab.Counter;   // 공짜 스트라이크
                    case Intent.Bunt: return fastball && z.Row == ZoneRow.Hi ? Stab.Counter : z.Row == ZoneRow.Lo ? Stab.Bad : Stab.Neutral;
                }
                return Stab.Neutral;
            }
            switch (intent)
            {
                case Intent.Fastball: return Stab.Counter;                                  // 닮은 직구 / 타이밍이 앞서는 빠지는 변화구
                case Intent.Breaking: return fastball ? Stab.Neutral : Stab.Counter;        // 존 밖 직구는 늦어서 손이 안 나간다
                case Intent.Inside: return z.Col == ZoneCol.In ? Stab.Counter : Stab.Neutral;
                case Intent.Outside: return z.Col == ZoneCol.Out ? Stab.Counter : Stab.Neutral;
                case Intent.Contact: return Stab.Counter;                                   // 보호 스윙이 따라 나온다
                case Intent.Take: return Stab.Bad;                                          // 타자 뜻대로 볼
                case Intent.Bunt: return fastball && z.Row == ZoneRow.Hi ? Stab.Counter : Stab.Neutral;
            }
            return Stab.Neutral;
        }
    }
}
