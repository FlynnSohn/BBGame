using System;
using BBGame.Core;

namespace BBGame.Bots
{
    /// <summary>
    /// 봇의 선택지와 고르기 점수 — 프로토타입 options(G) · botChoose(G, target).
    /// 선택지 버퍼를 다시 쓴다 (공마다 할당 없음). 곱셈 순서는 프로토타입과 같게 둔다 (부동소수 결과가 같도록)
    /// </summary>
    public sealed class BotChooser
    {
        private struct Option
        {
            public int Hand;
            public ZoneId? Zone;    // 전력투구는 null (프로토타입 "zone9")
            public bool Fastball;
        }

        private readonly GameDatabase _db;
        private readonly BotTuning _bt;
        private readonly double _ballKeep;
        private readonly double _zoneAmp;
        private readonly double _powerCenter, _powerNear;
        private Option[] _opts = new Option[64];
        private int _count;

        public BotChooser(GameDatabase db)
        {
            _db = db;
            _bt = db.Tuning.bot;
            _ballKeep = db.Tuning.read.ballKeep;
            _zoneAmp = db.Tuning.inPlay.zoneAmp;
            _powerCenter = db.Tuning.control.powerCenter;
            _powerNear = db.Tuning.control.powerNear;
        }

        /// <summary>선택지 수 (마지막 Fill 기준) — 테스트용</summary>
        public int Count => _count;

        /// <summary>무작위 — 선택지 중 하나를 경기 난수로 (프로토타입 pick(options(G)))</summary>
        public ThrowCommand Random(MatchEngine e)
        {
            Fill(e);
            var o = _opts[e.BotRandomIndex(_count)];
            return new ThrowCommand(o.Hand, o.Zone);
        }

        /// <summary>
        /// 목표 노림에 가장 점수가 높은 투구 — 프로토타입 botChoose(G, target, zmap)[0].
        /// zoneMap: 타자의 진짜 핫/콜드 존을 아는가 (정답 봇 — 프로토타입은 cfg.scout면 sc.hit를 넘긴다)
        /// </summary>
        public ThrowCommand Best(MatchEngine e, Intent target, bool zoneMap = false)
        {
            Fill(e);
            var s = e.State;
            var b = s.CurrentBatter.Def;
            var zoneHit = zoneMap ? b.ZoneHit : null;
            // 프로토타입은 안정 정렬 뒤 첫 번째를 쓴다 = "처음 나온 최댓값" (점수에 NaN이 없으므로 정렬 없이 한 번 훑는다)
            int best = 0;
            double bestV = double.NegativeInfinity;
            for (int k = 0; k < _count; k++)
            {
                double v = Score(e, s, target, in _opts[k], zoneHit);
                if (v > bestV) { bestV = v; best = k; }
            }
            var o0 = _opts[best];
            return new ThrowCommand(o0.Hand, o0.Zone);
        }

        /// <summary>선택지 하나의 점수 (프로토타입 botChoose의 v)</summary>
        private double Score(MatchEngine e, IMatchReadOnly s, Intent target, in Option o, double[] zoneHit)
        {
            // 전력투구는 미리보기·판정 모두 가운데 직구로 본다 (프로토타입 pitchInfo zone9 → mid-mid)
            var z = new Zone(o.Zone ?? ZoneId.MidMid);
            var j = StabJudge.Judge(target, z, o.Fastball);
            double v = e.BotPreviewBase(o.Hand, o.Zone) * (j == Stab.Counter ? _bt.counterW : j == Stab.Neutral ? 1 : _bt.badW);
            if (zoneHit != null)
            {
                // 뜨거운 존은 피한다. 전력투구는 칸이 던질 때 정해지므로 떨어질 칸 확률로 핫/콜드 기댓값을 쓴다
                // (2026-11-05 수정 — 프로토타입은 여기서 NaN이 되어 정답 봇이 전력투구를 던지지 않았다. decisions 기록)
                double hit = o.Zone.HasValue ? zoneHit[(int)o.Zone.Value] : PowerLandingHit(zoneHit);
                v /= 1 + (hit - 1) * _zoneAmp;
            }
            if (z.Outside && target != Intent.Contact)
            {
                // 존 밖 공은 볼이 되기 쉽다 — 찔렀다면 제압치 일부(ballKeep)는 남는다
                double f = s.Strikes == 2 ? _bt.ballFar2S : _bt.ballFar;
                v *= j == Stab.Counter ? f + (1 - f) * _ballKeep : f;
            }
            if (z.Outside && s.Balls == 3) v *= _bt.threeBall;                                  // 볼넷 위험
            if (s.Gauge >= s.Target && s.Strikes == 2 && z.Outside) v *= _bt.reachedBall;       // 이미 잡을 수 있으면 존 안으로
            return v;
        }

        /// <summary>선택지 = 손패 순서 × 카드의 칸 순서. 전력투구 카드는 칸 없이 하나</summary>
        private void Fill(MatchEngine e)
        {
            _count = 0;
            var hand = e.State.Hand;
            for (int i = 0; i < hand.Count; i++)
            {
                var def = hand[i].Def;
                bool fast = _db.PitchTypes.Get(def.PitchType).IsFastball;
                if (def.IsPowerPitch) { Push(i, null, fast); continue; }
                var zones = def.Zones;
                for (int z = 0; z < zones.Length; z++) Push(i, zones[z], fast);
            }
        }

        private void Push(int hand, ZoneId? zone, bool fast)
        {
            if (_count == _opts.Length) Array.Resize(ref _opts, _opts.Length * 2);   // 거의 일어나지 않는다 (손패 8장 × 칸 몇 개)
            _opts[_count++] = new Option { Hand = hand, Zone = zone, Fastball = fast };
        }

        /// <summary>
        /// 전력투구가 떨어질 칸의 핫/콜드 기댓값 — Landing.PowerLanding과 같은 확률
        /// (한가운데 powerCenter · 옆 4칸이 나머지 powerNear까지 나눠 갖고 · 모서리 4칸이 그 나머지).
        /// 덧셈 순서는 비교 기록을 만든 내보내기 도구와 같다
        /// </summary>
        private double PowerLandingHit(double[] h)
        {
            double near = h[(int)ZoneId.InMid] + h[(int)ZoneId.OutMid] + h[(int)ZoneId.MidHi] + h[(int)ZoneId.MidLo];
            double corner = h[(int)ZoneId.InHi] + h[(int)ZoneId.OutHi] + h[(int)ZoneId.InLo] + h[(int)ZoneId.OutLo];
            return _powerCenter * h[(int)ZoneId.MidMid] + (_powerNear - _powerCenter) / 4 * near + (1 - _powerNear) / 4 * corner;
        }
    }
}
