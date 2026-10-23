using BBGame.Presentation.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BBGame.Presentation.Views
{
    /// <summary>'오늘' 칩 — 날씨·컨디션·판정 방식·보너스. 칩은 미리 만들어 둔 것을 켜고 끈다 (할당 없음)</summary>
    public sealed class TodayChipsView : MonoBehaviour
    {
        [SerializeField] internal Image[] _chips;
        [SerializeField] internal TMP_Text[] _labels;

        private int _used;

        public void Begin() => _used = 0;

        /// <summary>칩 하나를 켠다. 칩이 모자라면 무시한다</summary>
        public void Add(string text, Color background, Color textColor)
        {
            if (_used >= _chips.Length) return;
            _chips[_used].gameObject.SetActive(true);
            _chips[_used].color = background;
            _labels[_used].text = text;
            _labels[_used].color = textColor;
            _used++;
        }

        public void End()
        {
            for (int i = _used; i < _chips.Length; i++) _chips[i].gameObject.SetActive(false);
        }
    }
}
