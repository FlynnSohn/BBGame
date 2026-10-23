using BBGame.Core;
using NUnit.Framework;

namespace BBGame.Tests
{
    /// <summary>17분할 좌표 (§5)</summary>
    public class ZoneTests
    {
        [Test]
        public void AllZones_RoundTripThroughPrototypeId()
        {
            for (int i = 0; i < Zone.Count; i++)
            {
                var z = new Zone((ZoneId)i);
                Assert.IsTrue(Zone.TryParsePrototype(z.ToPrototypeId(), out var back));
                Assert.AreEqual(z, back);
                Assert.AreEqual(z, Zone.From(z.Col, z.Row, z.Outside));
            }
        }

        [Test]
        public void NineInside_EightOutside_NoOutsideCenter()
        {
            int inside = 0, outside = 0;
            for (int i = 0; i < Zone.Count; i++) { if (new Zone((ZoneId)i).Outside) outside++; else inside++; }
            Assert.AreEqual(9, inside);
            Assert.AreEqual(8, outside);
            Assert.IsFalse(Zone.TryParsePrototype("R:mid-mid", out _));
        }

        [Test]
        public void Corners_AreFour()
        {
            int n = 0;
            for (int i = 0; i < Zone.Count; i++) if (new Zone((ZoneId)i).IsCorner) n++;
            Assert.AreEqual(4, n);
        }

        [Test]
        public void Ring_CoversAllOutsideOnce()
        {
            Assert.AreEqual(8, Zone.Ring.Length);
            for (int i = 0; i < Zone.Ring.Length; i++)
            {
                Assert.IsTrue(new Zone(Zone.Ring[i]).Outside);
                for (int j = i + 1; j < Zone.Ring.Length; j++) Assert.AreNotEqual(Zone.Ring[i], Zone.Ring[j]);
            }
        }
    }
}
