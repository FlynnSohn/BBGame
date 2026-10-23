using System;

namespace BBGame.Core
{
    /// <summary>
    /// 프로토타입(JavaScript)과 같은 결과를 내는 수학 도우미.
    /// C# Math.Round는 기본이 은행가 반올림이라 JS Math.round(.5는 위로)와 값이 다르다.
    /// </summary>
    public static class JsMath
    {
        /// <summary>JS Math.round — .5는 +∞ 쪽으로 (예: 136.5 → 137, −2.5 → −2)</summary>
        public static int RoundHalfUp(double x) => (int)Math.Floor(x + 0.5);

        /// <summary>프로토타입 clamp(v, a, b) = max(a, min(b, v))</summary>
        public static double Clamp(double v, double min, double max) => Math.Max(min, Math.Min(max, v));

        /// <summary>정수 clamp</summary>
        public static int Clamp(int v, int min, int max) => Math.Max(min, Math.Min(max, v));
    }
}
