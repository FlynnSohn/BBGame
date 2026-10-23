using BBGame.Presentation.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BBGame.Presentation.Views
{
    /// <summary>
    /// 제압 게이지 (§20-3) — 값, "목표 T · 압도 2T", T·2T 마커, 채움 색 3단, 상태 문구, 목표치 보정 내역.
    /// 막대 끝 = max(1.5T, 게이지) — T가 약 67% 자리 (프로토타입 v9)
    /// </summary>
    public sealed class GaugeView : MonoBehaviour
    {
        [SerializeField] internal TMP_Text _value;
        [SerializeField] internal TMP_Text _target;
        [Tooltip("Image Type = Filled, Horizontal")]
        [SerializeField] internal Image _fill;
        [Tooltip("막대 안에서 앵커로 자리를 잡는다")]
        [SerializeField] internal RectTransform _markT, _mark2T;
        [SerializeField] internal TMP_Text _status;
        [SerializeField] internal TMP_Text _notes;

        public void Set(int gauge, int target, string targetText, string status, string notes, UiPalette p)
        {
            float max = Mathf.Max(1.5f * target, gauge * 1.04f, 1f);
            _value.SetText("{0}", gauge);
            _value.color = gauge >= target ? p.readText : p.text;
            _target.text = targetText;
            _fill.fillAmount = Mathf.Clamp01(gauge / max);
            _fill.color = gauge >= 2 * target ? p.gold : gauge >= target ? p.read : p.warn;
            Place(_markT, target / max);
            Place(_mark2T, Mathf.Min(1f, 2f * target / max));
            _status.text = status;
            _status.color = gauge >= 2 * target ? p.goldBorder : gauge >= target ? p.readText : p.warnText;
            _notes.text = notes;
        }

        private static void Place(RectTransform mark, float x)
        {
            mark.anchorMin = new Vector2(x, 0f);
            mark.anchorMax = new Vector2(x, 1f);
            mark.anchoredPosition = Vector2.zero;
        }
    }
}
