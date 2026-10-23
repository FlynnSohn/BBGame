using BBGame.Core;
using NUnit.Framework;

namespace BBGame.Tests
{
    /// <summary>찌르기 판정표 (§6-2) — 존 안·존 밖 × 노림 7종</summary>
    public class StabJudgeTests
    {
        private static Stab J(Intent i, string zone, bool fast)
        {
            Assert.IsTrue(Zone.TryParsePrototype(zone, out var z));
            return StabJudge.Judge(i, z, fast);
        }

        [Test]
        public void Inside_Zone()
        {
            Assert.AreEqual(Stab.Counter, J(Intent.Fastball, "mid-mid", false));
            Assert.AreEqual(Stab.Bad, J(Intent.Fastball, "mid-mid", true));
            Assert.AreEqual(Stab.Counter, J(Intent.Breaking, "in-hi", true));
            Assert.AreEqual(Stab.Bad, J(Intent.Breaking, "in-hi", false));
            Assert.AreEqual(Stab.Counter, J(Intent.Inside, "out-mid", true));
            Assert.AreEqual(Stab.Bad, J(Intent.Inside, "in-lo", true));
            Assert.AreEqual(Stab.Neutral, J(Intent.Inside, "mid-hi", true));
            Assert.AreEqual(Stab.Counter, J(Intent.Outside, "in-mid", false));
            Assert.AreEqual(Stab.Bad, J(Intent.Outside, "out-lo", false));
            Assert.AreEqual(Stab.Neutral, J(Intent.Outside, "mid-lo", false));
            Assert.AreEqual(Stab.Bad, J(Intent.Contact, "mid-mid", true));
            Assert.AreEqual(Stab.Neutral, J(Intent.Contact, "in-hi", true));
            Assert.AreEqual(Stab.Counter, J(Intent.Take, "out-lo", false));
            Assert.AreEqual(Stab.Counter, J(Intent.Bunt, "in-hi", true));
            Assert.AreEqual(Stab.Bad, J(Intent.Bunt, "mid-lo", true));
            Assert.AreEqual(Stab.Neutral, J(Intent.Bunt, "in-hi", false));
            Assert.AreEqual(Stab.Neutral, J(Intent.Bunt, "mid-mid", true));
        }

        [Test]
        public void Outside_Zone_LureOrNeutral()
        {
            Assert.AreEqual(Stab.Counter, J(Intent.Fastball, "R:out-lo", false));
            Assert.AreEqual(Stab.Counter, J(Intent.Fastball, "R:mid-hi", true));
            Assert.AreEqual(Stab.Counter, J(Intent.Breaking, "R:out-lo", false));
            Assert.AreEqual(Stab.Neutral, J(Intent.Breaking, "R:mid-hi", true));   // 존 밖 직구는 늦어서 손이 안 나간다
            Assert.AreEqual(Stab.Counter, J(Intent.Inside, "R:in-mid", true));
            Assert.AreEqual(Stab.Neutral, J(Intent.Inside, "R:out-mid", true));
            Assert.AreEqual(Stab.Counter, J(Intent.Outside, "R:out-lo", false));
            Assert.AreEqual(Stab.Neutral, J(Intent.Outside, "R:in-lo", false));
            Assert.AreEqual(Stab.Counter, J(Intent.Contact, "R:in-hi", true));
            Assert.AreEqual(Stab.Bad, J(Intent.Take, "R:mid-lo", false));            // 존 밖 걸렸다는 지켜보기뿐
            Assert.AreEqual(Stab.Counter, J(Intent.Bunt, "R:mid-hi", true));
            Assert.AreEqual(Stab.Neutral, J(Intent.Bunt, "R:mid-lo", true));
        }

        [Test]
        public void Outside_Zone_NeverBad_ExceptTake()
        {
            for (int i = 9; i < Zone.Count; i++)
                foreach (Intent intent in System.Enum.GetValues(typeof(Intent)))
                    foreach (bool fast in new[] { true, false })
                        if (intent != Intent.Take)
                            Assert.AreNotEqual(Stab.Bad, StabJudge.Judge(intent, new Zone((ZoneId)i), fast));
        }
    }
}
