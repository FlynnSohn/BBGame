namespace BBGame.Core
{
    public sealed partial class MatchEngine
    {
        /// <summary>인플레이 판정 결과 (프로토타입 resolveInplay 반환값)</summary>
        private struct InPlay
        {
            public int Hit;          // 0 범타 · 1 단타 · 2 2루타 · 3 3루타 · 4 홈런
            public bool Ground;
            public bool Sacrifice;
            public bool BuntPop;
            public bool Bloop;
        }

        /// <summary>인플레이 → 안타·진루·범타·병살·희생플라이. 득점 수를 돌려준다 (프로토타입 throwPitch 인플레이 가지)</summary>
        private int ResolveInPlayOutcome(CardDef def, PitchTypeRow pt, Zone z, Stab stab, bool chased, int velo, ref PitchOutcome res)
        {
            var r = _result;
            var ip = ResolveInPlay(def, pt, z, stab, chased, velo);
            const bool go = false;   // (M7) 1루 주자가 스타트를 끊었다
            var rn = _t.running;
            int runs = 0;
            r.Grounder = ip.Ground;
            r.Bloop = ip.Bloop;

            if (ip.Hit > 0)
            {
                runs += HitAdvance(ip.Hit, go);
                res = ip.Hit == 4 ? PitchOutcome.HomeRun : ip.Hit == 3 ? PitchOutcome.Triple : ip.Hit == 2 ? PitchOutcome.Double : PitchOutcome.Single;
                _s.HitsOpp++;
                AddMental(ip.Hit == 4 ? _t.mental.homeRun : ip.Hit >= 2 ? _t.mental.extraBase : _t.mental.hit);
                return runs;
            }
            if (ip.Sacrifice)
            {
                _s.Outs++;
                runs += _s.Outs < 3 ? RunOn(1) : 0;
                res = PitchOutcome.Sacrifice;
                return runs;
            }
            // (M7) 스퀴즈 번트가 떴다

            if (ip.Ground && _s.Bases[0].On && _s.Outs < 2 && _rng.NextDouble() < (go ? rn.doublePlayGo : rn.doublePlay))
            {
                _s.Outs += 2;
                _s.Bases[0] = Runner.Empty;
                res = PitchOutcome.DoublePlay;
                if (_s.Outs < 3 && _s.Bases[2].On && _rng.NextDouble() < rn.dpThirdScores) { runs += Score(_s.Bases[2]); _s.Bases[2] = Runner.Empty; }
                return runs;
            }

            _s.Outs++;
            res = ip.Ground ? PitchOutcome.GroundOut : PitchOutcome.FlyOut;
            // (M7) 뛰던 1루 주자 직선타 병살
            if (_s.Outs < 3 && _s.Bases[2].On)
            {
                // 짧은 평가 순서가 난수 소비를 정한다 — 프로토타입과 같게
                if (!ip.Ground && _rng.NextDouble() < rn.sacFly) { runs += Score(_s.Bases[2]); _s.Bases[2] = Runner.Empty; r.SacFly = true; }
                else if (ip.Ground && _rng.NextDouble() < rn.groundThirdScores) { runs += Score(_s.Bases[2]); _s.Bases[2] = Runner.Empty; }
            }
            if (_s.Outs < 3 && ip.Ground && _s.Bases[1].On && !_s.Bases[2].On && _rng.NextDouble() < (go ? rn.groundSecondAdvanceGo : rn.groundSecondAdvance))
            {
                _s.Bases[2] = _s.Bases[1];
                _s.Bases[1] = Runner.Empty;
            }
            // (M7) 스타트를 끊은 1루 주자 진루타
            return runs;
        }

        /// <summary>인플레이 확률 (§10-2) — 프로토타입 resolveInplay</summary>
        private InPlay ResolveInPlay(CardDef def, PitchTypeRow pt, Zone z, Stab stab, bool chased, int velo)
        {
            var ab = _s.AtBat;
            var b = ab.Batter;
            var t = _t.inPlay;
            var sc = _t.scout;
            var type = b.Def.Type;
            bool same = PitcherHand == b.Def.Hand;
            bool fast = pt.IsFastball;
            double r = (double)ab.Gauge / ab.Target;
            bool tier3 = _t.game.fatigueHard && Tier().Level == 3;

            double groundBias = (z.Row == ZoneRow.Lo ? t.groundLow : z.Row == ZoneRow.Hi ? t.groundHigh : t.groundMid) + pt.groundBonus;
            bool ground = _rng.NextDouble() < JsMath.Clamp(groundBias, t.groundMin, t.groundMax);
            // (M7) 스퀴즈

            // 작전형 번트 — 1·2루 주자, 2아웃 전 (§10-2)
            if (ab.Intent == Intent.Bunt && type.CanBunt && (_s.Bases[0].On || _s.Bases[1].On) && _s.Outs < 2)
            {
                double hp = r >= 1 ? t.buntHitFull : t.buntHitBase * (1 - r);
                if (_rng.NextDouble() < hp) return new InPlay { Hit = 1, Ground = true };
                if (stab == Stab.Counter && _rng.NextDouble() < t.buntPop) return new InPlay { BuntPop = true };
                return new InPlay { Ground = true, Sacrifice = true };
            }

            double hitP;
            if (r >= 1) hitP = def.Special == CardSpecial.Bite ? 0 : t.bloop * (stab == Stab.Counter ? t.bloopCounterMul : 1);
            else
            {
                hitP = (chased ? t.hitChase : stab == Stab.Counter ? t.hitCounter : stab == Stab.Neutral ? t.hitNeutral : t.hitBad) * (1 - t.hitDamp * r);
                if (type.Kind == BatterKind.Power && stab == Stab.Bad) hitP *= t.powerBadHit;
                if (tier3 && stab == Stab.Bad) hitP *= t.tier3BadHit;
            }
            if (z.IsCenter) hitP *= t.centerHit;
            hitP *= 1 - t.weakPerLv * Lv(BonusId.Weak);
            if (sc.scout && sc.zone) hitP *= 1 + (b.Def.ZoneHit[(int)z.Id] - 1) * t.zoneAmp;   // 핫/콜드 존
            if (sc.scout && sc.risp && IsRisp()) hitP *= b.Def.RispFactor;               // 득점권 경향
            if (_s.Modifiers.Weather == Weather.Wet && ground) hitP *= t.wetGround;
            if (!same) hitP *= pt.hitOppMul;
            if (!same && z.Col == ZoneCol.In) hitP *= pt.jamOppMul;
            if (_f.veloFx && fast && velo != 0) hitP *= 1 - t.veloHit * JsMath.Clamp(velo - pt.baseVelo, -_t.reaction.veloClamp, _t.reaction.veloClamp);
            hitP = JsMath.Clamp(hitP, 0, t.hitMax);

            if (_rng.NextDouble() < hitP)
            {
                if (r >= 1) return new InPlay { Hit = 1, Bloop = true, Ground = ground };
                double xbh = (stab == Stab.Bad ? t.xbhBad : stab == Stab.Neutral ? t.xbhNeutral : t.xbhCounter) * type.Power * (1 - t.xbhDamp * r);
                if (type.Kind == BatterKind.Contact) xbh = 0;   // 컨택형 — 안타는 단타까지
                if (_s.Modifiers.Weather == Weather.Wind && z.Row == ZoneRow.Hi) xbh *= t.windHighXbh;
                if (tier3) xbh *= t.tier3Xbh;
                if (z.IsCenter) xbh *= t.midXbh;
                if (def.Special == CardSpecial.Eephus && stab == Stab.Bad) xbh *= t.eephusXbh;
                xbh = JsMath.Clamp(xbh, 0, t.xbhMax);
                if (_rng.NextDouble() < xbh)
                {
                    double hr = (fast && z.Row != ZoneRow.Lo ? t.hrFastHigh : t.hrOther) * System.Math.Min(t.hrPowerCap, type.Power);
                    double x = _rng.NextDouble();
                    if (x < hr) return new InPlay { Hit = 4 };
                    if (x < hr + t.triple) return new InPlay { Hit = 3 };
                    return new InPlay { Hit = 2 };
                }
                return new InPlay { Hit = 1, Ground = ground };
            }
            return new InPlay { Ground = ground };
        }
    }
}
