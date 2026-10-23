using BBGame.Core;
using BBGame.Presentation.Match;
using NUnit.Framework;

namespace BBGame.Tests
{
    /// <summary>카드·칸 선택 규칙 — 프로토타입 v12 bindPlay와 같게 (match_m2_screen_plan "조작 규칙")</summary>
    public sealed class SelectionStateTests
    {
        /// <summary>
        /// 가짜 손패: 0 몸쪽 높은 직구(InHi) · 1 S카드(OutLo, 바깥 낮게 빠지는) · 2 전력투구 직구 · 3 가운데 보고 던지기(MidMid)
        /// </summary>
        private sealed class FakeHand : IHandRules
        {
            private static readonly ZoneId[][] s_zones =
            {
                new[] { ZoneId.InHi },
                new[] { ZoneId.OutLo, ZoneId.OOutLo },
                new ZoneId[0],
                new[] { ZoneId.MidMid },
            };

            public int HandCount => s_zones.Length;
            public bool IsPowerPitch(int i) => i == 2;
            public bool CanTarget(int i, ZoneId z) => System.Array.IndexOf(s_zones[i], z) >= 0;
            public ZoneId FirstZone(int i) => s_zones[i][0];
        }

        private readonly FakeHand _hand = new FakeHand();

        [Test]
        public void ClickCard_SelectsCardAndItsFirstZone()
        {
            var s = new SelectionState();
            s.ClickCard(0, _hand);
            Assert.AreEqual(0, s.Card);
            Assert.AreEqual(ZoneId.InHi, s.Zone);
            Assert.IsFalse(s.ZoneFirst);
        }

        [Test]
        public void ClickSameCard_Deselects_AndClearsCardZone()
        {
            var s = new SelectionState();
            s.ClickCard(0, _hand);
            s.ClickCard(0, _hand);
            Assert.IsFalse(s.HasCard);
            Assert.IsNull(s.Zone, "카드가 정한 칸은 함께 풀린다");
        }

        [Test]
        public void SCard_SecondZoneClick_KeepsCard()
        {
            var s = new SelectionState();
            s.ClickCard(1, _hand);
            s.ClickZone(ZoneId.OOutLo, _hand);
            Assert.AreEqual(1, s.Card);
            Assert.AreEqual(ZoneId.OOutLo, s.Zone);
            Assert.AreEqual(ZoneId.OOutLo, s.ToCommand(_hand).Zone);
        }

        [Test]
        public void ZoneFirst_ThenMatchingCard_KeepsZone()
        {
            var s = new SelectionState();
            s.ClickZone(ZoneId.OOutLo, _hand);
            Assert.IsTrue(s.ZoneFirst);
            Assert.IsTrue(s.Fits(1, _hand));
            Assert.IsFalse(s.Fits(0, _hand));
            s.ClickCard(1, _hand);
            Assert.AreEqual(ZoneId.OOutLo, s.Zone, "고른 칸으로 던질 수 있는 카드면 칸을 유지한다");
            s.ClickCard(1, _hand);
            Assert.AreEqual(ZoneId.OOutLo, s.Zone, "코스 먼저로 고른 칸은 카드를 풀어도 남는다");
        }

        [Test]
        public void ZoneFirst_ThenOtherCard_SwitchesToCardZone()
        {
            var s = new SelectionState();
            s.ClickZone(ZoneId.OOutLo, _hand);
            s.ClickCard(0, _hand);
            Assert.AreEqual(ZoneId.InHi, s.Zone, "고른 칸으로 못 던지면 카드의 첫 코스로");
            Assert.IsFalse(s.ZoneFirst);
        }

        [Test]
        public void CardSelected_ClickZoneOutsideCard_SwitchesToZoneFirst()
        {
            var s = new SelectionState();
            s.ClickCard(0, _hand);
            s.ClickZone(ZoneId.MidLo, _hand);
            Assert.IsFalse(s.HasCard);
            Assert.AreEqual(ZoneId.MidLo, s.Zone);
            Assert.IsTrue(s.ZoneFirst);
        }

        [Test]
        public void ZoneFirst_ClickSameZone_Clears()
        {
            var s = new SelectionState();
            s.ClickZone(ZoneId.MidLo, _hand);
            s.ClickZone(ZoneId.MidLo, _hand);
            Assert.IsNull(s.Zone);
            Assert.IsFalse(s.ZoneFirst);
        }

        [Test]
        public void PowerPitch_HasNoZone_AndFitsAnyInsideZone()
        {
            var s = new SelectionState();
            s.ClickZone(ZoneId.InLo, _hand);
            Assert.IsTrue(s.Fits(2, _hand), "전력투구는 존 안이면 맞는다");
            s.ClickCard(2, _hand);
            Assert.IsNull(s.Zone);
            Assert.IsNull(s.ToCommand(_hand).Zone, "전력투구는 칸 없이 던진다");

            s.Clear();
            s.ClickZone(ZoneId.OInLo, _hand);
            Assert.IsFalse(s.Fits(2, _hand), "존 밖 칸에는 전력투구가 가지 않는다");
        }

        [Test]
        public void ToCommand_UsesSelectedCardAndZone()
        {
            var s = new SelectionState();
            s.ClickCard(3, _hand);
            var cmd = s.ToCommand(_hand);
            Assert.AreEqual(3, cmd.HandIndex);
            Assert.AreEqual(ZoneId.MidMid, cmd.Zone);
        }
    }
}
