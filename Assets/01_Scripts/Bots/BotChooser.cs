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
            public double Value;
        }

        private readonly GameDatabase _db;
        private readonly BotTuning _bt;
        private readonly double _ballKeep;
        private readonly double _zoneAmp;
        private Option[] _opts = new Option[64];
        private int _count;

        public BotChooser(GameDatabase db)
        {
            _db = db;
            _bt = db.Tuning.bot;
            _ballKeep = db.Tuning.read.ballKeep;
            _zoneAmp = db.Tuning.inPlay.zoneAmp;
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
            for (int k = 0; k < _count; k++) _opts[k].Value = Score(e, s, target, in _opts[k], zoneMap ? b.ZoneHit : null);
            SortLikeV8(_opts, _count);
            var o0 = _opts[0];
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
                // 뜨거운 존은 피한다. 전력투구는 프로토타입에서 칸 "zone9"가 존 표에 없어 점수가 NaN이 된다 —
                // 정렬에서 아무것과도 "같음"이 되어 자리가 정렬 구현에 달린다. 기준선(§19-3)이 이 동작으로 재졌으므로 그대로 따른다 (M9 결과 메모)
                v /= o.Zone.HasValue ? 1 + (zoneHit[(int)o.Zone.Value] - 1) * _zoneAmp : double.NaN;
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
        // ───────────────────────── 정렬 — 프로토타입(브라우저 V8)과 같은 순서 ─────────────────────────

        // 프로토타입 비교 함수 (a, b) => b.v − a.v. 결과가 NaN이면 JS 명세(SortCompare)대로 0 — "같음"
        private static double Compare(in Option a, in Option b)
        {
            double d = b.Value - a.Value;
            return double.IsNaN(d) ? 0 : d;
        }

        /// <summary>
        /// V8 Array.prototype.sort (TimSort)의 짧은 배열 경로를 그대로 옮긴다 — 64개 미만이면 첫 런 만들기 + 이진 삽입 정렬뿐이다.
        /// 점수에 NaN이 섞이면(정답 봇의 전력투구) 결과가 이 구현 순서에 달려서, 일반 정렬로는 프로토타입과 같아지지 않는다
        /// </summary>
        private static void SortLikeV8(Option[] a, int n)
        {
            if (n < 2) return;
            if (n >= 64) throw new NotSupportedException("선택지가 64개 이상 — 병합 단계는 옮기지 않았다");
            int run = CountAndMakeRun(a, 0, n);
            if (run < n) BinaryInsertionSort(a, 0, run, n);
        }

        private static int CountAndMakeRun(Option[] a, int lowArg, int high)
        {
            int low = lowArg + 1;
            if (low == high) return 1;
            int runLength = 2;
            double order = Compare(in a[low], in a[low - 1]);
            bool descending = order < 0;
            int prev = low;
            for (int i = low + 1; i < high; i++)
            {
                order = Compare(in a[i], in a[prev]);
                if (descending) { if (order >= 0) break; }
                else { if (order < 0) break; }
                prev = i;
                runLength++;
            }
            if (descending) Array.Reverse(a, lowArg, runLength);
            return runLength;
        }

        private static void BinaryInsertionSort(Option[] a, int low, int startArg, int high)
        {
            int start = low == startArg ? startArg + 1 : startArg;
            for (; start < high; start++)
            {
                int left = low, right = start;
                var pivot = a[start];
                while (left < right)
                {
                    int mid = left + ((right - left) >> 1);
                    if (Compare(in pivot, in a[mid]) < 0) right = mid;
                    else left = mid + 1;
                }
                for (int p = start; p > left; p--) a[p] = a[p - 1];
                a[left] = pivot;
            }
        }
    }
}
