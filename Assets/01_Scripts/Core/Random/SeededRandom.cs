namespace BBGame.Core
{
    /// <summary>
    /// 시드 난수 — 프로토타입 mulberry32와 같은 수열 (§21, decisions 2026-10-13: 주입된 시드 RNG).
    /// 값 타입이라 할당이 없다. 메서드에 넘길 때는 ref로 넘겨야 상태가 이어진다.
    /// 확인값: 시드 12345 → 0.9797282678, 0.3067522645, 0.4842054215
    /// </summary>
    public struct SeededRandom
    {
        private uint _state;

        public SeededRandom(uint seed) { _state = seed; }

        /// <summary>[0, 1) 실수 — 프로토타입 R()</summary>
        public double NextDouble()
        {
            unchecked
            {
                _state += 0x6D2B79F5u;
                uint t = (_state ^ (_state >> 15)) * (1u | _state);
                t = (t + ((t ^ (t >> 7)) * (61u | t))) ^ t;
                return (t ^ (t >> 14)) / 4294967296.0;
            }
        }

        /// <summary>[0, count) 정수 — 프로토타입 Math.floor(R() * n) (pick의 인덱스)</summary>
        public int NextIndex(int count) => (int)(NextDouble() * count);

        /// <summary>이름 시드 난수 — 프로토타입 mulberry(hash32(key))</summary>
        public static SeededRandom FromKey(string key) => new SeededRandom(NameHash.Fnv1a(key));
    }
}
