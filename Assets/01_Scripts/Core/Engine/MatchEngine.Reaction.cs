using System;

namespace BBGame.Core
{
    public sealed partial class MatchEngine
    {
        // 반응 가중치 배열 칸 — 가지마다 프로토타입 객체 키 순서가 다르다 (추첨 결과가 순서에 따라 달라진다)
        private enum W : byte { Whiff, Look, Foul, InPlay }

        /// <summary>타자 반응 (§9) — 프로토타입 react</summary>
        private Reaction React(CardDef def, PitchTypeRow pt, Zone z, Stab stab, int velo, out bool chased)
        {
            chased = false;
            var ab = _s.AtBat;
            var b = ab.Batter;
            var rt = _t.reaction;
            var sc = _t.scout;
            bool same = PitcherHand == b.Def.Hand;
            bool fast = pt.IsFastball;
            // (M7) 스퀴즈

            // 번트 노림 + 존 안 (§9-3)
            if (ab.Intent == Intent.Bunt && !z.Outside)
            {
                Span<double> bw = stackalloc double[4];
                if (stab == Stab.Counter)
                {
                    bw[0] = rt.buntCounterFoul; bw[1] = rt.buntCounterWhiff; bw[2] = rt.buntCounterInPlay; bw[3] = rt.buntCounterLook;
                    int k = WeightedPick.Pick(ref _rng, bw);
                    return k == 0 ? Reaction.Foul : k == 1 ? Reaction.Whiff : k == 2 ? Reaction.InPlay : Reaction.Look;
                }
                bw = bw.Slice(0, 3);
                bw[0] = rt.buntOtherInPlay; bw[1] = rt.buntOtherFoul; bw[2] = rt.buntOtherLook;
                int k2 = WeightedPick.Pick(ref _rng, bw);
                return k2 == 0 ? Reaction.InPlay : k2 == 1 ? Reaction.Foul : Reaction.Look;
            }

            // 존 밖 — 쫓는가 (§9-2). 유인이면 배트가 나오고, 비켜간 공은 잘 안 쫓는다
            if (z.Outside)
            {
                var I = ab.Intent;
                double ch = rt.chaseBase;
                if (I == Intent.Take) ch = rt.takeChase;
                else if (I == Intent.Contact) ch = rt.ctChase;
                else if (stab == Stab.Counter) ch = I == Intent.Fastball && !fast ? rt.timingChase : rt.lureChase;
                else if (I == Intent.Inside || I == Intent.Outside || I == Intent.Breaking) ch = rt.awayChase;
                if (ab.Strikes == 2) ch += rt.twoStrikeChaseAdd;
                // (M6) 다혈질 발동 +
                double zc = b.Def.ZoneChase[(int)z.Id];
                if (sc.scout && sc.zone && zc != 0) ch *= zc;
                if (sc.scout && sc.count && ab.Strikes == 2) ch += b.Def.TwoStrikeChase;
                ch *= b.Def.Type.Chase;
                if (def.Special == CardSpecial.Drop || def.Special == CardSpecial.Dirt) ch += rt.dropChaseAdd;
                // (M7) 히트앤런·런앤히트
                if (_rng.NextDouble() >= ch) return Reaction.Ball;

                Span<double> cw = stackalloc double[3];
                cw[0] = rt.chasedWhiff; cw[1] = rt.chasedFoul; cw[2] = rt.chasedInPlay;
                if (def.Special == CardSpecial.Dirt && ab.Strikes == 2) cw[0] *= rt.dirtTwoStrikeWhiff;
                chased = true;
                int k = WeightedPick.Pick(ref _rng, cw);
                return k == 0 ? Reaction.Whiff : k == 1 ? Reaction.Foul : Reaction.InPlay;
            }

            // 존 안 (§9-1) — 가지마다 가중치와 키 순서
            double whiff, look, foul, inplay;
            Span<byte> order = stackalloc byte[4];
            if (ab.Intent == Intent.Take)
            {
                look = rt.takeLook; foul = rt.takeFoul; inplay = rt.takeInPlay; whiff = 0;
                order[0] = (byte)W.Look; order[1] = (byte)W.Foul; order[2] = (byte)W.InPlay; order[3] = (byte)W.Whiff;
            }
            else if (stab == Stab.Counter)
            {
                double ip = JsMath.Clamp(rt.ipCounterBase + Math.Max(0, b.Def.Type.Adjust - rt.ipAdjustPivot) * rt.ipCounterAdj, 0, rt.ipMax);
                whiff = rt.counterWhiff * (1 - ip); look = rt.counterLook * (1 - ip); foul = rt.counterFoul * (1 - ip); inplay = rt.counterInPlay * ip;
                order[0] = (byte)W.Whiff; order[1] = (byte)W.Look; order[2] = (byte)W.Foul; order[3] = (byte)W.InPlay;
            }
            else if (stab == Stab.Neutral)
            {
                foul = rt.neutralFoul; inplay = rt.neutralInPlay; look = rt.neutralLook; whiff = rt.neutralWhiff;
                order[0] = (byte)W.Foul; order[1] = (byte)W.InPlay; order[2] = (byte)W.Look; order[3] = (byte)W.Whiff;
            }
            else
            {
                inplay = rt.badInPlay; foul = rt.badFoul; whiff = 0; look = 0;
                order[0] = (byte)W.InPlay; order[1] = (byte)W.Foul; order[2] = (byte)W.Whiff; order[3] = (byte)W.Look;
            }

            if (b.Def.Type.Kind == BatterKind.Contact) { foul += whiff * rt.contactWhiffShift; whiff *= rt.contactWhiffShift; }
            if (sc.scout && sc.count && ab.Balls == 0 && ab.Strikes == 0)
                look *= b.Def.FirstPitch == FirstPitchStyle.Passive ? rt.firstPassLook : b.Def.FirstPitch == FirstPitchStyle.Aggressive ? rt.firstAggLook : 1;
            if (ab.Strikes == 2 && stab != Stab.Counter) look *= rt.twoStrikeLook;   // 보호 스윙
            if (z.IsCenter) inplay *= rt.centerInPlay;
            if (fast && z.Row == ZoneRow.Hi) whiff *= rt.highFastballWhiff;
            // 프로토타입은 계열이 아니라 그 구종 하나에만 건다 (리서치 §3-6)
            if (def.PitchType == PitchType.Curve && z.Row == ZoneRow.Lo) whiff *= rt.lowCurveWhiff;
            if (def.PitchType == PitchType.Slider && same) whiff *= rt.sameSliderWhiff;
            if (def.PitchType == PitchType.Changeup && !same) whiff *= rt.oppChangeupWhiff;
            // 구종 보정 (§4-5)
            whiff *= pt.whiffMul;
            if (z.Row == ZoneRow.Lo) whiff *= pt.whiffLowMul;
            if (same) whiff *= pt.whiffSameMul;
            if (!same) whiff *= pt.whiffOppMul;
            if (def.Special == CardSpecial.Backdoor && !same) look *= rt.backdoorLook;
            // 구속 반응 — 직구 계열만, 작게 (§4-6)
            if (_f.veloFx && fast && velo != 0) whiff *= 1 + rt.veloWhiff * JsMath.Clamp(velo - pt.baseVelo, -rt.veloClamp, rt.veloClamp);
            // (M7) 히트앤런·런앤히트 루킹 ↓
            if (_t.game.fatigueHard && Tier().Level == 3) whiff *= rt.tier3Whiff;   // 구속이 떨어져 헛스윙이 준다

            Span<double> w = stackalloc double[4];
            for (int i = 0; i < 4; i++)
            {
                var key = (W)order[i];
                w[i] = key == W.Whiff ? whiff : key == W.Look ? look : key == W.Foul ? foul : inplay;
            }
            var picked = (W)order[WeightedPick.Pick(ref _rng, w)];
            var kind = picked == W.Whiff ? Reaction.Whiff : picked == W.Look ? Reaction.Look : picked == W.Foul ? Reaction.Foul : Reaction.InPlay;

            // 진화 「라이징 패스트볼」 — 2스트라이크에서 찌른 직구는 반드시 헛스윙 (지켜보기 제외)
            if (_s.Modifiers.Has(Evolution.Rising) && fast && ab.Strikes == 2 && stab == Stab.Counter && ab.Intent != Intent.Take)
                kind = Reaction.Whiff;
            return kind;
        }
    }
}
