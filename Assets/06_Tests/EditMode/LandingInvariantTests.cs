using BBGame.Core;
using NUnit.Framework;

namespace BBGame.Tests
{
    /// <summary>제구 불변식 (§11-3, v11) — 흔들린 공은 반드시 다른 칸, 가운데 보고 던지기는 존 안</summary>
    public class LandingInvariantTests
    {
        private const int N = 100000;

        [Test]
        public void Neighbor_NeverSameZone()
        {
            var t = TestData.Db.Tuning.control;
            var r = new SeededRandom(42);
            for (int i = 0; i < N; i++)
            {
                var z = new Zone((ZoneId)(i % Zone.Count));
                Assert.AreNotEqual(z, Landing.Neighbor(t, ref r, z));
            }
        }

        [Test]
        public void NeighborIn_StaysInsideAndMoves()
        {
            var r = new SeededRandom(7);
            for (int i = 0; i < N; i++)
            {
                var z = new Zone((ZoneId)(i % 9));
                var n = Landing.NeighborIn(ref r, z);
                Assert.IsFalse(n.Outside);
                Assert.AreNotEqual(z, n);
            }
        }

        [Test]
        public void SpecialLandings_AreInside()
        {
            var t = TestData.Db.Tuning.control;
            var r = new SeededRandom(9);
            for (int i = 0; i < N; i++)
            {
                Assert.IsFalse(Landing.PowerLanding(t, ref r).Outside);
                Assert.IsFalse(Landing.MidLanding(t, ref r).Outside);
            }
        }
    }
}
