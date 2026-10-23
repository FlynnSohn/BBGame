using System.Collections.Generic;

namespace BBGame.Core
{
    public sealed partial class MatchEngine
    {
        /// <summary>이 카드를 이 칸으로 던질 수 있는가. 전력투구 직구는 칸을 null로 (코스를 고르지 않는다)</summary>
        public bool CanThrow(int handIndex, ZoneId? zone)
        {
            if (_s.Phase != MatchPhase.Pitching) return false;
            var hand = _s.Deck.Hand;
            if (handIndex < 0 || handIndex >= hand.Count) return false;
            var def = hand[handIndex].Def;
            if (def.IsPowerPitch) return zone == null || !new Zone(zone.Value).Outside;
            return zone != null && def.CanTarget(zone.Value);
        }

        /// <summary>
        /// 던지기 전 미리보기 — 구위 × (1 + 가산) = 기본 제압치까지만 (§8-1, §20-4).
        /// 가산 칩은 넘겨받은 버퍼에 채운다 (할당 없음, chipsOut은 비우고 채운다). 난수를 쓰지 않는다.
        /// 전력투구 직구는 칸이 정해지지 않아 칸 족보·칸 가산을 빼고 보여 준다 (§8-3 Z17).
        /// </summary>
        public PitchPreview Preview(int handIndex, ZoneId? zone, List<AddChip> chipsOut)
        {
            chipsOut?.Clear();
            if (!CanThrow(handIndex, zone)) return default;
            var inst = _s.Deck.Hand[handIndex];
            bool any = inst.Def.IsPowerPitch;
            var z = any ? new Zone(ZoneId.MidMid) : new Zone(zone.Value);
            return PreviewCore(inst, z, any, chipsOut);
        }

        /// <summary>프로토타입 preview(G, inst, zone) — 실제 계산과 화면 미리보기가 같은 함수를 쓴다</summary>
        private PitchPreview PreviewCore(CardInstance inst, Zone zone, bool anyZone, List<AddChip> chips)
        {
            var ab = _s.AtBat;
            var def = inst.Def;
            var pt = _db.PitchTypes.Get(def.PitchType);
            var mods = _s.Modifiers;
            var st = _t.stuff;
            var at = _t.add;
            bool fast = pt.IsFastball;
            var tier = Tier();

            // 구위 (§8-2)
            int stuff = PitchPower.CardStuff(st, def, inst.Plus);
            if (fast && Lv(BonusId.Heavy) > 0) stuff += st.heavyPerLv * Lv(BonusId.Heavy);
            if (fast && mods.Has(Condition.Live)) stuff += st.liveFastball;
            if (pt.like == PitchLike.Curve && mods.Has(Condition.Curve)) stuff -= st.curvePenalty;
            stuff = System.Math.Max(1, JsMath.RoundHalfUp(stuff * tier.StuffMul));

            // 가산 (§8-3) — 합산 순서는 프로토타입 칩 순서와 같다 (부동소수 합이 같도록)
            double sum = 0;
            void Add(AddChipKind kind, double v, ComboKind combo = ComboKind.None)
            {
                if (v == 0) return;   // 프로토타입 add(label, v)는 0을 넣지 않는다
                sum += v;
                chips?.Add(new AddChip(kind, v, combo));
            }

            if (ab.Strikes > 0) Add(AddChipKind.Count, at.countAdd * ab.Strikes);

            double veloExp = PitchPower.ExpectedVelo(st, pt, def, tier.Level, false, 0);
            if (ab.HasPrev)
            {
                var prev = ab.Prev;
                var kind = BasicCombo.Of(at, new ComboPitch(prev.Actual, prev.Type, prev.Velo, false),
                    new ComboPitch(zone, def.PitchType, veloExp, anyZone), out double cv);
                if (kind != ComboKind.None)
                {
                    cv += at.comboPerLv * Lv(BonusId.Combo);
                    if (def.Special == CardSpecial.Mix) cv += at.mixCard;
                    Add(AddChipKind.Combo, cv, kind);
                }
            }
            // (M7) 고급·기묘한 족보 자리 — 발견 전이면 AddShown에서 뺀다

            if (ab.Strikes == 2)
            {
                Add(AddChipKind.Finisher, at.finisherPerLv * Lv(BonusId.Finisher));
                if (def.Special == CardSpecial.Finish) Add(AddChipKind.FinishCard, at.finishCard);
                if (def.Special == CardSpecial.Rise) Add(AddChipKind.RiseCard, at.riseCard);
            }
            bool prevFast = ab.HasPrev && ab.Prev.Fastball;
            if (def.Special == CardSpecial.Tempo && prevFast && !fast) Add(AddChipKind.TempoCard, at.tempoCard);
            if (def.Special == CardSpecial.Eephus && prevFast) Add(AddChipKind.EephusCard, at.eephusCard);
            if (mods.Weather == Weather.Cold && fast && zone.Col == ZoneCol.In && !zone.Outside && !anyZone) Add(AddChipKind.ColdInsideFastball, at.coldInsideFastball);
            if (mods.Weather == Weather.Wind && zone.Row == ZoneRow.Lo && !anyZone) Add(AddChipKind.WindLow, at.windLow);
            if (mods.Weather == Weather.Night && fast && zone.Row == ZoneRow.Hi && !anyZone) Add(AddChipKind.NightHighFastball, at.nightHighFastball);
            if (IsRisp()) Add(AddChipKind.Clutch, at.clutchPerLv * Lv(BonusId.Clutch));

            double hidden = 0;   // (M7) 발견하지 않은 고급·기묘한 족보 가산
            int baseActual = JsMath.RoundHalfUp(stuff * (1 + sum));
            int baseShown = JsMath.RoundHalfUp(stuff * (1 + sum - hidden));
            return new PitchPreview(stuff, tier.StuffMul, sum, sum - hidden, baseActual, baseShown);
        }
    }
}
