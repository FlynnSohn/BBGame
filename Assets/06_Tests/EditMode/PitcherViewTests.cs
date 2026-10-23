using BBGame.Core;
using BBGame.Presentation.Match;
using NUnit.Framework;

namespace BBGame.Tests
{
    /// <summary>투수 시점 칸 배치 (§5-2) — 화면 왼쪽 = 1루 쪽 = 좌타석</summary>
    public sealed class PitcherViewTests
    {
        [Test]
        public void RightHandedBatter_RightColumnIsInside()
        {
            Assert.AreEqual(ZoneCol.Out, PitcherView.ColOfVisual(0, Hand.Right), "우타자 — 화면 왼쪽은 바깥쪽");
            Assert.AreEqual(ZoneCol.Mid, PitcherView.ColOfVisual(1, Hand.Right));
            Assert.AreEqual(ZoneCol.In, PitcherView.ColOfVisual(2, Hand.Right), "우타자 — 화면 오른쪽은 몸쪽");
        }

        [Test]
        public void LeftHandedBatter_LeftColumnIsInside()
        {
            Assert.AreEqual(ZoneCol.In, PitcherView.ColOfVisual(0, Hand.Left), "좌타자 — 화면 왼쪽은 몸쪽");
            Assert.AreEqual(ZoneCol.Out, PitcherView.ColOfVisual(2, Hand.Left));
        }

        [TestCase(ZoneId.InHi, Hand.Right, ZoneId.OutHi)]
        [TestCase(ZoneId.OutLo, Hand.Right, ZoneId.InLo)]
        [TestCase(ZoneId.MidMid, Hand.Right, ZoneId.MidMid)]
        [TestCase(ZoneId.OInMid, Hand.Right, ZoneId.OOutMid)]
        [TestCase(ZoneId.OMidLo, Hand.Right, ZoneId.OMidLo)]
        [TestCase(ZoneId.InHi, Hand.Left, ZoneId.InHi)]
        [TestCase(ZoneId.OOutHi, Hand.Left, ZoneId.OOutHi)]
        public void ZoneOfVisual_MapsByBatterHand(ZoneId visual, Hand hand, ZoneId expected)
        {
            Assert.AreEqual(expected, PitcherView.ZoneOfVisual(visual, hand));
        }

        [Test]
        public void ZoneOfVisual_IsItsOwnInverse_AndKeepsRowAndOutside()
        {
            foreach (Hand hand in new[] { Hand.Left, Hand.Right })
                for (int i = 0; i < Zone.Count; i++)
                {
                    var visual = (ZoneId)i;
                    var logical = PitcherView.ZoneOfVisual(visual, hand);
                    Assert.AreEqual(visual, PitcherView.VisualOfZone(logical, hand));
                    Assert.AreEqual(new Zone(visual).Row, new Zone(logical).Row);
                    Assert.AreEqual(new Zone(visual).Outside, new Zone(logical).Outside);
                }
        }
    }
}
