using BBGame.Core;
using NUnit.Framework;

namespace BBGame.Tests
{
    /// <summary>읽기 배수 = 찌르기 배수 × 선언 계수 (§8-4) — 튜닝 기본값 기준</summary>
    public class ReadMultiplierTests
    {
        private static ReadTuning T => TestData.Db.Tuning.read;

        private static double M(bool declareOn, Intent? decl, Intent intent, Stab stab, CardSpecial sp = CardSpecial.None)
            => ReadMultiplier.Compute(T, declareOn, decl, intent, stab, sp, 0, false, null, ZoneCol.Mid).Multiplier;

        [Test]
        public void Stage0()
        {
            Assert.AreEqual(1.25, M(false, null, Intent.Fastball, Stab.Counter), 1e-9);
            Assert.AreEqual(1.0, M(false, null, Intent.Fastball, Stab.Neutral), 1e-9);
            Assert.AreEqual(0.5, M(false, null, Intent.Fastball, Stab.Bad), 1e-9);
            Assert.AreEqual(0.75, M(false, null, Intent.Fastball, Stab.Bad, CardSpecial.Jam), 1e-9);
            Assert.AreEqual(ReadState.Stage0, ReadMultiplier.Compute(T, false, null, Intent.Fastball, Stab.Counter, CardSpecial.None, 0, false, null, ZoneCol.Mid).State);
        }

        [Test]
        public void DeclareCombinations_NineCells()
        {
            const Intent I = Intent.Fastball, Wrong = Intent.Breaking;
            Assert.AreEqual(2.5, M(true, I, I, Stab.Counter), 1e-9);          // 읽었다
            Assert.AreEqual(5.0 / 3, M(true, I, I, Stab.Neutral), 1e-9);      // 읽었지만 못 찔렀다
            Assert.AreEqual(5.0 / 6, M(true, I, I, Stab.Bad), 1e-9);          // 읽었지만 걸렸다
            Assert.AreEqual(1.25, M(true, null, I, Stab.Counter), 1e-9);      // 선언 없이 찔렀다
            Assert.AreEqual(1.0, M(true, null, I, Stab.Neutral), 1e-9);
            Assert.AreEqual(0.5, M(true, null, I, Stab.Bad), 1e-9);
            Assert.AreEqual(0.75, M(true, Wrong, I, Stab.Counter), 1e-9);     // 빗나갔지만 찔렀다
            Assert.AreEqual(0.5, M(true, Wrong, I, Stab.Neutral), 1e-9);      // 빗나갔다
            Assert.AreEqual(0.25, M(true, Wrong, I, Stab.Bad), 1e-9);
        }

        [Test]
        public void Ace_And_CourseCap()
        {
            Assert.AreEqual(3.0, M(true, Intent.Inside, Intent.Inside, Stab.Counter, CardSpecial.Ace), 1e-9);
            var capped = ReadMultiplier.Compute(T, false, null, Intent.Inside, Stab.Counter, CardSpecial.None, 0, true, ZoneCol.Out, ZoneCol.Out);
            Assert.AreEqual(1.0, capped.Multiplier, 1e-9);
            Assert.IsTrue(capped.CourseCapped);
            var mid = ReadMultiplier.Compute(T, false, null, Intent.Inside, Stab.Counter, CardSpecial.None, 0, true, ZoneCol.Mid, ZoneCol.Mid);
            Assert.IsFalse(mid.CourseCapped);   // 가운데는 제외
        }
    }
}
