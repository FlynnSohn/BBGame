using BBGame.Core;
using NUnit.Framework;

namespace BBGame.Tests
{
    /// <summary>시드 난수·이름 해시·JS 반올림 — 프로토타입과 같은 값 (§21)</summary>
    public class RandomTests
    {
        [Test]
        public void Mulberry32_MatchesPrototype()
        {
            var r = new SeededRandom(12345);
            Assert.AreEqual(0.9797282678, r.NextDouble(), 1e-10);
            Assert.AreEqual(0.3067522645, r.NextDouble(), 1e-10);
            Assert.AreEqual(0.4842054215, r.NextDouble(), 1e-10);
        }

        [Test]
        public void Fnv1a_MatchesPrototype() => Assert.AreEqual(1483444959u, NameHash.Fnv1a("김도현"));

        [TestCase(136.5, 137)]   // 컨택형 0단계 목표치 150 × 1.3 × 0.7
        [TestCase(54.6, 55)]
        [TestCase(-2.5, -2)]     // JS Math.round는 .5를 +∞ 쪽으로
        [TestCase(0.49, 0)]
        public void RoundHalfUp_IsJsRound(double x, int expected) => Assert.AreEqual(expected, JsMath.RoundHalfUp(x));

        [Test]
        public void WeightedPick_AllZero_ReturnsFirst()
        {
            var r = new SeededRandom(1);
            Assert.AreEqual(0, WeightedPick.Pick(ref r, new double[] { 0, 0, 0 }));
        }
    }
}
