using BBGame.Core;

namespace BBGame.Presentation.Match
{
    /// <summary>
    /// 투수 시점 칸 배치 (§5-2, match_loop_plan 7-2). 화면 왼쪽 = 1루 쪽 = 좌타석.
    /// 우타자는 화면 오른쪽 열이 몸쪽, 좌타자는 화면 왼쪽 열이 몸쪽이다.
    /// 화면 칸은 ZoneId와 같은 이름을 쓰되 열을 "화면 기준"으로 읽는다 — In = 화면 왼쪽, Out = 화면 오른쪽
    /// </summary>
    public static class PitcherView
    {
        /// <summary>화면 열(0 = 왼쪽, 1 = 가운데, 2 = 오른쪽) → 타자 기준 열</summary>
        public static ZoneCol ColOfVisual(int visualCol, Hand batterHand)
            => batterHand == Hand.Right
                ? (visualCol == 0 ? ZoneCol.Out : visualCol == 1 ? ZoneCol.Mid : ZoneCol.In)
                : (visualCol == 0 ? ZoneCol.In : visualCol == 1 ? ZoneCol.Mid : ZoneCol.Out);

        /// <summary>화면 칸 → 논리 칸 (판정에 쓰는 타자 기준 칸)</summary>
        public static ZoneId ZoneOfVisual(ZoneId visual, Hand batterHand)
        {
            var v = new Zone(visual);
            return Zone.From(ColOfVisual((int)v.Col, batterHand), v.Row, v.Outside).Id;
        }

        /// <summary>논리 칸 → 화면 칸. 좌우만 뒤집으므로 같은 변환을 한 번 더 하면 된다</summary>
        public static ZoneId VisualOfZone(ZoneId logical, Hand batterHand) => ZoneOfVisual(logical, batterHand);
    }
}
