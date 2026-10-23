namespace BBGame.Core
{
    /// <summary>이름 시드 해시 — 프로토타입 hash32(FNV-1a 32bit). hash32("김도현") = 1483444959</summary>
    public static class NameHash
    {
        /// <summary>UTF-16 코드 단위 기준 FNV-1a (JS charCodeAt와 같다)</summary>
        public static uint Fnv1a(string s)
        {
            unchecked
            {
                uint h = 2166136261u;
                for (int i = 0; i < s.Length; i++)
                {
                    h ^= s[i];
                    h *= 16777619u;
                }
                return h;
            }
        }
    }
}
