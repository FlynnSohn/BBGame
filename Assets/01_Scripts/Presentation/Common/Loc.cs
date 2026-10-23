using System.Collections.Generic;
using System.Globalization;
using UnityEngine.Localization;

namespace BBGame.Presentation.Common
{
    /// <summary>
    /// 문구 꺼내기 도우미. 문구는 모두 String Table에서 온다 — 코드에는 문장을 쓰지 않는다 (decisions 2026-10-13).
    /// 화면 갱신(투구·버튼 입력 뒤)에서만 부른다. 매 프레임 부르지 않는다
    /// </summary>
    public static class Loc
    {
        /// <summary>변수 없는 문구. 비어 있으면 빈 문자열</summary>
        public static string Get(LocalizedString text)
            => text == null || text.IsEmpty ? string.Empty : text.GetLocalizedString();

        /// <summary>소수 표시 — 끝의 0은 지운다 (1.50 → 1.5, 2.00 → 2). 문화권과 상관없이 점</summary>
        public static string Num(double value, int maxDecimals = 2)
            => value.ToString(maxDecimals <= 0 ? "0" : maxDecimals == 1 ? "0.#" : "0.##", CultureInfo.InvariantCulture);

        /// <summary>타율 표시 — .333 (1.000은 그대로)</summary>
        public static string Average(int hits, int atBats)
        {
            double avg = atBats > 0 ? (double)hits / atBats : 0;
            string s = avg.ToString("0.000", CultureInfo.InvariantCulture);
            return s.StartsWith("0", System.StringComparison.Ordinal) ? s.Substring(1) : s;
        }
    }

    /// <summary>
    /// Smart String 이름 변수 묶음 — 사전 하나를 재사용한다 (문구: "목표 {target} · 압도 {dominate}").
    /// 사용: _args.Clear().Set("target", t).Set("dominate", 2 * t).Format(ui.gaugeTarget)
    /// </summary>
    public sealed class TextArgs
    {
        private readonly Dictionary<string, object> _values = new Dictionary<string, object>(8);
        private readonly object[] _wrap = new object[1];

        public TextArgs Clear()
        {
            _values.Clear();
            return this;
        }

        public TextArgs Set(string name, object value)
        {
            _values[name] = value;
            return this;
        }

        /// <summary>지금 변수로 문구를 만든다 (DictionarySource가 {이름}을 찾는다)</summary>
        public string Format(LocalizedString text)
        {
            if (text == null || text.IsEmpty) return string.Empty;
            _wrap[0] = _values;
            return text.GetLocalizedString(_wrap);
        }
    }
}
