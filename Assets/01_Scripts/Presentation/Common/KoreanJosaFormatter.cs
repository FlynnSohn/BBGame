using System;
using UnityEngine.Localization.SmartFormat.Core.Extensions;

namespace BBGame.Presentation.Common
{
    /// <summary>
    /// 한국어 조사 고르기 — Smart String 확장 (decisions 2026-10-13: 한국어 조사는 커스텀 포매터).
    /// 쓰는 법: "{n:josa(이|가)} 2루타를 쳤다" → "윤재석이 2루타를 쳤다" / "강태민이" / "송유찬이" / "노지성이"…
    /// 받침 있는 쪽 | 받침 없는 쪽 순서로 쓴다 (이|가 · 을|를 · 은|는 · 과|와 · 으로|로). "으로|로"는 ㄹ 받침이면 "로"
    /// 한글이 아닌 글자로 끝나면 받침 없음으로 본다. 다른 언어 문장에서는 쓰지 않는다
    /// </summary>
    [Serializable]
    public sealed class KoreanJosaFormatter : FormatterBase
    {
        private const char HangulFirst = '가', HangulLast = '힣';
        private const int JongCount = 28, JongRieul = 8;

        // 실행 중에 만든 확장은 이름이 비어 있다 — 기본 이름을 넣어 둔다
        public KoreanJosaFormatter() => Names = DefaultNames;

        public override string[] DefaultNames => new[] { "josa" };

        public override bool TryEvaluateFormat(IFormattingInfo info)
        {
            string options = info.FormatterOptions;
            int bar = string.IsNullOrEmpty(options) ? -1 : options.IndexOf('|');
            if (bar <= 0 || bar >= options.Length - 1) return false;
            string text = info.CurrentValue as string ?? info.CurrentValue?.ToString();
            if (string.IsNullOrEmpty(text)) return false;

            char last = text[text.Length - 1];
            bool batchim = false, rieul = false;
            if (last >= HangulFirst && last <= HangulLast)
            {
                int jong = (last - HangulFirst) % JongCount;
                batchim = jong != 0;
                rieul = jong == JongRieul;
            }
            // "으로|로" — ㄹ 받침은 "로"를 쓴다 (서울로, 마을로)
            bool first = batchim && !(rieul && string.CompareOrdinal(options, 0, "으로", 0, 2) == 0);

            info.Write(text);
            if (first) info.Write(options, 0, bar);
            else info.Write(options, bar + 1, options.Length - bar - 1);
            return true;
        }

        /// <summary>문자열 데이터베이스의 Smart Formatter에 한 번만 등록한다 (Localization 초기화가 끝난 뒤)</summary>
        public static void Register()
        {
            var sf = UnityEngine.Localization.Settings.LocalizationSettings.StringDatabase?.SmartFormatter;
            if (sf != null && sf.GetFormatterExtension<KoreanJosaFormatter>() == null) sf.AddExtensions(new KoreanJosaFormatter());
        }
    }
}
