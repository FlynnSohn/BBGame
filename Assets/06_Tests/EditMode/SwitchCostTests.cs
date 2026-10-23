using BBGame.Core;
using NUnit.Framework;

namespace BBGame.Tests
{
    /// <summary>노림 전환 비용 (§6-4)</summary>
    public class SwitchCostTests
    {
        private static IntentTuning T => TestData.Db.Tuning.intent;

        [Test]
        public void Table()
        {
            Assert.AreEqual(0, SwitchCost.Cost(T, null, Intent.Fastball));
            Assert.AreEqual(0, SwitchCost.Cost(T, Intent.Inside, Intent.Inside));
            Assert.AreEqual(0.15, SwitchCost.Cost(T, Intent.Take, Intent.Fastball), 1e-9);
            Assert.AreEqual(0.15, SwitchCost.Cost(T, Intent.Bunt, Intent.Take), 1e-9);    // 지켜보기가 먼저
            Assert.AreEqual(0.30, SwitchCost.Cost(T, Intent.Bunt, Intent.Inside), 1e-9);
            Assert.AreEqual(0.15, SwitchCost.Cost(T, Intent.Fastball, Intent.Contact), 1e-9);
            Assert.AreEqual(0.30, SwitchCost.Cost(T, Intent.Contact, Intent.Fastball), 1e-9);
            Assert.AreEqual(0.80, SwitchCost.Cost(T, Intent.Fastball, Intent.Breaking), 1e-9);
            Assert.AreEqual(0.70, SwitchCost.Cost(T, Intent.Inside, Intent.Outside), 1e-9);
            Assert.AreEqual(0.42, SwitchCost.Cost(T, Intent.Fastball, Intent.Inside), 1e-9);
        }

        [Test]
        public void SuccessChance_FloorAt030()
        {
            Assert.AreEqual(0.30, SwitchCost.SuccessChance(T, 0.8, 0.0), 1e-9);            // 1 − 0.8 × 1 × 1.6 < 0.3
            Assert.AreEqual(1.0, SwitchCost.SuccessChance(T, 0, 0.5), 1e-9);
            Assert.AreEqual(1 - 0.42 * 0.12 * 1.6, SwitchCost.SuccessChance(T, 0.42, 0.88), 1e-9);
        }
    }
}
