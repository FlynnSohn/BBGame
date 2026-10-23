using BBGame.Presentation.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BBGame.Presentation.Views
{
    /// <summary>투구 칸 — 투구 수와 다음 휴식 구간 경계 (Q7: 늘 보인다, 5구 전부터 경고색 §22-4)</summary>
    public sealed class PitchCountView : MonoBehaviour
    {
        [SerializeField] internal TMP_Text _count;
        [SerializeField] internal TMP_Text _rest;
        [SerializeField] internal Image _warnBar;

        public void Set(int pitches, string restLine, bool warn, UiPalette p)
        {
            _count.SetText("{0}", pitches);
            _rest.text = restLine;
            _rest.color = warn ? p.warnText : p.textSub;
            _warnBar.enabled = warn;
            _warnBar.color = p.warn;
        }
    }
}
